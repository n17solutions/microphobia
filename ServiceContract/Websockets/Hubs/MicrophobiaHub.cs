using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace N17Solutions.Microphobia.ServiceContract.Websockets.Hubs
{
    public class MicrophobiaHub : Hub
    {
        private const string TaskHubConnectionsIdentifier = "Microphobia Hub Connections";

        public async Task RefreshTasks()
        {
            await MicrophobiaHubActions.RefreshTasks(Clients.All);
        }

        public async Task RefreshSystemStatus()
        {
            await MicrophobiaHubActions.RefreshSystemStatus(Clients.All);
        }
        
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TaskHubConnectionsIdentifier);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, TaskHubConnectionsIdentifier);
            await base.OnDisconnectedAsync(exception);
        }
    }
}