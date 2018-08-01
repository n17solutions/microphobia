using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Websockets.Hubs;
using Shouldly;
using Xunit;

namespace N17Solutions.Microphobia.Tests
{
    public class ClientTests : IDisposable
    {
        public class ClientLogger : ILogger
        {
            public static List<string> Logs = new List<string>();

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Logs.Add($"Log Level: {logLevel} - {formatter(state, exception)}");
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }

        public class TestOperations
        {
            public const string ExceptionText = "Test Operations threw this exception";

            public void ExceptionThrower()
            {
                throw new InvalidOperationException(ExceptionText);
            }

            public void Runner() { }

            public void FileCreator()
            {
                using (var fs = File.Create("TEST.txt"))
                {
                    var info = new UTF8Encoding(true).GetBytes(DateTime.UtcNow.ToShortDateString());
                    fs.Write(info, 0, info.Length);
                }
            }

            public void FileCreatorWithGuidArgument(Guid argument)
            {
                using (var fs = File.Create("TEST.txt"))
                {
                    var info = new UTF8Encoding(true).GetBytes(argument.ToString());
                    fs.Write(info, 0, info.Length);
                }
            }
        }

        private readonly Mock<IDataProvider> _dataProviderMock = new Mock<IDataProvider>();
        private readonly Mock<ILoggerFactory> _loggerFactoryMock = new Mock<ILoggerFactory>();
        private readonly Client _sut;

        public ClientTests()
        {
            _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new ClientLogger());

            var clientsProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            hubClientsMock.SetupGet(x => x.All).Returns(clientsProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<MicrophobiaHub>>();
            hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);
            var microphobiaContextMock = new MicrophobiaHubContext(hubContextMock.Object);            
            
            _sut = new Client(new Queue(_dataProviderMock.Object, microphobiaContextMock), new MicrophobiaConfiguration(microphobiaContextMock), _loggerFactoryMock.Object);
        }

        [Fact]
        public void Should_Start_Without_Error()
        {
            // Act
            Should.NotThrow(() => _sut.Start());

            // Assert (to get here, means success)
            _sut.Stop();
        }

        [Fact]
        public void Should_Stop_Without_Error()
        {
            // Arrange
            _sut.Start();

            // Allow everything to get going, then stop
            Thread.Sleep(1500);

            // Act/Assert
            Should.NotThrow(() => _sut.Stop());
        }

        [Fact]
        public void Should_Log_Task_Exception()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.ExceptionThrower();
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());

            // Act
            _sut.Start();
            Thread.Sleep(10000);

            // Assert
            ClientLogger.Logs.FirstOrDefault(x => x.Contains("Log Level: Error")).ShouldNotBeNull();
        }

        [Fact]
        public void Should_Log_Task_Started()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.Runner();
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());

            // Act
            _sut.Start();
            Thread.Sleep(10000);

            // Assert
            ClientLogger.Logs.FirstOrDefault(x => x.Contains("Task Started") && x.Contains(nameof(TestOperations.Runner))).ShouldNotBeNull();
        }

        [Fact]
        public void Should_Log_Task_Completed()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.Runner();
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());

            // Act
            _sut.Start();
            Thread.Sleep(10000);

            // Assert
            ClientLogger.Logs.FirstOrDefault(x => x.Contains("Task Finished Processing") && x.Contains(nameof(TestOperations.Runner))).ShouldNotBeNull();
        }

        [Fact]
         public void Should_Perform_Task()
         {
             // Arrange
             File.Delete("TEST.txt");
             Expression<Action<TestOperations>> expression = to => to.FileCreator();
             _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());
 
             // Act
             _sut.Start();
             Thread.Sleep(10000);
 
             // Assert
             File.Exists("TEST.txt").ShouldBeTrue();
         }

        [Fact]
        public void Should_Perform_Task_With_A_Guid_Argument()
        {
            // Arrange
            var guid = Guid.NewGuid();
            File.Delete("TEST.txt");
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithGuidArgument(guid);
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());

            // Act
            _sut.Start();
            Thread.Sleep(10000);

            // Assert
            File.Exists("TEST.txt").ShouldBeTrue();
        }

        public void Dispose()
        {
            _sut.Stop();
        }
    }
}
