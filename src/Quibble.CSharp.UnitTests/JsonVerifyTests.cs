using System.Linq;
using Xunit;

namespace Quibble.CSharp.UnitTests
{
    public class JsonVerifyTests
    {
        [Fact]
        public void TestDiffTrueVsFalse()
        {
            var diffMessages = JsonVerify.Diff("true", "false");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Boolean value mismatch at $.\nExpected false but was true.", diffMessage);
        }

        [Fact]
        public void TestDiffFalseVsTrue()
        {
            var diffMessages = JsonVerify.Diff("false", "true");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Boolean value mismatch at $.\nExpected true but was false.", diffMessage);
        }
        
        [Fact]
        public void TestDiffNullVsFalse()
        {
            var diffMessages = JsonVerify.Diff("null", "false");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the boolean false but was null.", diffMessage);
        }
        
        [Fact]
        public void TestDiffFalseVsNull()
        {
            var diffMessages = JsonVerify.Diff("false", "null");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected null but was the boolean false.", diffMessage);
        }
        
        [Fact]
        public void TestDiffFalseVsZero()
        {
            var diffMessages = JsonVerify.Diff("false", "0");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the number 0 but was the boolean false.", diffMessage);
        }

        [Fact]
        public void TestDiffOneVsTrue()
        {
            var diffMessages = JsonVerify.Diff("1", "true");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the boolean true but was the number 1.", diffMessage);
        }
        
        [Fact]
        public void TestDiffEmptyArrayVsOne()
        {
            var diffMessages = JsonVerify.Diff("[]", "1");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the number 1 but was an empty array.", diffMessage);
        }
        
        [Fact]
        public void TestDiffArrayOfOneElementVsOne()
        {
            var diffMessages = JsonVerify.Diff("[ 1 ]", "1");
            var diffMessage = diffMessages.Single();
            Assert.Equal("Kind mismatch at $.\nExpected the number 1 but was an array with 1 item.", diffMessage);
        }
    }
}