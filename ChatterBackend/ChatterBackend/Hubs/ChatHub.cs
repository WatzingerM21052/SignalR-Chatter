using Microsoft.AspNetCore.SignalR;

namespace ChatterBackend.Hubs;

// Schnittstelle für Methoden, die der Server am Client aufruft
public interface IChatServerToClient
{
    Task NewMessage(string name, string message, string timestamp);
    Task ClientConnected(string name);
    Task ClientDisconnected(string name);
    Task AdminNotification(string message);
    Task NrClientsChanged(int nr);
}

// Schnittstelle für Methoden, die der Client am Server aufruft
public interface IChatClientToServer
{
    int GetNrClients();
    Task SendMessage(string name, string message, string topic = "");
    Task RegisterTopicsOfInterest(List<string> topicsOfInterest);
    Task<bool> SignIn(string username, string password);
    Task SignOut();
}

public class ChatHub : Hub<IChatServerToClient>, IChatClientToServer
{
    private readonly ClientRepository _repository;

    public ChatHub(ClientRepository repository)
    {
        _repository = repository;
    }

    public override Task OnConnectedAsync()
    {
        this.Log(); // Nutzt deine Extension Method
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        this.Log();
        var client = _repository.GetClient(Context.ConnectionId);
        if (client != null)
        {
            _repository.RemoveClient(Context.ConnectionId);
            await Clients.Others.ClientDisconnected(client.Name);
            await NotifyAdminsClientCountChanged();
        }
        await base.OnDisconnectedAsync(exception);
    }

    public int GetNrClients()
    {
        return _repository.Count;
    }

    public async Task SendMessage(string name, string message, string topic = "")
    {
        _repository.UpdateLastMessageTime(Context.ConnectionId);
        string timestamp = DateTime.Now.ToString("HH:mm:ss");

        if (string.IsNullOrEmpty(topic))
        {
            // An alle senden
            await Clients.All.NewMessage(name, message, timestamp);
        }
        else
        {
            // Extension: Nur an Clients mit diesem Topic senden (plus den Sender selbst)
            var interestedConnectionIds = _repository.GetAllClients()
                .Where(c => c.TopicsOfInterest.Contains(topic) || c.ConnectionId == Context.ConnectionId)
                .Select(c => c.ConnectionId)
                .ToList();

            await Clients.Clients(interestedConnectionIds).NewMessage(name, message, timestamp);
        }
    }

    public Task RegisterTopicsOfInterest(List<string> topicsOfInterest)
    {
        _repository.UpdateTopics(Context.ConnectionId, topicsOfInterest);
        return Task.CompletedTask;
    }

    public async Task<bool> SignIn(string username, string password)
    {
        if (password.Length < 5)
        {
            // Laut Angabe Exception werfen
            throw new HubException("Password must be at least 5 characters long.");
        }

        // Alten Eintrag entfernen falls vorhanden (Re-Login)
        _repository.RemoveClient(Context.ConnectionId);
        _repository.AddClient(Context.ConnectionId, username);

        await Clients.Others.ClientConnected(username);
        await NotifyAdminsClientCountChanged();

        bool isAdmin = username.StartsWith("Admin");
        return isAdmin;
    }

    public async Task SignOut()
    {
        var client = _repository.GetClient(Context.ConnectionId);
        if (client != null)
        {
            _repository.RemoveClient(Context.ConnectionId);
            await Clients.Others.ClientDisconnected(client.Name);
            await NotifyAdminsClientCountChanged();
        }
    }

    private async Task NotifyAdminsClientCountChanged()
    {
        // Finde alle Admins
        var adminIds = _repository.GetAllClients()
            .Where(c => c.IsAdmin)
            .Select(c => c.ConnectionId)
            .ToList();

        if (adminIds.Any())
        {
            await Clients.Clients(adminIds).NrClientsChanged(_repository.Count);
        }
    }
}