using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceResolution;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Utilities.Serialization;
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
                using (var fs = File.Create($"TEST.txt"))
                {
                    var info = new UTF8Encoding(true).GetBytes(DateTime.UtcNow.ToShortDateString());
                    fs.Write(info, 0, info.Length);
                }
            }

            public void FileCreatorWithGuidArgument(Guid argument)
            {
                var filename = $"TEST_{argument}.txt";
                using (var fs = File.Create(filename))
                {
                    var info = new UTF8Encoding(true).GetBytes(argument.ToString());
                    fs.Write(info, 0, info.Length);
                }
            }

            public void FileCreatorWithMultipleGuidArguments(Guid argument1, Guid argument2)
            {
                var filename = $"TEST_{argument1}_{argument2}.txt";
                
                using (var fs = File.Create(filename))
                {
                    var info = new UTF8Encoding(true).GetBytes($"{argument1} - {argument2}");
                    fs.Write(info, 0, info.Length);
                }
            }
            
            public void FileCreatorWithMultipleBoolArguments(bool argument1, bool argument2)
            {
                var filename = $"TEST_{argument1}_{argument2}.txt";
                
                using (var fs = File.Create(filename))
                {
                    var info = new UTF8Encoding(true).GetBytes($"{argument1} - {argument2}");
                    fs.Write(info, 0, info.Length);
                }
            }

            public async Task FileCreatorAsync()
            {
                using (var fs = File.Create("TEST.async.txt"))
                {
                    var info = new UTF8Encoding(true).GetBytes(DateTime.UtcNow.ToLongDateString());
                    await fs.WriteAsync(info, 0, info.Length).ConfigureAwait(false);
                }
            }
        }

        private readonly Mock<IDataProvider> _dataProviderMock = new Mock<IDataProvider>();
        private readonly Mock<ILoggerFactory> _loggerFactoryMock = new Mock<ILoggerFactory>();
        private readonly MicrophobiaConfiguration _configuration;
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
            
            _configuration = new MicrophobiaConfiguration(microphobiaContextMock);
            
            _sut = new Client(new Queue(_dataProviderMock.Object, microphobiaContextMock, _configuration), _configuration, _loggerFactoryMock.Object);
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
            Thread.Sleep(1000);

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
            Thread.Sleep(1000);

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
            Thread.Sleep(1000);

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
             Thread.Sleep(1000);
 
             // Assert
             File.Exists("TEST.txt").ShouldBeTrue();
         }

        [Fact]
        public void Should_Perform_Task_With_A_Guid_Argument()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var filename = $"TEST_{guid}.txt";
            
            File.Delete(filename);
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithGuidArgument(guid);
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());

            // Act
            _sut.Start();
            Thread.Sleep(1000);

            // Assert
            File.Exists(filename).ShouldBeTrue();
        }

        [Fact]
        public void Should_Perform_Asynchronous_Task()
        {
            // Arrange
            File.Delete("TEST.async.txt");
            Expression<Action<TestOperations>> expression = to => to.FileCreatorAsync();
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(expression.ToTaskInfo());
            
            // Act
            _sut.Start();
            Thread.Sleep(1000);
            
            // Assert
            File.Exists("TEST.async.txt").ShouldBeTrue();
        }

        [Fact]
        [Trait("Reason", "Found errors in the wild.")]
        public void Should_Deserialise_Multiple_Guids_Properly()
        {
            // Arrange
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var filename = $"TEST_{guid1}_{guid2}.txt";
            
            File.Delete(filename);
            
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithMultipleGuidArguments(guid1, guid2);
            
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                var json = TaskInfoSerialization.Serialize(expression.ToTaskInfo());
                return TaskInfoSerialization.Deserialize(json);
            });
            
            // Act
            _sut.Start();
            Thread.Sleep(1000);
            
            // Assert
            File.Exists(filename).ShouldBeTrue();
        }
        
        [Fact]
        [Trait("Reason", "Found errors in the wild.")]
        public void Should_Deserialise_Multiple_Bool_Properly()
        {
            // Arrange
            const bool bool1 = false;
            const bool bool2 = true;
            
            var filename = $"TEST_{bool1}_{bool2}.txt";
            
            File.Delete(filename);
            
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithMultipleBoolArguments(bool1, bool2);
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                var json = TaskInfoSerialization.Serialize(expression.ToTaskInfo());
                return TaskInfoSerialization.Deserialize(json);
            });
            
            // Act
            _sut.Start();
            Thread.Sleep(1000);
            
            // Assert
            File.Exists(filename).ShouldBeTrue();
        }
        
        [Fact]
        public void Should_Run_With_ScopedServiceFactory()
        {
            // Arrange
            var instanceRetrieved = false;
            object ServiceFactory(Type type)
            {
                instanceRetrieved = true;
                return new TestOperations();
            }
            
            Expression<Action<TestOperations>> expression = to => to.Runner();
            var taskInfo = expression.ToTaskInfo();
            _configuration.ScopedServiceFactories.TryAdd(taskInfo.Id, ServiceFactory);
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(taskInfo);

            // Act
            _sut.Start();
            Thread.Sleep(1000);
            
            // Assert
            instanceRetrieved.ShouldBeTrue();
        }

        [Fact]
        public void Should_Clear_ScopedServiceFactory()
        {
            // Arrange
            object ServiceFactory(Type type) => new TestOperations();

            Expression<Action<TestOperations>> expression = to => to.Runner();
            var taskInfo = expression.ToTaskInfo();
            _configuration.ScopedServiceFactories.TryAdd(taskInfo.Id, ServiceFactory);
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(taskInfo);

            // Act
            _sut.Start();
            Thread.Sleep(1000);

            // Assert
            _configuration.ScopedServiceFactories.ContainsKey(taskInfo.Id).ShouldBeFalse();
        }

        public void Dispose()
        {
            _sut.Stop();
        }
    }
}
