using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using Xunit;

namespace N17Solutions.Microphobia.Tests
{
    public class RunnerTests
    {
        private readonly Mock<IDataProvider> _dataProviderMock = new Mock<IDataProvider>();
        private readonly Runners _sut;

        public RunnerTests()
        {
            var clientsProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            hubClientsMock.SetupGet(x => x.All).Returns(clientsProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<MicrophobiaHub>>();
            hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);
            var microphobiaContextMock = new MicrophobiaHubContext(hubContextMock.Object); 
            
            var configuration = new MicrophobiaConfiguration(microphobiaContextMock);
            
            _sut = new Runners(_dataProviderMock.Object, configuration);
        }

        [Fact]
        public async Task Should_Call_DataProvider_Register_Runner_Method()
        {
            // Act
            await _sut.Register(new QueueRunner()).ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.RegisterQueueRunner(It.IsAny<QueueRunner>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Call_DataProvider_Deregister_Runner_Method()
        {
            // Act
            await _sut.Deregister("Test", 1).ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.DeregisterQueueRunner(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Clean_Should_Call_DataProvider_GetRunners_Method()
        {
            // Act
            await _sut.Clean().ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.GetRunners(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task Clean_Should_Call_DataProvider_DeregisterQueueRunners_Method()
        {
            // Act
            await _sut.Clean().ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.DeregisterQueueRunners(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Call_DataProvider_MarkTaskProcessed_Method()
        {
            // Act
            await _sut.MarkTaskProcessedTime().ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.MarkQueueRunnerTaskProcessed(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}