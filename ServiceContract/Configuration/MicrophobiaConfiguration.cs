using System;
using System.Collections.Concurrent;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.ServiceContract.Configuration
{
    public class MicrophobiaConfiguration
    {
        private readonly MicrophobiaHubContext _microphobiaHub;
        private bool _isRunning;

        public MicrophobiaConfiguration(MicrophobiaHubContext microphobiaHub)
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
                _microphobiaHub.RefreshSystemStatus();
            }
        }
    }
}