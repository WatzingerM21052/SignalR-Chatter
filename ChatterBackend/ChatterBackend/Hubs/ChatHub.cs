using Microsoft.AspNetCore.SignalR;
using ChatterBackend.Services;

namespace ChatterBackend.Hubs;

// Interfaces gemäß PDF
public interface IChatServerToClient
{
    Task NewMessage(string name, string message, string timestamp);
    Task ClientConnected(string name);
    Task ClientDisconnected(string name);
    Task AdminNotification(string message);
    Task NrClientsChanged(int nr);
}

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
        this.Log(); // Logging Extension
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        this.Log();
        var client = _repository.GetClient(Context.ConnectionId);

        // Wenn Client bekannt war (eingeloggt), entfernen und andere benachrichtigen
        if (client != null)
        {
            _repository.RemoveClient(Context.ConnectionId);

            // Info an alle anderen
            await Clients.Others.ClientDisconnected(client.Name);

            // Admins über neue Anzahl informieren
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
        // Update LastMessageTime
        _repository.UpdateLastMessageTime(Context.ConnectionId);

        string timestamp = DateTime.Now.ToString("HH:mm:ss");

        if (string.IsNullOrEmpty(topic))
        {
            // Kein Topic: Broadcast an alle
            await Clients.All.NewMessage(name, message, timestamp);
        }
        else
        {
            // Extension: Nur an Clients mit diesem Topic + Sender
            var recipients = _repository.GetAllClients()
                .Where(c => c.TopicsOfInterest.Contains(topic) || c.ConnectionId == Context.ConnectionId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (recipients.Any())
            {
                await Clients.Clients(recipients).NewMessage(name, message, timestamp);
            }
        }
    }

    public Task RegisterTopicsOfInterest(List<string> topicsOfInterest)
    {
        // Speichern im Repository
        _repository.UpdateTopics(Context.ConnectionId, topicsOfInterest);
        return Task.CompletedTask;
    }

    public async Task<bool> SignIn(string username, string password)
    {
        // Validierung: Pwd < 5 Zeichen -> Exception
        if (string.IsNullOrEmpty(password) || password.Length < 5)
        {
            throw new HubException("Password length must be at least 5 characters");
        }

        // User registrieren (alte Connection ggf. überschreiben)
        _repository.RemoveClient(Context.ConnectionId);
        _repository.AddClient(Context.ConnectionId, username);

        // ClientConnected an alle anderen
        await Clients.Others.ClientConnected(username);

        // Anzahl der Clients hat sich geändert -> Admins informieren
        await NotifyAdminsClientCountChanged();

        // Rückgabe true, wenn Admin
        return username.StartsWith("Admin", StringComparison.OrdinalIgnoreCase);
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

    // Hilfsmethode für Admin-Benachrichtigungen
    private async Task NotifyAdminsClientCountChanged()
    {
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