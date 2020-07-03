using System;
using System.Collections.Generic;
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

        [Fact]
        public void WidgetPriceExample()
        {
            var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
            var str2 = @"{ ""item"": ""widget"", ""quantity"": 88, ""in stock"": true }";
            var diffs = JsonStrings.TextDiff(str1, str2);
            var diff = diffs.Single();
            Assert.Equal("Object difference at $.\nLeft only property: 'price' (number).\nRight only properties: 'quantity' (number), 'in stock' (bool).", diff);
        }

        [Fact]
        public void BookExample()
        {
            var str1 = @"{
    ""books"": [{
        ""title"": ""Data and Reality"",  
        ""author"": ""William Kent""
    }, {
        ""title"": ""Thinking Forth"",
        ""author"": ""Leo Brodie""
    }]
}";
            var str2 = @"{
    ""books"": [{
        ""title"": ""Data and Reality"",
        ""author"": ""William Kent"",
        ""edition"": ""2nd""
    }, {
        ""title"": ""Thinking Forth"",
        ""author"": ""Chuck Moore""
    }]
}";
            var diffs = JsonStrings.TextDiff(str1, str2);

            Assert.Equal("Object difference at $.books[0].\nRight only property: 'edition' (string).", diffs[0]);
            Assert.Equal("String value difference at $.books[1].author: Leo Brodie vs Chuck Moore.", diffs[1]);
        }
        
        [Fact]
        public void ArrayExampleNumberOfItems()
        {
            var actualDiffs = JsonStrings.TextDiff("[ 3 ]", "[ 3, 7 ]");

            var expectedDiffs = new List<string>
            {
                "Array difference at $.\n + [1] (the number 7)"
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }
        
        [Fact]
        public void ArrayExampleOrderMatters()
        {
            var actualDiffs = JsonStrings.TextDiff("[ 24, 12 ]", "[ 12, 24 ]");

            var expectedDiffs = new List<string>
            {
                "Array difference at $.\n - [0] (the number 24)\n + [1] (the number 24)"
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }
    }
}