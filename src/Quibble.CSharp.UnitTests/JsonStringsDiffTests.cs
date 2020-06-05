using System;
using System.Collections.Generic;
using System.Globalization;
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
            var diffs = JsonStrings.Diff(str1, str2);

            Assert.Equal(2, diffs.Count);
            var diff1 = diffs[0];
            Assert.True(diff1.IsProperties);
            var propsDiff = (Properties) diff1;
            Assert.Equal("$.books[0]", propsDiff.Path);
            var mismatch = propsDiff.Single();
            Assert.Equal("edition", mismatch.PropertyName);
            var propertyValue = ((String) mismatch.PropertyValue).Text;
            Assert.Equal("2nd", propertyValue);

            
            var diff2 = diffs[1];
            Assert.True(diff2.IsValue);
            Assert.Equal("$.books[1].author", diff2.Path);
            Assert.Equal("Leo Brodie", ((String) diff2.Left).Text);
            Assert.Equal("Chuck Moore", ((String) diff2.Right).Text);
        }

        [Fact]
        public void NumberExample1NotEqualTo2()
        {
            var actualDiffs = JsonStrings.Diff("1", "2");

            var expectedDiffs = new List<Diff>
            {
                new Value(
                    new DiffPoint("$",
                        new Number(1, "1"),
                        new Number(2, "2")))
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }
        
        [Fact]
        public void ArrayExampleNumberOfItems()
        {
            var actualDiffs = JsonStrings.Diff("[ 3 ]", "[ 3, 7 ]");

            var expectedDiffs = new List<Diff>
            {
                new ItemCount(
                    new DiffPoint("$",
                        new Array (
                            new JsonValue []
                            {
                                new Number(3, "3")
                            }),
                        new Array (new JsonValue []
                        {
                            new Number(3, "3"),
                            new Number(7, "7")
                        })))
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
            var actualDiffs = JsonStrings.Diff("[ 24, 12 ]", "[ 12, 24 ]");

            var expectedDiffs = new List<Diff>
            {
                new Value(
                    new DiffPoint("$[0]",
                        new Number(24, "24"), 
                        new Number(12, "12"))), 
                new Value(
                    new DiffPoint("$[1]",
                        new Number(12, "12"), 
                        new Number(24, "24")))
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }
        
        /*
         *    [<Fact>]
    let ``Array example: order matters`` () =
        let actualDiffs =
            JsonStrings.diff "[ 24, 12 ]" "[ 12, 24 ]"

        let expectedDiffs =
            [ Value
                { Path = "$[0]"
                  Left = Number(24., "24")
                  Right = Number(12., "12") }
              Value
                  { Path = "$[1]"
                    Left = Number(12., "12")
                    Right = Number(24., "24") } ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual)) 
         */
        
        [Fact]
        public void TestUndefined()
        {
            var jv = (JsonValue) Undefined.Instance;
            Assert.True(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(Undefined.Instance, Undefined.Instance);

            var set = new HashSet<JsonValue> {Undefined.Instance, Undefined.Instance};

            Assert.Single(set);
            
            Assert.Equal("undefined", jv.ToString());
        }
        
        [Fact]
        public void TestNull()
        {
            var jv = (JsonValue) Null.Instance;
            Assert.False(jv.IsUndefined);
            Assert.True(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(Null.Instance, Null.Instance);

            var set = new HashSet<JsonValue> {Null.Instance, Null.Instance};

            Assert.Single(set);

            Assert.Equal("null", jv.ToString());
        }
        
        [Fact]
        public void TestTrue()
        {
            var jv = (JsonValue) True.Instance;
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.True(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(True.Instance, True.Instance);

            var set = new HashSet<JsonValue> {True.Instance, True.Instance};

            Assert.Single(set);

            Assert.Equal("true", jv.ToString());
        }
        
        [Fact]
        public void TestFalse()
        {
            var jv = (JsonValue) False.Instance;
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.True(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(False.Instance, False.Instance);

            var set = new HashSet<JsonValue> {False.Instance, False.Instance};

            Assert.Single(set);
            
            Assert.Equal("false", jv.ToString());
        }
        
        [Fact]
        public void TestNumber()
        {
            var jv = (JsonValue) new Number(1, "1");
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.True(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            // Numbers with same numeric value and same text representation are equal.
            Assert.Equal(
                new Number(1, "1"), 
                new Number(1, "1"));

            Assert.Equal(
                new Number(123.4, "123.4"), 
                new Number(123.4, "123.4"));

            // Numbers with same numeric value and different text representation are *also* equal.
            Assert.Equal(
                new Number(1, "1"), 
                new Number(1, "1.0"));

            Assert.Equal(
                new Number(123.4, "123.4"), 
                new Number(123.4, "1.234E2"));

            // Numbers with different numeric value are obviously not equal.
            Assert.NotEqual(
                new Number(1, "1.0"), 
                new Number(1.001, "1.001"));

            Assert.NotEqual(
                new Number(123.4, "123.4"), 
                new Number(1.234, "1.234"));

            var set = new HashSet<JsonValue>
            {
                new Number(1, "1"), 
                new Number(1, "1.0"),        
                new Number(1.001, "1.001")            
            };

            Assert.Equal(2, set.Count);
            
            var num = new Number(123.4, "1.234E2");

            Assert.Equal(System.String.Format(NumberFormatInfo.InvariantInfo, "{0} ({1})", num.NumericValue, num.TextRepresentation), 
                num.ToString());
        }
        
        [Fact]
        public void TestString()
        {
            var jv = (JsonValue) new String("name");
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.True(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(new String("name"), new String("name"));

            Assert.NotEqual(new String("name"), new String("game"));

            var set = new HashSet<JsonValue>
            {
                new String("name"),
                new String("game"),
                new String("same"),
                new String("name"),
            };

            Assert.Equal(3, set.Count);
            
            var s = new String("name");
            Assert.Equal(s.Text, jv.ToString());
        }
        
        [Fact]
        public void TestArray()
        {
            var items = new List<JsonValue>
            {
                True.Instance,
                new String("name"),
                new Number(1, "1")
            };
            
            var jv = (JsonValue) new Array(items);
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.True(jv.IsArray);
            Assert.False(jv.IsObject);

            Assert.Equal(new Array(items), new Array(items));

            var items2 = new List<JsonValue>
            {
                True.Instance,
                new String("name"),
                new Number(1, "1")
            };
            
            Assert.Equal(new Array(items), new Array(items2));
            
            var items3 = new List<JsonValue>
            {
                True.Instance,
                new String("game"),
                new Number(1, "1")
            };

            Assert.NotEqual(new Array(items), new Array(items3));
            
            var set = new HashSet<JsonValue>
            {
                new Array(items), 
                new Array(items2), 
                new Array(items3), 
            };

            Assert.Equal(2, set.Count);

            var arr = new Array(items);
            Assert.Equal($"Array [{items.Count} items]", arr.ToString());
        }
        
        [Fact]
        public void TestObject()
        {
            var props = new Dictionary<string, JsonValue>
            {
                {
                    "active", True.Instance
                },
                {
                    "name", new String("Widget")
                },
                {
                    "amount", new Number(1, "1.0")
                }
            };
            
            var jv = (JsonValue) new Object(props);
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.True(jv.IsObject);

            Assert.Equal(new Object(props), new Object(props));

            var sameProps = new Dictionary<string, JsonValue>
            {
                {
                    "active", True.Instance
                },
                {
                    "name", new String("Widget")
                },
                {
                    "amount", new Number(1, "1.0")
                }
            };
            
            Assert.Equal(new Object(props), new Object(sameProps));
            
            var notQuiteTheSameProps = new Dictionary<string, JsonValue>
            {
                {
                    "active", True.Instance
                },
                {
                    "name", new String("Gizmo")
                },
                {
                    "amount", new Number(1, "1.0")
                }
            };

            Assert.NotEqual(new Object(props), new Object(notQuiteTheSameProps));
            
            var set = new HashSet<JsonValue>
            {
                new Object(props), 
                new Object(sameProps), 
                new Object(notQuiteTheSameProps), 
            };

            Assert.Equal(2, set.Count);

            var obj = new Object(props);
            Assert.Equal($"Object {{{props.Count} properties}}", obj.ToString());
        }
        
        [Fact]
        public void TestTypeDiff()
        {
            var str1 = "true";
            var str2 = "\"true\"";

            var diff1 = JsonStrings.Diff(str1, str2).Single();
            Assert.True(diff1.IsType);
            Assert.False(diff1.IsValue);
            Assert.False(diff1.IsItemCount);
            Assert.False(diff1.IsProperties);

            var diff2 = new Type(
                new DiffPoint("$",
                    True.Instance,
                    new String("true")));
            Assert.True(diff2.IsType);
            Assert.False(diff2.IsValue);
            Assert.False(diff2.IsItemCount);
            Assert.False(diff2.IsProperties);
            
            Assert.Equal(diff1, diff2);
        }

        [Fact]
        public void TestValueDiff()
        {
            var str1 = @"{
    ""title"": ""Thinking Forth"",
    ""author"": ""Leo Brodie""
}";
            var str2 = @"{
    ""title"": ""Thinking Forth"",
    ""author"": ""Chuck Moore""
}";

            var diff1 = JsonStrings.Diff(str1, str2).Single();
            Assert.True(diff1.IsValue);
            Assert.False(diff1.IsType);
            Assert.False(diff1.IsItemCount);
            Assert.False(diff1.IsProperties);

            var diff2 = new Value(
                new DiffPoint("$.author",
                    new String("Leo Brodie"),
                    new String("Chuck Moore")));
            Assert.True(diff2.IsValue);
            Assert.False(diff2.IsType);
            Assert.False(diff2.IsItemCount);
            Assert.False(diff2.IsProperties);

            Assert.Equal(diff1, diff2);
        }
        
        [Fact]
        public void TestItemCountDiff()
        {
            var str1 = @"[ ""Adele Goldberg"", ""Dan Ingalls"", ""Alan Kay"" ]";
            var str2 = @"[ ""Adele Goldberg"", ""Dan Ingalls"" ]";

            var diff1 = JsonStrings.Diff(str1, str2).Single();
            Assert.True(diff1.IsItemCount);
            Assert.False(diff1.IsType);
            Assert.False(diff1.IsValue);
            Assert.False(diff1.IsProperties);

            var diff2 = new ItemCount(
                new DiffPoint("$",
                    new Array(
                        new JsonValue[]
                        {
                            new String("Adele Goldberg"),
                            new String("Dan Ingalls"),
                            new String("Alan Kay")
                        }),
                    new Array(
                        new JsonValue[]
                        {
                            new String("Adele Goldberg"),
                            new String("Dan Ingalls")
                        })));
            Assert.True(diff2.IsItemCount);
            Assert.False(diff2.IsType);
            Assert.False(diff2.IsValue);
            Assert.False(diff2.IsProperties);
            
            Assert.Equal(diff1, diff2);
        }
    }
}