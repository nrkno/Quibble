using System.Linq;
using Xunit;

namespace Quibble.CSharp.UnitTests
{
    public class JsonStringsTextDiffTests
    {
        [Fact]
        public void TestDiffTrueVsFalse()
        {
            // In JSON there is no boolean type, but we pretend there is to provide a better message.
            var diffs = JsonStrings.TextDiff("true", "false");
            var diff = diffs.Single();
            Assert.Equal("Boolean value difference at $: true vs false.", diff);
        }

        [Fact]
        public void TestDiffFalseVsTrue()
        {
            // In JSON there is no boolean type, but we pretend there is to provide a better message.
            var diffs = JsonStrings.TextDiff("false", "true");
            var diff = diffs.Single();
            Assert.Equal("Boolean value difference at $: false vs true.", diff);
        }
        
        [Fact]
        public void TestDiffNullVsFalse()
        {
            var diffs = JsonStrings.TextDiff("null", "false");
            var diff = diffs.Single();
            Assert.Equal("Type difference at $: null vs the boolean false.", diff);
        }
        
        [Fact]
        public void TestDiffFalseVsNull()
        {
            var diffs = JsonStrings.TextDiff("false", "null");
            var diff = diffs.Single();
            Assert.Equal("Type difference at $: the boolean false vs null.", diff);
        }
        
        [Fact]
        public void TestDiffFalseVsZero()
        {
            var diffs = JsonStrings.TextDiff("false", "0");
            var diff = diffs.Single();
            Assert.Equal("Type difference at $: the boolean false vs the number 0.", diff);
        }

        [Fact]
        public void TestDiffOneVsTrue()
        {
            var diffs = JsonStrings.TextDiff("1.42", "true");
            var diff = diffs.Single();
            Assert.Equal("Type difference at $: the number 1.42 vs the boolean true.", diff);
        }
        
        [Fact]
        public void TestDiffEmptyArrayVsOne()
        {
            var diffs = JsonStrings.TextDiff("[]", "1");
            var diff = diffs.Single();
            Assert.Equal("Type difference at $: an empty array vs the number 1.", diff);
        }
        
        [Fact]
        public void TestDiffArrayOfOneElementVsOne()
        {
            var diffs = JsonStrings.TextDiff("[ 1 ]", "1");
            var diff = diffs.Single();
            Assert.Equal("Type difference at $: an array with 1 item vs the number 1.", diff);
        }
    }
}