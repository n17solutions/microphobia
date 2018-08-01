using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace N17Solutions.Microphobia.Websockets.Hubs
{
    internal static class MicrophobiaHubActions
    {
        public static Task RefreshSystemStatus(IClientProxy clientProxy) => clientProxy.SendAsync("RefreshSystemStatus");
        public static Task RefreshTasks(IClientProxy clientProxy) => clientProxy.SendAsync("RefreshTasks");
    }
}