using Microsoft.AspNetCore.SignalR;

namespace ChatterBackend.Hubs
{
    public class ChatHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            this.Log();

            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception? exception)
        {
            this.Log();

            return base.OnDisconnectedAsync(exception);
        }
    }
}
