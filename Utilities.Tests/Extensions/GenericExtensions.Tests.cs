using N17Solutions.Microphobia.Utilities.Extensions;
using Shouldly;
using Xunit;

namespace N17Solutions.Microphobia.Utilities.Tests.Extensions
{
    public class GenericExtensionsTests
    {
        private object _testObject;
        [Fact]
        public void Should_Return_True_If_Value_Is_Default()
        {
            // Arrange
            _testObject = default(int);

            // Assert
            _testObject.IsDefault().ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_False_If_Value_Is_Not_Default()
        {
            // Arrange
            _testObject = 7;

            // Assert
            _testObject.IsDefault().ShouldBeFalse();
        }
    }
}