using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.Utilities.Extensions;
using Shouldly;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable MemberCanBeMadeStatic.Local

namespace N17Solutions.Microphobia.Tests
{
    public class QueueTests
    {
        private class TestOperator
        {
            public void VoidMethod() => Console.WriteLine("Void Method");
            public string ResultMethod() => "Result Method";
            public string ResultWithArgumentMethod(string arg) => $"Result With Argument Method: {arg}";
            public async Task TaskMethod() => await Task.Run(() => Console.WriteLine("Task Method")).ConfigureAwait(false);
        }

        private readonly MicrophobiaConfiguration _configuration;
        private readonly Mock<IDataProvider> _dataProviderMock = new Mock<IDataProvider>();
        private readonly Queue _sut;

        private const string ExpressionResult = "Test Expression Result";

        public QueueTests()
        {
            var clientsProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            hubClientsMock.SetupGet(x => x.All).Returns(clientsProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<MicrophobiaHub>>();
            hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);
            var microphobiaContextMock = new MicrophobiaHubContext(hubContextMock.Object);
            
            _configuration = new MicrophobiaConfiguration(microphobiaContextMock);
            
            _sut = new Queue(_dataProviderMock.Object, microphobiaContextMock, _configuration);
        }

        [Fact]
        public async Task Enqueues_Typeless_Action_Properly()
        {
            // Arrange
            Expression<Action> expression = () => Console.WriteLine(ExpressionResult);

            // Act
            await _sut.Enqueue(expression).ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.Enqueue(It.Is<TaskInfo>(info => expression.ToTaskInfo().MethodName.Equals(info.MethodName)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Enqueues_Void_Action_With_Type_Properly()
        {
            // Arrange
            Expression<Action<TestOperator>> expression = x => x.VoidMethod();

            // Act
            await _sut.Enqueue(expression).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Enqueue(It.Is<TaskInfo>(info => expression.ToTaskInfo().MethodName.Equals(info.MethodName)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Enqueues_Result_Action_With_Type_Properly()
        {
            // Arrange
            Expression<Action<TestOperator>> expression = x => x.ResultMethod();

            // Act
            await _sut.Enqueue(expression).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Enqueue(It.Is<TaskInfo>(info => expression.ToTaskInfo().MethodName.Equals(info.MethodName)
                                                                            && expression.ToTaskInfo().ReturnType == info.ReturnType),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Enqueues_Result_Action_With_Arguments_And_Type_Properly()
        {
            // Arrange
            Expression<Action<TestOperator>> expression = x => x.ResultWithArgumentMethod(ExpressionResult);

            // Act
            await _sut.Enqueue(expression).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Enqueue(It.Is<TaskInfo>(info => expression.ToTaskInfo().MethodName.Equals(info.MethodName)
                                                                            && expression.ToTaskInfo().ReturnType == info.ReturnType
                                                                            && expression.ToTaskInfo().Arguments.Length == info.Arguments.Length),
                It.IsAny<CancellationToken>()), Times.Once());
        }
        
        [Fact]
        public async Task Enqueues_Task_With_ScopedServiceFactory_Properly()
        {
            // Arrange
            object ServiceFactory(Type type) => new TestOperator();
            Expression<Action<TestOperator>> expression = x => x.ResultMethod();
            
            // Act
            await _sut.Enqueue(expression, ServiceFactory).ConfigureAwait(false);
            
            // Assert
            _configuration.ScopedServiceFactories.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task Enqueues_Task_With_Created_Status()
        {
            // Act
            await _sut.Enqueue(() => Console.WriteLine(ExpressionResult)).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Enqueue(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.Created)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Enqueues_Async_Task_Properly()
        {
            // Act
            await _sut.Enqueue<TestOperator>(x => x.TaskMethod()).ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.Enqueue(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.Created) && info.IsAsync), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Dequeues_Task_Properly()
        {
            // Act
            await _sut.Dequeue().ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Dequeue(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Set_Dequeued_Task_As_WaitingToRun()
        {
            // Arrange
            var taskInfo = new TaskInfo
            {
                Id = Guid.NewGuid(),
                MethodName = "Test Method",
                Status = TaskStatus.Created
            };
            _dataProviderMock.Setup(x => x.Dequeue(It.IsAny<CancellationToken>())).ReturnsAsync(taskInfo);

            // Act
            await _sut.Dequeue().ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Save(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.WaitingToRun)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Mark_Task_As_Faulted()
        {
            // Act
            await _sut.SetQueuedTaskFaulted(new TaskInfo(), new Exception()).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Save(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.Faulted)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Save_Failure_Information()
        {
            // Arrange
            const string failureText = "Test Failure Information";
            const string innerFailureText = "Inner Test Failure Information";
            
            var innerException = new Exception(innerFailureText);
            var exception = new Exception(failureText, innerException);
            
            // Act
            await _sut.SetQueuedTaskFaulted(new TaskInfo(), exception).ConfigureAwait(false);
            
            // Assert
            _dataProviderMock.Verify(x => x.Save(It.Is<TaskInfo>(info => info.FailureDetails.Contains(failureText) && info.FailureDetails.Contains(innerFailureText)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Mark_Task_As_Completed()
        {
            // Act
            await _sut.SetQueuedTaskCompleted(new TaskInfo()).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Save(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.RanToCompletion)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Mark_Task_As_WaitingToRun()
        {
            // Act
            await _sut.SetQueuedTaskWaitingToRun(new TaskInfo()).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Save(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.WaitingToRun)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Should_Mark_Task_As_Running()
        {
            // Act
            await _sut.SetQueuedTaskRunning(new TaskInfo()).ConfigureAwait(false);

            // Assert
            _dataProviderMock.Verify(x => x.Save(It.Is<TaskInfo>(info => info.Status.Equals(TaskStatus.Running)), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}