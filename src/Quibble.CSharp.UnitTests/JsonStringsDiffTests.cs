using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Quibble.CSharp.UnitTests
{
    public class JsonStringsDiffTests
    {
        [Fact]
        public void TestDiffEmptyStringVsTrue()
        {
            // Empty string is not valid JSON.
            var ex = Assert.Throws<ArgumentException>(() => JsonStrings.Diff("", "true"));
            Assert.Equal("leftJsonString", ex.ParamName);
            Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        }
        
        [Fact]
        public void TestDiffTrueVsEmptyString()
        {
            // Empty string is not valid JSON.
            var ex = Assert.Throws<ArgumentException>(() => JsonStrings.Diff("true", ""));
            Assert.Equal("rightJsonString", ex.ParamName);
            Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        }
        
        [Fact]
        public void TestDiffTrueVsFalse()
        {
            // In JSON, true and false are distinct types.
            var diffs = JsonStrings.Diff("true", "false");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsTrue);
            Assert.True(diff.Right.IsFalse);
        }

        [Fact]
        public void TestDiffFalseVsTrue()
        {
            // In JSON, true and false are distinct types.
            var diffs = JsonStrings.Diff("false", "true");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsFalse);
            Assert.True(diff.Right.IsTrue);
        }
        
        [Fact]
        public void TestDiffNullVsFalse()
        {
            var diffs = JsonStrings.Diff("null", "false");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsNull);
            Assert.True(diff.Right.IsFalse);
        }
        
        [Fact]
        public void TestDiffFalseVsNull()
        {
            var diffs = JsonStrings.Diff("false", "null");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsFalse);
            Assert.True(diff.Right.IsNull);
        }
        
        [Fact]
        public void TestDiffFalseVsZero()
        {
            var diffs = JsonStrings.Diff("false", "0");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsFalse);
            Assert.True(diff.Right.IsNumber);
        }

        [Fact]
        public void TestDiffOneVsTrue()
        {
            var diffs = JsonStrings.Diff("1", "true");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsNumber);
            Assert.True(diff.Right.IsTrue);
        }
        
        [Fact]
        public void TestDiffEmptyArrayVsOne()
        {
            var diffs = JsonStrings.Diff("[]", "1");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsArray);
            Assert.True(diff.Right.IsNumber);
        }
        
        [Fact]
        public void TestDiffArrayOfOneElementVsOne()
        {
            var diffs = JsonStrings.Diff("[ 1 ]", "1");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsType);
            Assert.True(diff.Left.IsArray);
            Assert.True(diff.Right.IsNumber);
        }
    }
}