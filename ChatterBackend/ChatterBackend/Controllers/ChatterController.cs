using ChatterBackend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatterBackend.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class ChatterController : ControllerBase
{
    private readonly ClientRepository _repository;
    private readonly IHubContext<ChatHub, IChatServerToClient> _hubContext;

    public ChatterController(ClientRepository repository, IHubContext<ChatHub, IChatServerToClient> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    [HttpGet]
    public IActionResult AllUsers()
    {
        this.Log(); // Logging Extension nutzen
        NotifyAdminsOfAction("All users requested");

        var users = _repository.GetAllClients().Select(c => new
        {
            name = c.Name,
            registeredString = c.RegisterTime.ToString("HH:mm:ss"),
            lastMessageTimeString = c.LastMessageTime.ToString("HH:mm:ss"),
            topicsOfInterest = c.TopicsOfInterest
        });

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Broadcast([FromBody] string message)
    {
        this.Log();
        NotifyAdminsOfAction("Broadcast sent");

        // Nachricht an alle via Hub Context senden
        await _hubContext.Clients.All.NewMessage("BROADCAST", message, DateTime.Now.ToString("HH:mm:ss"));

        return Ok();
    }

    private void NotifyAdminsOfAction(string actionMessage)
    {
        // Dies muss "fire and forget" sein oder awaited werden, hier einfachheitshalber nicht awaited im void
        var adminIds = _repository.GetAllClients().Where(c => c.IsAdmin).Select(c => c.ConnectionId).ToList();
        if (adminIds.Any())
        {
            _hubContext.Clients.Clients(adminIds).AdminNotification(actionMessage);
        }
    }
}