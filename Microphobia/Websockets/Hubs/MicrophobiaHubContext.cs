using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace N17Solutions.Microphobia.Websockets.Hubs
{
    public class MicrophobiaHubContext
    {
        private IHubContext<MicrophobiaHub> _hubContext;
        
        public MicrophobiaHubContext(IHubContext<MicrophobiaHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Alerts all connected clients that the system status has changed.
        /// </summary>
        public Task RefreshSystemStatus() => MicrophobiaHubActions.RefreshSystemStatus(_hubContext.Clients.All);

        /// <summary>
        /// Alerts all connected clients that the tasks has changed.
        /// </summary>
        public Task RefreshTasks() => MicrophobiaHubActions.RefreshTasks(_hubContext.Clients.All);

        /// <summary>
        /// Allows the replacement of the HubContext.
        /// </summary>
        /// <param name="newHub">The HubContext to replace the existing one with</param>
        /// <remarks>This is useful in circumstances where different app domains need to listen and interact with the same hub.</remarks>
        public void ReplaceHubContext(IHubContext<MicrophobiaHub> newHub)
        {
            _hubContext = newHub;
        }
    }
}