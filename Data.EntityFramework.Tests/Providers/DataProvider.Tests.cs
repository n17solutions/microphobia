using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Data.EntityFramework.Providers;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.Utilities.Extensions;
using Shouldly;
using Xunit;
using TaskInfo = N17Solutions.Microphobia.Domain.Tasks.TaskInfo;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBeMadeStatic.Local

namespace N17Solutions.Microphobia.Data.EntityFramework.Tests.Providers
{
    public class DataProviderTests : IDisposable
    {
        private readonly TaskContext _context;
        private readonly IDataProvider _sut;
        private const string ConfigTag = "ConfigTag";

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

            var config = new MicrophobiaConfiguration(microphobiaContextMock)
            {
                Tag = ConfigTag
            };
            _sut = new DataProvider(_context, config);
        }

        [Fact]
        public async Task Dequeues_All_Waiting_Tasks()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            Expression<Action<TestOperator>> expression3 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            task1.DateCreated = DateTime.UtcNow;
            task1.DateLastUpdated = task1.DateCreated;

            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            task2.DateCreated = DateTime.UtcNow;
            task2.DateLastUpdated = task2.DateCreated;

            var task3 = TaskInfo.FromTaskInfoResponse(expression3.ToTaskInfo(), Storage.None);
            task3.DateCreated = DateTime.UtcNow;
            task3.DateLastUpdated = task3.DateCreated;
            await _context.Tasks.AddRangeAsync(task1, task2, task3).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.Dequeue(cancellationToken: CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            var resultArray = result as ServiceContract.Models.TaskInfo[] ?? result.ToArray();
            resultArray.ShouldNotBeEmpty();
            resultArray.Length.ShouldBe(3);
        }
            

        [Fact]
        public async Task Dequeues_The_Earliest_Task_With_No_Tags()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            Expression<Action<TestOperator>> expression3 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            task1.DateCreated = DateTime.UtcNow;
            task1.DateLastUpdated = task1.DateCreated;

            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            task2.DateCreated = DateTime.UtcNow;
            task2.DateLastUpdated = task2.DateCreated;

            var task3 = TaskInfo.FromTaskInfoResponse(expression3.ToTaskInfo(), Storage.None);
            task3.DateCreated = DateTime.UtcNow;
            task3.DateLastUpdated = task3.DateCreated;
            task3.Tags = "Tag1,Tag2";
            await _context.Tasks.AddRangeAsync(task1, task2, task3).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Act
            var result = await _sut.DequeueSingle(cancellationToken: CancellationToken.None).ConfigureAwait(false);

            // Assert
            result.MethodName.ShouldBe(nameof(TestOperator.VoidMethod));
            result.Tags.ShouldBeNull();
        }

        [Fact]
        public async Task Dequeues_The_Earliest_Task_With_The_Given_Tag()
        {
            // Arrange
            const string tag = "Tag1";
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            Expression<Action<TestOperator>> expression3 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(new []{tag, "Tag2"}), Storage.None);
            task1.DateCreated = DateTime.UtcNow;
            task1.DateLastUpdated = task1.DateCreated;
            
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(new []{tag,"Tag3"}), Storage.None);
            task2.DateCreated = DateTime.UtcNow;
            task2.DateLastUpdated = task2.DateCreated;
            
            var task3 = TaskInfo.FromTaskInfoResponse(expression3.ToTaskInfo(), Storage.None);
            task3.DateCreated = DateTime.UtcNow;
            task3.DateLastUpdated = task3.DateCreated;
            await _context.Tasks.AddRangeAsync(task1, task2, task3).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.DequeueSingle(tag, CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            result.MethodName.ShouldBe(nameof(TestOperator.VoidMethod));
            result.Tags.ShouldContain(tag);
        }
        
        [Fact]
        public async Task Dequeues_The_LastUpdated_Task()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);

            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);

            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            Thread.Sleep(2000);

            task1.Status = TaskStatus.Canceled;
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.DequeueSingle().ConfigureAwait(false);
            
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
            var result = await _sut.DequeueSingle().ConfigureAwait(false);
            
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

        [Fact]
        public async Task Enqueues_With_Tags_Correctly()
        {
            // Arrange
            string[] tags = {"Tag1", "Tag2"};
            var taskInfo = new ServiceContract.Models.TaskInfo
            {
                Id = Guid.NewGuid(),
                AssemblyName = "Test.Assembly",
                MethodName = "Test.Method",
                ReturnType = typeof(string),
                TypeName = "string",
                Arguments = new object[] {"Argument"},
                Status = TaskStatus.WaitingForActivation,
                Tags = tags
            };
            
            // Act
            await _sut.Enqueue(taskInfo).ConfigureAwait(false);
            
            // Assert
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(taskInfo.Id)).ConfigureAwait(false);
            foreach (var tag in tags)
                task.Tags.Contains(tag).ShouldBeTrue();
        }
        
        [Fact]
        public async Task Enqueues_With_Config_Tags_Correctly()
        {
            // Arrange
            string[] tags = {"Tag1", "Tag2"};
            var taskInfo = new ServiceContract.Models.TaskInfo
            {
                Id = Guid.NewGuid(),
                AssemblyName = "Test.Assembly",
                MethodName = "Test.Method",
                ReturnType = typeof(string),
                TypeName = "string",
                Arguments = new object[] {"Argument"},
                Status = TaskStatus.WaitingForActivation,
                Tags = tags
            };
            
            // Act
            await _sut.Enqueue(taskInfo).ConfigureAwait(false);
            
            // Assert
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(taskInfo.Id)).ConfigureAwait(false);
            task.Tags.ShouldContain(ConfigTag);
        }

        [Fact]
        public async Task Enqueues_With_Distinct_Tags_Correctly()
        {
            // Arrange
            var tags = new[]
            {
                "Tag1",
                "Tag2",
                ConfigTag
            };
            
            var taskInfo = new ServiceContract.Models.TaskInfo
            {
                Id = Guid.NewGuid(),
                AssemblyName = "Test.Assembly",
                MethodName = "Test.Method",
                ReturnType = typeof(string),
                TypeName = "string",
                Arguments = new object[] {"Argument"},
                Status = TaskStatus.WaitingForActivation,
                Tags = tags
            };
            
            // Act
            await _sut.Enqueue(taskInfo).ConfigureAwait(false);
            
            // Assert
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(taskInfo.Id)).ConfigureAwait(false);
            var explodedTaskTags = task.Tags.Split(',');
            foreach (var tag in tags.Union(new[] {ConfigTag}))
                explodedTaskTags.Count(t => t.Equals(tag)).ShouldBe(1);
        }

        [Fact]
        public async Task Should_Register_Runner()
        {
            // Arrange
            var runner = new QueueRunner
            {
                Name = "Test Runner"
            };
            
            // Act
            var result = await _sut.RegisterQueueRunner(runner, CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            (await _context.Runners.FirstOrDefaultAsync(r => r.Name == runner.Name).ConfigureAwait(false)).ShouldNotBeNull();
            result.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Register_Again_If_Already_Registered()
        {
            // Arrange
            var runner = new QueueRunner
            {
                Name = "Test Runner"
            };
            await _context.Runners.AddAsync(new Domain.Clients.QueueRunner {Name = runner.Name, Tag = ConfigTag}).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.RegisterQueueRunner(runner).ConfigureAwait(false);
            
            // Assert
            _context.Runners.Count(x => x.Name == runner.Name).ShouldBe(2);
            result.ShouldBe(2);
        }

        [Fact]
        public async Task Should_Deregister_Runner_If_Registered()
        {
            // Arrange
            const string name = "Test Runner";
            const int indexer = 1;
            await _context.Runners.AddAsync(new Domain.Clients.QueueRunner {Name = name, UniqueIndexer = indexer, Tag = ConfigTag}).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            await _sut.DeregisterQueueRunner(name, ConfigTag, indexer, CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            (await _context.Runners.FirstOrDefaultAsync(r => r.Name == name).ConfigureAwait(false)).ShouldBeNull();
        }
        
        [Fact]
        public async Task Should_Deregister_Runner_If_Not_Registered()
        {
            // Arrange
            const string name = "Test Runner";
            const string tag = "Test Tag";
            
            // Act
            await _sut.DeregisterQueueRunner(name, tag, 1, CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            (await _context.Runners.FirstOrDefaultAsync(r => r.Name == name).ConfigureAwait(false)).ShouldBeNull();
        }

        [Fact]
        public async Task Should_Deregister_Multiple_Runners_If_Registered()
        {
            // Arrange
            await _context.Runners.AddRangeAsync(new Domain.Clients.QueueRunner {Name = "Runner 1"}, new Domain.Clients.QueueRunner {Name = "Runner 2"})
                .ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            await _sut.DeregisterQueueRunners(new[] {"Runner 1", "Runner 2"}, CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            _context.Runners.Where(runner => runner.Name == "Runner 1" || runner.Name == "Runner 2").ToArray().Length.ShouldBe(0);
        }
        
        [Fact]
        public async Task Should_Deregister_Multiple_Runners_If_Not_Registered()
        {
            // Act
            await _sut.DeregisterQueueRunners(new[] {"Runner 1", "Runner 2"}, CancellationToken.None).ConfigureAwait(false);
            
            // Assert
            _context.Runners.Where(runner => runner.Name == "Runner 1" || runner.Name == "Runner 2").ToArray().Length.ShouldBe(0);
        }
        
        [Fact]
        public async Task Should_Get_Runners()
        {
            // Arrange
            await _context.Runners.AddRangeAsync(new Domain.Clients.QueueRunner
                {
                    Name = "Runner 1",
                    IsRunning = true
                }, new Domain.Clients.QueueRunner
                {
                    Name = "Runner 2",
                    IsRunning = true
                })
                .ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetRunners().ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(2);
        }

        [Fact]
        public async Task Should_Only_Get_Runners_With_Tag()
        {
            // Arrange
            const string tag = "Tag";
            await _context.Runners.AddRangeAsync(new Domain.Clients.QueueRunner
                {
                    Name = "Runner 1", 
                    Tag = tag,
                    IsRunning = true
                }, new Domain.Clients.QueueRunner
                {
                    Name = "Runner 2",
                    IsRunning = true
                })
                .ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetRunners(tag).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_Only_Get_Runners_With_Given_IsRunning(bool isRunning)
        {
            // Arrange
            await _context.Runners.AddRangeAsync(new Domain.Clients.QueueRunner
                {
                    Name = "Runner 1", 
                    IsRunning = true
                }, new Domain.Clients.QueueRunner
                {
                    Name = "Runner 2",
                    IsRunning = false
                })
                .ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetRunners(isRunning: isRunning).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Only_Get_Runners_With_LastUpdatedDate_Less_Than_Given()
        {
            // Arrange
            var runner1 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 1",
                IsRunning = true
            };
            var runner2 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 2",
                IsRunning = true
            };
            await _context.AddRangeAsync(runner1, runner2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            Thread.Sleep(1000);
            var testDate = DateTime.UtcNow;
            Thread.Sleep(1000);

            runner1.Name = "Changed Runner 1";
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetRunners(lastUpdatedBefore: testDate).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
            result[0].Name.ShouldBe(runner2.Name);
        }
        
        [Fact]
        public async Task Should_Only_Get_Runners_With_LastUpdatedDate_Greater_Than_Given()
        {
            // Arrange
            var runner1 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 1",
                IsRunning = true
            };
            var runner2 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 2",
                IsRunning = true
            };
            await _context.AddRangeAsync(runner1, runner2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            Thread.Sleep(1000);
            var testDate = DateTime.UtcNow;
            Thread.Sleep(1000);

            runner1.Name = "Changed Runner 1";
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetRunners(lastUpdatedSince: testDate).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
            result[0].Name.ShouldBe(runner1.Name);
        }

        [Fact]
        public async Task Should_Only_Get_Runners_With_Tag_And_IsRunningFlag_And_LastUpdatedDateSince_And_LastUpdatedDateBefore_Provided()
        {
            // Arrange
            const string tag = "Tag";
            var runner1 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 1",
                IsRunning = true
            };
            var runner2 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 2",
                IsRunning = false
            };
            var runner3 = new Domain.Clients.QueueRunner
            {
                Name = "Runner 3",
                IsRunning = true,
                Tag = tag
            };
            await _context.AddRangeAsync(runner1, runner2, runner3).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            Thread.Sleep(1000);
            var testDate = DateTime.UtcNow;
            Thread.Sleep(1000);

            runner1.Tag = tag;
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetRunners(tag, true, testDate, DateTime.UtcNow).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(2);
            result[0].Name.ShouldBe(runner1.Name);
            result[1].Name.ShouldBe(runner3.Name);
        }

        [Fact]
        public async Task Should_Mark_Runner_As_Task_Processed()
        {
            // Arrange
            var runner = new Domain.Clients.QueueRunner
            {
                Name = "Runner 1",
                IsRunning = true
            };
            await _context.AddAsync(runner).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            await _sut.MarkQueueRunnerTaskProcessed(runner.Name).ConfigureAwait(false);
            
            // Assert
            (await _context.Runners.SingleAsync(r => r.Name == runner.Name).ConfigureAwait(false)).LastTaskProcessed.ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Get_All_Tasks()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);

            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);

            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.GetTasks().ConfigureAwait(false);
            
            // Assert
            result.Count().ShouldBe(2);
        }

        [Fact]
        public async Task Should_Get_Tasks_With_Created_Date_Higher_Than_Given()
        {
            // Arrange
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);

            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);

            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            task1.DateCreated = DateTime.Now.AddYears(-5);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Act
            var result = await _sut.GetTasks(DateTime.Now.AddDays(-1)).ConfigureAwait(false);

            // Assert
            result.Count().ShouldBe(1);
        }

        [Fact]
        public async Task Should_Get_Tasks_Only_With_Given_Tag()
        {
            // Arrange
            const string tag = "RetrieveMe";
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(new[]{tag}), Storage.None);
            
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            
            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.GetTasks(tag: tag).ConfigureAwait(false);
            
            // Assert
            result.Count().ShouldBe(1);
        }

        [Fact]
        public async Task Should_Get_Tasks_Only_With_Given_Status()
        {
            // Arrange
            const TaskStatus status = TaskStatus.RanToCompletion;
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            task1.Status = status;
            
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(), Storage.None);
            
            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.GetTasks(status: status).ConfigureAwait(false);
            
            // Assert
            result.Count().ShouldBe(1);
        }

        [Fact]
        public async Task Should_Get_Tasks_Only_With_Given_Tag_And_Status_With_Created_Date_Higher_Than_Given()
        {
            // Arrange
            const string tag = "RetrieveMe";
            const TaskStatus status = TaskStatus.WaitingForActivation;
            Expression<Action<TestOperator>> expression1 = x => x.VoidMethod();
            var task1 = TaskInfo.FromTaskInfoResponse(expression1.ToTaskInfo(), Storage.None);
            
            Expression<Action<TestOperator>> expression2 = x => x.ResultMethod();
            var task2 = TaskInfo.FromTaskInfoResponse(expression2.ToTaskInfo(new[]{tag}), Storage.None);
            task2.Status = status;
            
            await _context.Tasks.AddRangeAsync(task1, task2).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            task1.DateCreated = DateTime.Now.AddYears(-5);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.GetTasks(DateTime.Now.AddDays(-1), tag, status).ConfigureAwait(false);
            
            // Assert
            result.Count().ShouldBe(1);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
