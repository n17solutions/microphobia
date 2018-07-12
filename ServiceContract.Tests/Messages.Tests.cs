using System;
using System.Globalization;
using System.Threading;
using Humanizer;
using N17Solutions.Microphobia.ServiceContract.Models;
using Shouldly;
using Xunit;

namespace N17Solutions.Microphobia.ServiceContract.Tests
{
    public class MessagesTests
    {
        private readonly TaskInfo _taskInfo = new TaskInfo
        {
            Id = Guid.NewGuid(),
            AssemblyName = "Test.Assembly",
            MethodName = "TestMethod"
        };

        [Fact]
        public void Should_Get_TaskException_Message()
        {
            // Act
            var message = Messages.TaskThrewException(_taskInfo);

            // Assert
            message.ShouldNotBeNull();
            message.ShouldNotBeEmpty();
        }

        [Fact]
        public void Should_Get_Task_Started_Message()
        {
            // Act
            var message = Messages.TaskStarted(_taskInfo);

            // Assert
            message.ShouldNotBeNull();
            message.ShouldNotBeEmpty();
        }

        [Fact]
        public void Should_Get_Task_Finished_Message()
        {
            // Arrange
            var timespan = DateTime.Now.TimeOfDay;

            // Act
            var message = Messages.TaskFinished(_taskInfo, timespan);

            // Assert
            message.ShouldNotBeNull();
            message.ShouldNotBeEmpty();
            message.ShouldContain(timespan.Humanize(3));
        }

        [Fact]
        public void Should_Fall_Back_When_Culture_Has_No_Message()
        {
            // Arrange
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-ZU");

            // Act
            var message = Messages.TaskStarted(_taskInfo);

            // Assert
            message.ShouldNotBeNull();
            message.ShouldNotBeEmpty();
        }
    }
}
