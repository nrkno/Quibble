using System.Linq;
using Xunit;

namespace Quibble.CSharp.UnitTests
{
    public class JsonStringsTests
    {
        [Fact]
        public void TestDiffTrueVsFalse()
        {
            var diffMessages = JsonStrings.Verify("true", "false");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Boolean value mismatch at $.\nExpected false but was true.", diffMessage);
        }

        [Fact]
        public void TestDiffFalseVsTrue()
        {
            var diffMessages = JsonStrings.Verify("false", "true");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Boolean value mismatch at $.\nExpected true but was false.", diffMessage);
        }
        
        [Fact]
        public void TestDiffNullVsFalse()
        {
            var diffMessages = JsonStrings.Verify("null", "false");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the boolean false but was null.", diffMessage);
        }
        
        [Fact]
        public void TestDiffFalseVsNull()
        {
            var diffMessages = JsonStrings.Verify("false", "null");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected null but was the boolean false.", diffMessage);
        }
        
        [Fact]
        public void TestDiffFalseVsZero()
        {
            var diffMessages = JsonStrings.Verify("false", "0");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the number 0 but was the boolean false.", diffMessage);
        }

        [Fact]
        public void TestDiffOneVsTrue()
        {
            var diffMessages = JsonStrings.Verify("1", "true");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the boolean true but was the number 1.", diffMessage);
        }
        
        [Fact]
        public void TestDiffEmptyArrayVsOne()
        {
            var diffMessages = JsonStrings.Verify("[]", "1");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the number 1 but was an empty array.", diffMessage);
        }
        
        [Fact]
        public void TestDiffArrayOfOneElementVsOne()
        {
            var diffMessages = JsonStrings.Verify("[ 1 ]", "1");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the number 1 but was an array with 1 item.", diffMessage);
        }
    }
}