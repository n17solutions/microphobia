using System.Collections.Generic;
using System.Linq;
using N17Solutions.Microphobia.Utilities.Extensions;
using Shouldly;
using Xunit;

namespace N17Solutions.Microphobia.Utilities.Tests.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void Should_Return_True_When_Enumerable_Is_Null()
        {
            // Arrange
            IEnumerable<string> enumerable = null;

            // Act
            // ReSharper disable once ExpressionIsAlwaysNull
            var result = enumerable.IsNullOrEmpty();

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_True_When_Enumerable_Is_Empty()
        {
            // Arrange
            var enumerable = Enumerable.Empty<string>();

            // Act
            var result = enumerable.IsNullOrEmpty();

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_False_When_Not_Null_And_Not_Empty()
        {
            // Arrange
            var enumerable = Enumerable.Repeat("Value", 1);

            // Act
            var result = enumerable.IsNullOrEmpty();

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_Chunked_Collections()
        {
            // Arrange
            var collection = Enumerable.Range(1, 10);
            
            // Act
            var result = collection.Chunk(2).ToArray();
            
            // Assert
            result.Length.ShouldBe(5);
            result.ShouldAllBe(x => x.Count() == 2);
        }
    }
}