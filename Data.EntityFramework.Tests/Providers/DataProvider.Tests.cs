using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Data.EntityFramework.Providers;
using N17Solutions.Microphobia.Domain.Tasks;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.Utilities.Extensions;
using Shouldly;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBeMadeStatic.Local

namespace N17Solutions.Microphobia.Data.EntityFramework.Tests.Providers
{
    public class DataProviderTests : IDisposable
    {
        private readonly TaskContext _context;
        private readonly IDataProvider _sut;

        private class TestOperator
        {
            public void VoidMethod() => Console.WriteLine("Void Method");
            public string ResultMethod() => "Result Method";
        }

        public DataProviderTests()
        {
            var clientsProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            hubClientsMock.SetupGet(x => x.All).Returns(clientsProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<MicrophobiaHub>>();
            hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);
            var microphobiaContextMock = new MicrophobiaHubContext(hubContextMock.Object);  
            
            var dbOptions = new DbContextOptionsBuilder<TaskContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new TaskContext(dbOptions);

            var config = new MicrophobiaConfiguration(microphobiaContextMock);
            _sut = new DataProvider(_context, config);
        }

        [Fact]
        public async Task Dequeues_The_Earliest_Task()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            task1.DateCreated = DateTime.UtcNow;
            task1.DateLastUpdated = task1.DateCreated;

            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            task2.DateCreated = DateTime.UtcNow;
            task2.DateLastUpdated = task2.DateCreated;
            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Act
            var result = await _sut.Dequeue(CancellationToken.None).ConfigureAwait(false);

            // Assert
            result.MethodName.ShouldBe(nameof(TestOperator.VoidMethod));
        }

        [Fact]
        public async Task Dequeues_The_LastUpdated_Task()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            task1.DateCreated = DateTime.UtcNow;
            task1.DateLastUpdated = task1.DateCreated;

            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            task2.DateCreated = DateTime.UtcNow.AddMinutes(-5);
            task2.DateLastUpdated = DateTime.UtcNow.AddMinutes(-10);
            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.Dequeue().ConfigureAwait(false);
            
            // Assert
            result.MethodName.ShouldBe(nameof(TestOperator.ResultMethod));
        }

        [Fact]
        public async Task Only_Dequeues_Waiting_Tasks()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            task1.DateCreated = DateTime.UtcNow;
            task1.DateLastUpdated = task1.DateCreated;
            
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            task2.Status = TaskStatus.Faulted;
            task2.DateCreated = DateTime.UtcNow.AddMinutes(-5);
            task2.DateLastUpdated = DateTime.UtcNow.AddMinutes(-10);
            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.Dequeue().ConfigureAwait(false);
            
            // Assert
            result.MethodName.ShouldBe(nameof(TestOperator.VoidMethod));
        }

        [Fact]
        public async Task Enqueues_Correctly_With_No_TaskStatus()
        {
            // Arrange 
            var taskInfo = new ServiceContract.Models.TaskInfo
            {
                Id = Guid.NewGuid(),
                AssemblyName = "Test.Assembly",
                MethodName = "Test.Method",
                ReturnType = typeof(string),
                TypeName = "string",
                Arguments = new object[]{"Argument"}
            };
            
            // Act
            await _sut.Enqueue(taskInfo).ConfigureAwait(false);
            
            // Assert
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(taskInfo.Id) && t.Status.Equals(TaskStatus.Created)).ConfigureAwait(false);
            task.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task Enqueues_Correctly_With_TaskStatus_Given()
        {
            // Arrange 
            var taskInfo = new ServiceContract.Models.TaskInfo
            {
                Id = Guid.NewGuid(),
                AssemblyName = "Test.Assembly",
                MethodName = "Test.Method",
                ReturnType = typeof(string),
                TypeName = "string",
                Arguments = new object[]{"Argument"},
                Status = TaskStatus.WaitingForActivation
            };
            
            // Act
            await _sut.Enqueue(taskInfo).ConfigureAwait(false);
            
            // Assert
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(taskInfo.Id) && t.Status.Equals(TaskStatus.Created)).ConfigureAwait(false);
            task.ShouldNotBeNull();
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
