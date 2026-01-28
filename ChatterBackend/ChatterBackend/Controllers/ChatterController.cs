using ChatterBackend.Hubs;
using ChatterBackend.Services;
using Microsoft.AspNetCore.Mvc;
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
        this.Log();

        NotifyAdmins("All users requested");

        var data = _repository.GetAllClients().Select(c => new
        {
            name = c.Name,
            registeredString = c.RegisterTime.ToString("HH:mm:ss"),
            // Falls noch keine Nachricht gesendet wurde, könnte man 00:00:00 anzeigen oder die echte Zeit
            lastMessageTimeString = c.LastMessageTime.ToString("HH:mm:ss"),
            topicsOfInterest = c.TopicsOfInterest
        });

        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Broadcast([FromBody] string message)
    {
        this.Log();
        NotifyAdmins($"Broadcast sent: {message}");

        // "Admin" oder "System" als Absender Name
        await _hubContext.Clients.All.NewMessage("Admin", message, DateTime.Now.ToString("HH:mm:ss"));

        return Ok();
    }

    private void NotifyAdmins(string msg)
    {
        // Fire & Forget Notification an Admins
        var adminIds = _repository.GetAllClients()
            .Where(c => c.IsAdmin)
            .Select(c => c.ConnectionId)
            .ToList();

        if (adminIds.Any())
        {
            _hubContext.Clients.Clients(adminIds).AdminNotification(msg);
        }
    }
}