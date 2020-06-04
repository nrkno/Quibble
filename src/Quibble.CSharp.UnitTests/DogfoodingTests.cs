using System.Linq;
using Xunit;

namespace Quibble.CSharp.UnitTests
{
    public class DogfoodingTests
    {
        [Fact]
        public void TestBookExample()
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
            var textDiffs1 = JsonStrings.TextDiff(str1, str2);
            var textDiffs2 = Dogfooding.TextDiff(str1, str2);
            
            Assert.Equal(textDiffs1.Count, textDiffs2.Count);

            foreach (var (textDiff1, textDiff2) in textDiffs1.Zip(textDiffs2))
            {
                Assert.Equal(textDiff1, textDiff2);
            }
        }
    }
}