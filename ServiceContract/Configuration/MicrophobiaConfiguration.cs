using System;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.ServiceContract.Configuration
{
    public class MicrophobiaConfiguration
    {
        private readonly MicrophobiaHubContext _microphobiaHub;
        
        public MicrophobiaConfiguration(MicrophobiaHubContext microphobiaHub)
        {
            _microphobiaHub = microphobiaHub;            
        }
        
        public int PollIntervalMs { get; set; } = 5000;

        public int MaxThreads { get; set; } = DiscoverMaxThreads();
        
        public Storage StorageType { get; set; }
        
        public ServiceFactory ServiceFactory { get; set; }

        public int StopLoggingNothingToQueueAfter { get; set; } = 3;

        public string RunnerName { get; set; } = Guid.NewGuid().ToString("N");
        
        public int RunnerIndexer { get; private set; }

        public string Tag { get; set; }

        public void SetRunnerIndexer(int indexer)
        {
            RunnerIndexer = indexer;
        }
        
        private static int DiscoverMaxThreads()
        {
            var processorCount = Environment.ProcessorCount;
            if (processorCount % 2 == 0)
                return processorCount / 2;

            var evenProcessorCount = processorCount - 1;
            if (evenProcessorCount <= 1)
                return 1;

            return evenProcessorCount / 2;
        }
    }
}