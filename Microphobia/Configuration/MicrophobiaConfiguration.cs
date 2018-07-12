﻿using Microsoft.AspNetCore.SignalR;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceResolution;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace N17Solutions.Microphobia.Configuration
{
    public class MicrophobiaConfiguration
    {
        private readonly IHubContext<MicrophobiaHub> _microphobiaHub;
        private bool _isRunning;

        public MicrophobiaConfiguration(IHubContext<MicrophobiaHub> microphobiaHub)
        {
            _microphobiaHub = microphobiaHub;
        }

        public int PollIntervalMs { get; set; } = 100;
        
        public Storage StorageType { get; set; }
        
        public ServiceFactory ServiceFactory { get; set; }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                MicrophobiaHubActions.RefreshSystemStatus(_microphobiaHub.Clients.All);
            }
        }
    }
}