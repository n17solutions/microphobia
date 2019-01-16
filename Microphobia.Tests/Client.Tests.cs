using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Utilities.Serialization;
using Shouldly;
using Xunit;

namespace N17Solutions.Microphobia.Tests
{
    public class ClientLogger : ILogger<Client>, IDisposable
    {
        private readonly List<string> _logs = new List<string>();
        public string[] Logs => _logs.ToArray();
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logs.Add($"Log Level: {logLevel} - {formatter(state, exception)}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public void Dispose()
        {
            // Do Nothing
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
    
    public class ClientTestsSingleThread : IDisposable
    {
        private readonly Mock<IDataProvider> _dataProviderMock = new Mock<IDataProvider>();
        private readonly ClientLogger _logger;
        private readonly Client _sut;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public ClientTestsSingleThread()
        {
            var clientsProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            hubClientsMock.SetupGet(x => x.All).Returns(clientsProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<MicrophobiaHub>>();
            hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);
            var microphobiaContextMock = new MicrophobiaHubContext(hubContextMock.Object);

            var configuration = new MicrophobiaConfiguration(microphobiaContextMock)
            {
                PollIntervalMs = 1000,
                MaxThreads = 1
            };
            
            var runnersMock = new Runners(_dataProviderMock.Object, configuration);
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(Runners))).Returns(runnersMock);
            
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            var queueMock = new Queue(_dataProviderMock.Object, microphobiaContextMock, serviceScopeFactoryMock.Object);
            
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = new ClientLogger();
            
            _sut = new Client(queueMock, serviceScopeFactoryMock.Object, configuration, _logger);
        }

        [Fact]
        public void Should_Start_Without_Error()
        {
            // Act
            Should.NotThrow(async () => await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false));
            
            // Assert (to get here, means success)
            _cancellationTokenSource.Cancel();
        }

        [Fact]
        public async Task Should_Stop_Without_Error()
        {
            // Arrange
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            
            // Allow everything to get going, then stop
            await Task.Delay(1000).ConfigureAwait(false);
            
            // Act/Assert
            Should.NotThrow(() => _cancellationTokenSource.Cancel());
        }

        [Fact]
        public async Task Should_Log_Single_Task_Exception()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.ExceptionThrower();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression.ToTaskInfo())
                .ReturnsAsync(null);

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Log Level: Error"));
            };
            
            // Act
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Log_Multiple_Task_Exceptions()
        {
            // Arrange
            Expression<Action<TestOperations>> expression1 = to => to.ExceptionThrower();
            Expression<Action<TestOperations>> expression2 = to => to.ExceptionThrower();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression1.ToTaskInfo())
                .ReturnsAsync(expression2.ToTaskInfo())
                .ReturnsAsync(null);

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Log Level: Error"));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);        
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(2);
        }
        
        [Fact]
        public async Task Should_Log_Task_Started()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression.ToTaskInfo())
                .ReturnsAsync(null);

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Started") && x.Contains(nameof(TestOperations.Runner)));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);

            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Log_Multiple_Tasks_Started()
        {
            // Arrange
            Expression<Action<TestOperations>> expression1 = to => to.Runner();
            Expression<Action<TestOperations>> expression2 = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression1.ToTaskInfo())
                .ReturnsAsync(expression2.ToTaskInfo())
                .ReturnsAsync(null);
            
            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Started"));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(2);
        }
        
        [Fact]
        public async Task Should_Log_Task_Completed()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression.ToTaskInfo())
                .ReturnsAsync(null);

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Finished Processing") && x.Contains(nameof(TestOperations.Runner)));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);

            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Log_Multiple_Tasks_Completed()
        {
            // Arrange
            Expression<Action<TestOperations>> expression1 = to => to.Runner();
            Expression<Action<TestOperations>> expression2 = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression1.ToTaskInfo())
                .ReturnsAsync(expression2.ToTaskInfo())
                .ReturnsAsync(null);
            
            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Finished Processing") && x.Contains(nameof(TestOperations.Runner)));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(2);
        }
        
        [Fact]
        public async Task Should_Perform_Task()
        {
            // Arrange
            File.Delete("TEST.txt");
            Expression<Action<TestOperations>> expression = to => to.FileCreator();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression.ToTaskInfo())
                .ReturnsAsync(null);

            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                finished = true;
                _cancellationTokenSource.Cancel();
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
 
            // Assert
            File.Exists("TEST.txt").ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Perform_Task_With_A_Guid_Argument()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var filename = $"TEST_{guid}.txt";
            
            File.Delete(filename);
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithGuidArgument(guid);
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression.ToTaskInfo())
                .ReturnsAsync(null);

            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);

            // Assert
            File.Exists(filename).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Perform_Asynchronous_Task()
        {
            // Arrange
            File.Delete("TEST.async.txt");
            Expression<Func<TestOperations, Task>> expression = to => to.FileCreatorAsync();
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expression.ToTaskInfo())
                .ReturnsAsync(null);

            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            File.Exists("TEST.async.txt").ShouldBeTrue();
        }

        [Fact]
        [Trait("Reason", "Found errors in the wild.")]
        public async Task Should_Deserialise_Multiple_Guids_Properly()
        {
            // Arrange
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var filename = $"TEST_{guid1}_{guid2}.txt";
            
            File.Delete(filename);
            
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithMultipleGuidArguments(guid1, guid2);

            var json = TaskInfoSerialization.Serialize(expression.ToTaskInfo());
            var taskInfo = TaskInfoSerialization.Deserialize(json);
            
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskInfo)
                .ReturnsAsync(null);            
            
            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            File.Exists(filename).ShouldBeTrue();
        }
        
        [Fact]
        [Trait("Reason", "Found errors in the wild.")]
        public async Task Should_Deserialise_Multiple_Bool_Properly()
        {
            // Arrange
            const bool bool1 = false;
            const bool bool2 = true;
            
            var filename = $"TEST_{bool1}_{bool2}.txt";
            
            File.Delete(filename);
            
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithMultipleBoolArguments(bool1, bool2);
            var json = TaskInfoSerialization.Serialize(expression.ToTaskInfo());
            var taskInfo = TaskInfoSerialization.Deserialize(json);
            
            _dataProviderMock.SetupSequence(x => x.DequeueSingle(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskInfo)
                .ReturnsAsync(null);
            
            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            File.Exists(filename).ShouldBeTrue();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
    
    public class ClientTestsMultipleThreads : IDisposable
    {
        private readonly Mock<IDataProvider> _dataProviderMock = new Mock<IDataProvider>();
        private readonly ClientLogger _logger;
        private readonly Client _sut;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public ClientTestsMultipleThreads()
        {
            var clientsProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            hubClientsMock.SetupGet(x => x.All).Returns(clientsProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<MicrophobiaHub>>();
            hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);
            var microphobiaContextMock = new MicrophobiaHubContext(hubContextMock.Object);

            var configuration = new MicrophobiaConfiguration(microphobiaContextMock)
            {
                PollIntervalMs = 1000,
                MaxThreads = 2
            };
            
            var runnersMock = new Runners(_dataProviderMock.Object, configuration);
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(Runners))).Returns(runnersMock);
            
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            var queueMock = new Queue(_dataProviderMock.Object, microphobiaContextMock, serviceScopeFactoryMock.Object);
            
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = new ClientLogger();
            
            _sut = new Client(queueMock, serviceScopeFactoryMock.Object, configuration, _logger);
        }

        [Fact]
        public void Should_Start_Without_Error()
        {
            // Act
            Should.NotThrow(async () => await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false));
            
            // Assert (to get here, means success)
            _cancellationTokenSource.Cancel();
        }

        [Fact]
        public async Task Should_Stop_Without_Error()
        {
            // Arrange
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            
            // Allow everything to get going, then stop
            await Task.Delay(1000).ConfigureAwait(false);
            
            // Act/Assert
            Should.NotThrow(() => _cancellationTokenSource.Cancel());
        }

        [Fact]
        public async Task Should_Log_Single_Task_Exception()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.ExceptionThrower();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Log Level: Error"));
            };
            
            // Act
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Log_Multiple_Task_Exceptions()
        {
            // Arrange
            Expression<Action<TestOperations>> expression1 = to => to.ExceptionThrower();
            Expression<Action<TestOperations>> expression2 = to => to.ExceptionThrower();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression1.ToTaskInfo(), expression2.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Log Level: Error"));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);        
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(2);
        }
        
        [Fact]
        public async Task Should_Log_Task_Started()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Started") && x.Contains(nameof(TestOperations.Runner)));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);

            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Log_Multiple_Tasks_Started()
        {
            // Arrange
            Expression<Action<TestOperations>> expression1 = to => to.Runner();
            Expression<Action<TestOperations>> expression2 = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression1.ToTaskInfo(), expression2.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());
            
            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Started"));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(2);
        }
        
        [Fact]
        public async Task Should_Log_Task_Completed()
        {
            // Arrange
            Expression<Action<TestOperations>> expression = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());

            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Finished Processing") && x.Contains(nameof(TestOperations.Runner)));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);

            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Log_Multiple_Tasks_Completed()
        {
            // Arrange
            Expression<Action<TestOperations>> expression1 = to => to.Runner();
            Expression<Action<TestOperations>> expression2 = to => to.Runner();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression1.ToTaskInfo(), expression2.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());
            
            int? logCount = null;
            async Task LoopUntilFinished()
            {
                while (!logCount.HasValue)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                logCount = _logger.Logs.Count(x => x.Contains("Task Finished Processing") && x.Contains(nameof(TestOperations.Runner)));
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            logCount.ShouldNotBeNull();
            logCount.ShouldBe(2);
        }
        
        [Fact]
        public async Task Should_Perform_Task()
        {
            // Arrange
            File.Delete("TEST.txt");
            Expression<Action<TestOperations>> expression = to => to.FileCreator();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new []{expression.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());

            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) =>
            {
                finished = true;
                _cancellationTokenSource.Cancel();
            };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
 
            // Assert
            File.Exists("TEST.txt").ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Perform_Task_With_A_Guid_Argument()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var filename = $"TEST_{guid}.txt";
            
            File.Delete(filename);
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithGuidArgument(guid);
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());

            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);

            // Assert
            File.Exists(filename).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Perform_Asynchronous_Task()
        {
            // Arrange
            File.Delete("TEST.async.txt");
            Expression<Func<TestOperations, Task>> expression = to => to.FileCreatorAsync();
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {expression.ToTaskInfo()})
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());
            
            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            File.Exists("TEST.async.txt").ShouldBeTrue();
        }

        [Fact]
        [Trait("Reason", "Found errors in the wild.")]
        public async Task Should_Deserialise_Multiple_Guids_Properly()
        {
            // Arrange
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var filename = $"TEST_{guid1}_{guid2}.txt";
            
            File.Delete(filename);
            
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithMultipleGuidArguments(guid1, guid2);

            var json = TaskInfoSerialization.Serialize(expression.ToTaskInfo());
            var taskInfo = TaskInfoSerialization.Deserialize(json);
            
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { taskInfo })
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());            
            
            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            File.Exists(filename).ShouldBeTrue();
        }
        
        [Fact]
        [Trait("Reason", "Found errors in the wild.")]
        public async Task Should_Deserialise_Multiple_Bool_Properly()
        {
            // Arrange
            const bool bool1 = false;
            const bool bool2 = true;
            
            var filename = $"TEST_{bool1}_{bool2}.txt";
            
            File.Delete(filename);
            
            Expression<Action<TestOperations>> expression = to => to.FileCreatorWithMultipleBoolArguments(bool1, bool2);
            _dataProviderMock.SetupSequence(x => x.Dequeue(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    var json = TaskInfoSerialization.Serialize(expression.ToTaskInfo());
                    var taskInfo = TaskInfoSerialization.Deserialize(json);
                    return Task.FromResult(new[] {taskInfo}.AsEnumerable());
                })
                .ReturnsAsync(Enumerable.Empty<TaskInfo>());
            
            var finished = false;

            async Task LoopUntilFinished()
            {
                while (!finished)
                    await Task.Delay(200).ConfigureAwait(false);
            }
            
            // Act
            _sut.AllTasksProcessed += (o, e) => { finished = true; };
            await _sut.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            await LoopUntilFinished().ConfigureAwait(false);
            
            // Assert
            File.Exists(filename).ShouldBeTrue();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
