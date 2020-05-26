using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Quibble.CSharp.UnitTests
{
    public class JsonDiffTests
    {
        [Fact]
        public void TestOfElementsTrueVsFalse()
        {
            using var d1 = JsonDocument.Parse("true");
            using var d2 = JsonDocument.Parse("false");
            var e1 = d1.RootElement;
            var e2 = d2.RootElement;
            var diffs = JsonDiff.OfElements(e1, e2).ToList();
            Assert.NotEmpty(diffs);
            var kindDiff = (Diff.Kind) diffs.Single();
            Assert.Equal("$", kindDiff.Item.Path);
            Assert.True(kindDiff.Item.Left.GetBoolean());
            Assert.False(kindDiff.Item.Right.GetBoolean());
        }

        [Fact]
        public void TestOfDocumentsTrueVsFalse()
        {
            using var d1 = JsonDocument.Parse("true");
            using var d2 = JsonDocument.Parse("false");
            var diffs = JsonDiff.OfDocuments(d1, d2).ToList();
            Assert.NotEmpty(diffs);
            var kindDiff = (Diff.Kind) diffs.Single();
            Assert.Equal("$", kindDiff.Item.Path);
            Assert.True(kindDiff.Item.Left.GetBoolean());
            Assert.False(kindDiff.Item.Right.GetBoolean());
        }

        [Fact]
        public void TestOfDocuments1Vs2()
        {
            using var d1 = JsonDocument.Parse("1");
            using var d2 = JsonDocument.Parse("2");
            var diffs = JsonDiff.OfDocuments(d1, d2).ToList();
            Assert.NotEmpty(diffs);
            var valueDiff = (Diff.Value)diffs.Single();
            Assert.Equal("$", valueDiff.Item.Path);
            Assert.Equal(1, valueDiff.Item.Left.GetInt32());
            Assert.Equal(2, valueDiff.Item.Right.GetInt32());
        }
        
        [Fact]
        public void TestOfExtensionMethod()
        {
            using var d1 = JsonDocument.Parse("true");
            using var d2 = JsonDocument.Parse("false");
            var e1 = d1.RootElement;
            var e2 = d2.RootElement;
            var diffs = e1.Diff(e2);
            var kindDiff = (Diff.Kind) diffs.Single();
            Assert.Equal("$", kindDiff.Item.Path);
            Assert.True(kindDiff.Item.Left.GetBoolean());
            Assert.False(kindDiff.Item.Right.GetBoolean());
        }
    }
}
