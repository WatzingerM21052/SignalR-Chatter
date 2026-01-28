using System.Collections.Concurrent;

namespace ChatterBackend.Services;

public class ClientInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime RegisterTime { get; set; }
    public DateTime LastMessageTime { get; set; }
    public List<string> TopicsOfInterest { get; set; } = new();

    public bool IsAdmin => Name.StartsWith("Admin");
}

public class ClientRepository
{
    // Thread-sicherer Dictionary-Speicher
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

    public void AddClient(string connectionId, string name)
    {
        var info = new ClientInfo
        {
            ConnectionId = connectionId,
            Name = name,
            RegisterTime = DateTime.Now,
            LastMessageTime = DateTime.Now
        };
        _clients[connectionId] = info;
    }

    public void RemoveClient(string connectionId)
    {
        _clients.TryRemove(connectionId, out _);
    }

    public ClientInfo? GetClient(string connectionId)
    {
        _clients.TryGetValue(connectionId, out var client);
        return client;
    }

    public void UpdateLastMessageTime(string connectionId)
    {
        if (_clients.TryGetValue(connectionId, out var client))
        {
            client.LastMessageTime = DateTime.Now;
        }
    }

    public void UpdateTopics(string connectionId, List<string> topics)
    {
        if (_clients.TryGetValue(connectionId, out var client))
        {
            client.TopicsOfInterest = topics;
        }
    }

    public IEnumerable<ClientInfo> GetAllClients() => _clients.Values;

    public int Count => _clients.Count;
}