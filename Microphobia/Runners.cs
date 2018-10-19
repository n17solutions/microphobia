using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;

namespace N17Solutions.Microphobia
{
    public class Runners
    {
        private readonly IDataProvider _dataProvider;
        private readonly MicrophobiaConfiguration _config;

        public Runners(IDataProvider dataProvider, MicrophobiaConfiguration config)
        {
            _dataProvider = dataProvider;
            _config = config;
        }

        public Task<int> Register(QueueRunner runner, CancellationToken cancellationToken = default)
        {
            return _dataProvider.RegisterQueueRunner(runner, cancellationToken);
        }

        public Task Deregister(string name, int uniqueIndexer, CancellationToken cancellationToken = default)
        {
            return _dataProvider.DeregisterQueueRunner(name, uniqueIndexer, cancellationToken);
        }

        public Task MarkTaskProcessedTime(string runnerName = default, CancellationToken cancellationToken = default)
        {
            return _dataProvider.MarkQueueRunnerTaskProcessed(runnerName.IsDefault() ? _config.RunnerName : runnerName, cancellationToken);
        }

        public async Task Clean(CancellationToken cancellationToken = default)
        {
            var getDeadRunnersTask = _dataProvider.GetRunners(isRunning: false, cancellationToken: cancellationToken);
            var getStalledRunnersTask = _dataProvider.GetRunners(lastUpdatedBefore: DateTime.UtcNow.AddMilliseconds((_config.PollIntervalMs + 1000) * -1), cancellationToken: cancellationToken);

            var deadRunners = await getDeadRunnersTask.ConfigureAwait(false);
            var stalledRunners = await getStalledRunnersTask.ConfigureAwait(false);

            var runnersToCleanNames = deadRunners.Select(runner => runner.Name).Union(stalledRunners.Select(runner => runner.Name)).Distinct();
            await _dataProvider.DeregisterQueueRunners(runnersToCleanNames, cancellationToken).ConfigureAwait(false);
        }
    }
}