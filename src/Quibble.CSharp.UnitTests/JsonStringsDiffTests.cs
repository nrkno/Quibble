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
            Assert.True(diff.IsTypeDiff);
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
            Assert.True(diff.IsTypeDiff);
            Assert.True(diff.Left.IsFalse);
            Assert.True(diff.Right.IsTrue);
        }
        
        [Fact]
        public void TestDiffNullVsFalse()
        {
            var diffs = JsonStrings.Diff("null", "false");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsTypeDiff);
            Assert.True(diff.Left.IsNull);
            Assert.True(diff.Right.IsFalse);
        }
        
        [Fact]
        public void TestDiffFalseVsNull()
        {
            var diffs = JsonStrings.Diff("false", "null");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsTypeDiff);
            Assert.True(diff.Left.IsFalse);
            Assert.True(diff.Right.IsNull);
        }
        
        [Fact]
        public void TestDiffFalseVsZero()
        {
            var diffs = JsonStrings.Diff("false", "0");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsTypeDiff);
            Assert.True(diff.Left.IsFalse);
            Assert.True(diff.Right.IsNumber);
        }

        [Fact]
        public void TestDiffOneVsTrue()
        {
            var diffs = JsonStrings.Diff("1", "true");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsTypeDiff);
            Assert.True(diff.Left.IsNumber);
            Assert.True(diff.Right.IsTrue);
        }
        
        [Fact]
        public void TestDiffEmptyArrayVsOne()
        {
            var diffs = JsonStrings.Diff("[]", "1");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsTypeDiff);
            Assert.True(diff.Left.IsArray);
            Assert.True(diff.Right.IsNumber);
        }
        
        [Fact]
        public void TestDiffArrayOfOneElementVsOne()
        {
            var diffs = JsonStrings.Diff("[ 1 ]", "1");
            var diff = diffs.Single();
            Assert.Equal("$", diff.Path);
            Assert.True(diff.IsTypeDiff);
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
            Assert.True(diff1.IsObjectDiff);
            var objectDiff = (ObjectDiff) diff1;
            Assert.Equal("$.books[0]", objectDiff.Path);
            var mismatch = objectDiff.Mismatches.Single();
            Assert.Equal("edition", mismatch.PropertyName);
            var propertyValue = ((JsonString) mismatch.PropertyValue).Text;
            Assert.Equal("2nd", propertyValue);

            
            var diff2 = diffs[1];
            Assert.True(diff2.IsValueDiff);
            Assert.Equal("$.books[1].author", diff2.Path);
            Assert.Equal("Leo Brodie", ((JsonString) diff2.Left).Text);
            Assert.Equal("Chuck Moore", ((JsonString) diff2.Right).Text);
        }

        [Fact]
        public void NumberExample1NotEqualTo2()
        {
            var actualDiffs = JsonStrings.Diff("1", "2");

            var expectedDiffs = new List<Diff>
            {
                new ValueDiff(
                    new DiffPoint("$",
                        new JsonNumber(1, "1"),
                        new JsonNumber(2, "2")))
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
                new ArrayDiff(
                    new DiffPoint("$",
                        new JsonArray (
                            new JsonValue []
                            {
                                new JsonNumber(3, "3")
                            }),
                        new JsonArray (new JsonValue []
                        {
                            new JsonNumber(3, "3"),
                            new JsonNumber(7, "7")
                        })), new ItemMismatch[]
                    {
                        new RightOnlyItem(1, new JsonNumber(7, "7")), 
                    })
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
                new ArrayDiff(new DiffPoint("$", 
                        new JsonArray (new JsonValue []
                        {
                            new JsonNumber(24, "24"),
                            new JsonNumber(12, "12")
                        }), 
                        new JsonArray (new JsonValue []
                        {
                            new JsonNumber(12, "12"),
                            new JsonNumber(24, "24")
                        })),
                    new ItemMismatch[]
                    {
                        new LeftOnlyItem(0, new JsonNumber(24, "24")),
                        new RightOnlyItem(1, new JsonNumber(24, "24"))
                    })
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void ArrayExampleMoreItems()
        {
            var str1 = @"[{
    ""title"": ""Data and Reality"",
    ""author"": ""William Kent""
}, {
    ""title"": ""Thinking Forth"",
    ""author"": ""Leo Brodie""
}, {
    ""title"": ""Programmers at Work"",
    ""author"": ""Susan Lammers""
}, {
    ""title"": ""The Little Schemer"",
    ""authors"": [ ""Daniel P. Friedman"", ""Matthias Felleisen"" ]
}, {
    ""title"": ""Object Design"",
    ""authors"": [ ""Rebecca Wirfs-Brock"", ""Alan McKean"" ]
}, {
    ""title"": ""Domain Modelling made Functional"",
    ""author"": ""Scott Wlaschin""
}, {
    ""title"": ""The Psychology of Computer Programming"",
    ""author"": ""Gerald M. Weinberg""
}, {
    ""title"": ""Exercises in Programming Style"",
    ""author"": ""Cristina Videira Lopes""
}, {
    ""title"": ""Land of Lisp"",
    ""author"": ""Conrad Barski""
}]";
            var str2 = @"[{
    ""title"": ""Data and Reality"",
    ""author"": ""William Kent""
}, {
    ""title"": ""Thinking Forth"",
    ""author"": ""Leo Brodie""
}, {
    ""title"": ""Coders at Work"",
    ""author"": ""Peter Seibel""
}, {
    ""title"": ""The Little Schemer"",
    ""authors"": [ ""Daniel P. Friedman"", ""Matthias Felleisen"" ]
}, {
    ""title"": ""Object Design"",
    ""authors"": [ ""Rebecca Wirfs-Brock"", ""Alan McKean"" ]
}, {
    ""title"": ""Domain Modelling made Functional"",
    ""author"": ""Scott Wlaschin""
}, {
    ""title"": ""The Psychology of Computer Programming"",
    ""author"": ""Gerald M. Weinberg""
}, {
    ""title"": ""Turtle Geometry"",
    ""authors"": [ ""Hal Abelson"", ""Andrea diSessa"" ]
}, {
    ""title"": ""Exercises in Programming Style"",
    ""author"": ""Cristina Videira Lopes""
}, {
    ""title"": ""Land of Lisp"",
    ""author"": ""Conrad Barski""
}]";
            var actualDiffs = JsonStrings.Diff(str1, str2);
            var expectedDiffs = new List<Diff>
            {
                new ArrayDiff(new DiffPoint("$",
                        new JsonArray(new JsonValue[]
                        {
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Data and Reality")},
                                    {"author", new JsonString("William Kent")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Thinking Forth")},
                                    {"author", new JsonString("Leo Brodie")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Programmers at Work")},
                                    {"author", new JsonString("Susan Lammers")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("The Little Schemer")},
                                    {
                                        "authors",
                                        new JsonArray(new JsonValue[]
                                            {new JsonString("Daniel P. Friedman"), new JsonString("Matthias Felleisen")})
                                    }
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Object Design")},
                                    {
                                        "authors",
                                        new JsonArray(new JsonValue[]
                                            {new JsonString("Rebecca Wirfs-Brock"), new JsonString("Alan McKean")})
                                    }
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Domain Modelling made Functional")},
                                    {"author", new JsonString("Scott Wlaschin")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("The Psychology of Computer Programming")},
                                    {"author", new JsonString("Gerald M. Weinberg")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Exercises in Programming Style")},
                                    {"author", new JsonString("Cristina Videira Lopes")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Land of Lisp")},
                                    {"author", new JsonString("Conrad Barski")}
                                })
                        }),
                        new JsonArray(new JsonValue[]
                        {
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Data and Reality")},
                                    {"author", new JsonString("William Kent")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Thinking Forth")},
                                    {"author", new JsonString("Leo Brodie")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Coders at Work")},
                                    {"author", new JsonString("Peter Seibel")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("The Little Schemer")},
                                    {
                                        "authors",
                                        new JsonArray(new JsonValue[]
                                            {new JsonString("Daniel P. Friedman"), new JsonString("Matthias Felleisen")})
                                    }
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Object Design")},
                                    {
                                        "authors",
                                        new JsonArray(new JsonValue[]
                                            {new JsonString("Rebecca Wirfs-Brock"), new JsonString("Alan McKean")})
                                    }
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Domain Modelling made Functional")},
                                    {"author", new JsonString("Scott Wlaschin")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("The Psychology of Computer Programming")},
                                    {"author", new JsonString("Gerald M. Weinberg")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Turtle Geometry")},
                                    {
                                        "authors",
                                        new JsonArray(new JsonValue[]
                                            {new JsonString("Hal Abelson"), new JsonString("Andrea diSessa")})
                                    }
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Exercises in Programming Style")},
                                    {"author", new JsonString("Cristina Videira Lopes")}
                                }),
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Land of Lisp")},
                                    {"author", new JsonString("Conrad Barski")}
                                })
                        })),
                    new ItemMismatch[]
                    {
                        new LeftOnlyItem(2,
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Programmers at Work")},
                                    {"author", new JsonString("Susan Lammers")}
                                })),
                        new RightOnlyItem(2,
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Coders at Work")},
                                    {"author", new JsonString("Peter Seibel")}
                                })),
                        new RightOnlyItem(7,
                            new JsonObject(
                                new Dictionary<string, JsonValue>
                                {
                                    {"title", new JsonString("Turtle Geometry")},
                                    {
                                        "authors",
                                        new JsonArray(new JsonValue[]
                                            {new JsonString("Hal Abelson"), new JsonString("Andrea diSessa")})
                                    }
                                }))
                    })
            };

            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void ObjectExamplePropertyDifferences()
        {
            var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
            var str2 = @"{ ""item"": ""widget"", ""quantity"": 88, ""in stock"": true }";
            var actualDiffs = JsonStrings.Diff(str1, str2);

            var expectedDiffs = new List<Diff>
            {
                new ObjectDiff(
                    new DiffPoint("$",
                        new JsonObject(
                            new Dictionary<string, JsonValue>
                            {
                                { "item", new JsonString("widget") },
                                { "price", new JsonNumber(12.20, "12.20") }
                            }), 
                        new JsonObject(
                            new Dictionary<string, JsonValue>
                            {
                                { "item", new JsonString("widget") },
                                { "quantity", new JsonNumber(88, "88") },
                                { "in stock", JsonTrue.Instance }
                            })), 
                    new List<PropertyMismatch>
                    {
                        new LeftOnlyProperty("price", new JsonNumber(12.20, "12.20")),
                        new RightOnlyProperty("quantity", new JsonNumber(88, "88")),
                        new RightOnlyProperty("in stock", JsonTrue.Instance)
                    })
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }
        
        [Fact]
        public void ObjectExamplePropertyWithSpaces()
        {
            var str1 = @"{ ""name"": ""Maya"", ""date of birth"": ""1999-04-23"" }";
            var str2 = @"{ ""name"": ""Maya"", ""date of birth"": ""1999-04-24"" }";
            var actualDiffs = JsonStrings.Diff(str1, str2);

            var expectedDiffs = new List<Diff>
            {
                new ValueDiff(
                    new DiffPoint("$['date of birth']",
                        new JsonString("1999-04-23"), 
                        new JsonString("1999-04-24")))
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void CompositeExampleWithBookds()
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
            var actualDiffs = JsonStrings.Diff(str1, str2);

            var expectedDiffs = new List<Diff>
            {
                new ObjectDiff(
                    new DiffPoint("$.books[0]",
                        new JsonObject(
                            new Dictionary<string, JsonValue>
                            {
                                { "title", new JsonString("Data and Reality") },
                                { "author", new JsonString("William Kent") }
                            }), 
                        new JsonObject(
                            new Dictionary<string, JsonValue>
                            {
                                { "title", new JsonString("Data and Reality") },
                                { "author", new JsonString("William Kent") },
                                { "edition", new JsonString("2nd") }
                            })), 
                    new List<PropertyMismatch>
                    {
                        new RightOnlyProperty("edition", new JsonString("2nd"))
                    }),
                new ValueDiff(
                new DiffPoint("$.books[1].author",
                new JsonString("Leo Brodie"), 
                new JsonString("Chuck Moore")))
            };
            
            Assert.Equal(expectedDiffs.Count, actualDiffs.Count);

            foreach (var (expected, actual) in expectedDiffs.Zip(actualDiffs))
            {
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void TestUndefined()
        {
            var jv = (JsonValue) JsonUndefined.Instance;
            Assert.True(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(JsonUndefined.Instance, JsonUndefined.Instance);

            var set = new HashSet<JsonValue> {JsonUndefined.Instance, JsonUndefined.Instance};

            Assert.Single(set);
            
            Assert.Equal("undefined", jv.ToString());
        }
        
        [Fact]
        public void TestNull()
        {
            var jv = (JsonValue) JsonNull.Instance;
            Assert.False(jv.IsUndefined);
            Assert.True(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(JsonNull.Instance, JsonNull.Instance);

            var set = new HashSet<JsonValue> {JsonNull.Instance, JsonNull.Instance};

            Assert.Single(set);

            Assert.Equal("null", jv.ToString());
        }
        
        [Fact]
        public void TestTrue()
        {
            var jv = (JsonValue) JsonTrue.Instance;
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.True(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(JsonTrue.Instance, JsonTrue.Instance);

            var set = new HashSet<JsonValue> {JsonTrue.Instance, JsonTrue.Instance};

            Assert.Single(set);

            Assert.Equal("true", jv.ToString());
        }
        
        [Fact]
        public void TestFalse()
        {
            var jv = (JsonValue) JsonFalse.Instance;
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.True(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(JsonFalse.Instance, JsonFalse.Instance);

            var set = new HashSet<JsonValue> {JsonFalse.Instance, JsonFalse.Instance};

            Assert.Single(set);
            
            Assert.Equal("false", jv.ToString());
        }
        
        [Fact]
        public void TestNumber()
        {
            var jv = (JsonValue) new JsonNumber(1, "1");
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
                new JsonNumber(1, "1"), 
                new JsonNumber(1, "1"));

            Assert.Equal(
                new JsonNumber(123.4, "123.4"), 
                new JsonNumber(123.4, "123.4"));

            // Numbers with same numeric value and different text representation are *also* equal.
            Assert.Equal(
                new JsonNumber(1, "1"), 
                new JsonNumber(1, "1.0"));

            Assert.Equal(
                new JsonNumber(123.4, "123.4"), 
                new JsonNumber(123.4, "1.234E2"));

            // Numbers with different numeric value are obviously not equal.
            Assert.NotEqual(
                new JsonNumber(1, "1.0"), 
                new JsonNumber(1.001, "1.001"));

            Assert.NotEqual(
                new JsonNumber(123.4, "123.4"), 
                new JsonNumber(1.234, "1.234"));

            var set = new HashSet<JsonValue>
            {
                new JsonNumber(1, "1"), 
                new JsonNumber(1, "1.0"),        
                new JsonNumber(1.001, "1.001")            
            };

            Assert.Equal(2, set.Count);
            
            var num = new JsonNumber(123.4, "1.234E2");

            Assert.Equal(string.Format(NumberFormatInfo.InvariantInfo, "{0} ({1})", num.NumericValue, num.TextRepresentation), 
                num.ToString());
        }
        
        [Fact]
        public void TestNumberEquality()
        {
            var numberOne1 = new JsonNumber(1, "1");
            var numberOne2 = new JsonNumber(1, "1.0");
            var numberTwo = new JsonNumber(2, "2");
            
            Assert.Equal(numberOne1, numberOne2);
            Assert.NotEqual(numberOne1, numberTwo);
            
            Assert.Equal(numberOne1.GetHashCode(), numberOne2.GetHashCode());
            Assert.NotEqual(numberOne1.GetHashCode(), numberTwo.GetHashCode());
        }
        
        [Fact]
        public void TestString()
        {
            var jv = (JsonValue) new JsonString("name");
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.True(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.False(jv.IsObject);
            
            Assert.Equal(new JsonString("name"), new JsonString("name"));

            Assert.NotEqual(new JsonString("name"), new JsonString("game"));

            var set = new HashSet<JsonValue>
            {
                new JsonString("name"),
                new JsonString("game"),
                new JsonString("same"),
                new JsonString("name"),
            };

            Assert.Equal(3, set.Count);
            
            var s = new JsonString("name");
            Assert.Equal(s.Text, jv.ToString());
        }
        
        [Fact]
        public void TestStringEquality()
        {
            var nameStr1 = new JsonString("name");
            var nameStr2 = new JsonString("name");
            var gameStr = new JsonString("game");
            
            Assert.Equal(nameStr1, nameStr2);
            Assert.NotEqual(nameStr1, gameStr);
            
            Assert.Equal(nameStr1.GetHashCode(), nameStr2.GetHashCode());
            Assert.NotEqual(nameStr1.GetHashCode(), gameStr.GetHashCode());
        }
        
        [Fact]
        public void TestArray()
        {
            var items = new List<JsonValue>
            {
                JsonTrue.Instance,
                new JsonString("name"),
                new JsonNumber(1, "1")
            };
            
            var jv = (JsonValue) new JsonArray(items);
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.True(jv.IsArray);
            Assert.False(jv.IsObject);

            Assert.Equal(new JsonArray(items), new JsonArray(items));

            var items2 = new List<JsonValue>
            {
                JsonTrue.Instance,
                new JsonString("name"),
                new JsonNumber(1, "1")
            };
            
            Assert.Equal(new JsonArray(items), new JsonArray(items2));
            
            var items3 = new List<JsonValue>
            {
                JsonTrue.Instance,
                new JsonString("game"),
                new JsonNumber(1, "1")
            };

            Assert.NotEqual(new JsonArray(items), new JsonArray(items3));
            
            var set = new HashSet<JsonValue>
            {
                new JsonArray(items), 
                new JsonArray(items2), 
                new JsonArray(items3), 
            };

            Assert.Equal(2, set.Count);

            var arr = new JsonArray(items);
            Assert.Equal($"Array [{items.Count} items]", arr.ToString());
        }
        
        [Fact]
        public void TestArrayEquality()
        {
            var array = new JsonArray(
                new List<JsonValue>
                {
                    JsonTrue.Instance,
                    new JsonString("name"),
                });
            
            var sameArray = new JsonArray(
                new List<JsonValue>
                {
                    JsonTrue.Instance,
                    new JsonString("name"),
                });
            
            var anotherArray = new JsonArray(
                new List<JsonValue>
                {
                    JsonTrue.Instance,
                    new JsonString("game")
                });
            
            var yetAnotherArray = new JsonArray(
                new List<JsonValue>
                {
                    JsonTrue.Instance,
                    new JsonString("name"),
                    new JsonNumber(1, "1")
                });

            
            Assert.Equal(array, sameArray);
            Assert.NotEqual(array, anotherArray);
            Assert.NotEqual(array, yetAnotherArray);
            
            Assert.Equal(array.GetHashCode(), sameArray.GetHashCode());
            Assert.NotEqual(array.GetHashCode(), anotherArray.GetHashCode());
            Assert.NotEqual(array.GetHashCode(), yetAnotherArray.GetHashCode());
        }
        
        [Fact]
        public void TestObject()
        {
            var props = new Dictionary<string, JsonValue>
            {
                {
                    "active", JsonTrue.Instance
                },
                {
                    "name", new JsonString("Widget")
                },
                {
                    "amount", new JsonNumber(1, "1.0")
                }
            };
            
            var jv = (JsonValue) new JsonObject(props);
            Assert.False(jv.IsUndefined);
            Assert.False(jv.IsNull);
            Assert.False(jv.IsTrue);
            Assert.False(jv.IsFalse);
            Assert.False(jv.IsNumber);
            Assert.False(jv.IsString);
            Assert.False(jv.IsArray);
            Assert.True(jv.IsObject);

            Assert.Equal(new JsonObject(props), new JsonObject(props));

            var sameProps = new Dictionary<string, JsonValue>
            {
                {
                    "active", JsonTrue.Instance
                },
                {
                    "name", new JsonString("Widget")
                },
                {
                    "amount", new JsonNumber(1, "1.0")
                }
            };
            
            Assert.Equal(new JsonObject(props), new JsonObject(sameProps));
            
            var notQuiteTheSameProps = new Dictionary<string, JsonValue>
            {
                {
                    "active", JsonTrue.Instance
                },
                {
                    "name", new JsonString("Gizmo")
                },
                {
                    "amount", new JsonNumber(1, "1.0")
                }
            };

            Assert.NotEqual(new JsonObject(props), new JsonObject(notQuiteTheSameProps));
            
            var set = new HashSet<JsonValue>
            {
                new JsonObject(props), 
                new JsonObject(sameProps), 
                new JsonObject(notQuiteTheSameProps), 
            };

            Assert.Equal(2, set.Count);

            var obj = new JsonObject(props);
            Assert.Equal($"Object {{{props.Count} properties}}", obj.ToString());
        }
        
        [Fact]
        public void TestObjectEquality()
        {
            var obj = new JsonObject(
                new Dictionary<string, JsonValue>
                {
                    { "enabled", JsonTrue.Instance },
                    { "item", new JsonString("widget") }
                });
            
            var sameObj = new JsonObject(
                new Dictionary<string, JsonValue>
                {
                    { "enabled", JsonTrue.Instance },
                    { "item", new JsonString("widget") }
                });
            
            var anotherObj = new JsonObject(
                new Dictionary<string, JsonValue>
                {
                    { "enabled", JsonTrue.Instance },
                    { "item", new JsonString("gizmo") }
                });
            
            var yetAnotherObj = new JsonObject(
                new Dictionary<string, JsonValue>
                {
                    { "enabled", JsonTrue.Instance },
                    { "item", new JsonString("widget") },
                    { "count", new JsonNumber(1, "1") }
                });

            Assert.Equal(obj, sameObj);
            Assert.NotEqual(obj, anotherObj);
            Assert.NotEqual(obj, yetAnotherObj);
            
            Assert.Equal(obj.GetHashCode(), sameObj.GetHashCode());
            Assert.NotEqual(obj.GetHashCode(), anotherObj.GetHashCode());
            Assert.NotEqual(obj.GetHashCode(), yetAnotherObj.GetHashCode());
        }
        
        [Fact]
        public void TestTypeDiff()
        {
            var str1 = "true";
            var str2 = "\"true\"";

            var diff1 = JsonStrings.Diff(str1, str2).Single();
            Assert.True(diff1.IsTypeDiff);
            Assert.False(diff1.IsValueDiff);
            Assert.False(diff1.IsArrayDiff);
            Assert.False(diff1.IsObjectDiff);

            var diff2 = new TypeDiff(
                new DiffPoint("$",
                    JsonTrue.Instance,
                    new JsonString("true")));
            Assert.True(diff2.IsTypeDiff);
            Assert.False(diff2.IsValueDiff);
            Assert.False(diff2.IsArrayDiff);
            Assert.False(diff2.IsObjectDiff);
            
            Assert.Equal(diff1, diff2);
            
            Assert.Equal("Type { Path = $, Left = true, Right = true }", diff1.ToString());
        }
        
        [Fact]
        public void TestTypeDiffEquality()
        {
            var diff = new TypeDiff(
                new DiffPoint("$",
                    JsonTrue.Instance,
                    new JsonString("true")));

            var sameDiff = new TypeDiff(
                new DiffPoint("$",
                    JsonTrue.Instance,
                    new JsonString("true")));

            var anotherDiff = new TypeDiff(
                new DiffPoint("$.value",
                    JsonTrue.Instance,
                    new JsonString("true")));

            var yetAnotherDiff = new TypeDiff(
                new DiffPoint("$",
                    JsonTrue.Instance,
                    new JsonString("false")));

            Assert.Equal(diff, sameDiff);
            Assert.NotEqual(diff, anotherDiff);
            Assert.NotEqual(diff, yetAnotherDiff);
            Assert.NotEqual(anotherDiff, yetAnotherDiff);
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
            Assert.True(diff1.IsValueDiff);
            Assert.False(diff1.IsTypeDiff);
            Assert.False(diff1.IsArrayDiff);
            Assert.False(diff1.IsObjectDiff);

            var diff2 = new ValueDiff(
                new DiffPoint("$.author",
                    new JsonString("Leo Brodie"),
                    new JsonString("Chuck Moore")));
            Assert.True(diff2.IsValueDiff);
            Assert.False(diff2.IsTypeDiff);
            Assert.False(diff2.IsArrayDiff);
            Assert.False(diff2.IsObjectDiff);

            Assert.Equal(diff1, diff2);
            
            Assert.Equal("Value { Path = $.author, Left = Leo Brodie, Right = Chuck Moore }", diff1.ToString());
        }
        
        [Fact]
        public void TestValueDiffEquality()
        {
            var diff = new ValueDiff(
                new DiffPoint("$",
                    new JsonString("Hello"), 
                    new JsonString("Goodbye")));

            var sameDiff = new ValueDiff(
                new DiffPoint("$",
                    new JsonString("Hello"), 
                    new JsonString("Goodbye")));

            var anotherDiff = new ValueDiff(
                new DiffPoint("$.value",
                    new JsonString("Hello"), 
                    new JsonString("Goodbye")));

            var yetAnotherDiff = new ValueDiff(
                new DiffPoint("$",
                    new JsonString("Hey"), 
                    new JsonString("Goodbye")));

            Assert.Equal(diff, sameDiff);
            Assert.NotEqual(diff, anotherDiff);
            Assert.NotEqual(diff, yetAnotherDiff);
            Assert.NotEqual(anotherDiff, yetAnotherDiff);
        }
        
        [Fact]
        public void TestItemsDiff()
        {
            var str1 = @"[ ""Dan Ingalls"", ""Alan Kay"" ]";
            var str2 = @"[ ""Adele Goldberg"", ""Dan Ingalls"", ""Alan Kay"" ]";

            var diff1 = JsonStrings.Diff(str1, str2).Single();
            Assert.True(diff1.IsArrayDiff);
            Assert.False(diff1.IsTypeDiff);
            Assert.False(diff1.IsValueDiff);
            Assert.False(diff1.IsObjectDiff);

            var diff2 = new ArrayDiff(
                new DiffPoint("$",
                    new JsonArray(
                        new JsonValue[]
                        {
                            new JsonString("Dan Ingalls"),
                            new JsonString("Alan Kay")
                        }),
                    new JsonArray(
                        new JsonValue[]
                        {
                            new JsonString("Adele Goldberg"),
                            new JsonString("Dan Ingalls"),
                            new JsonString("Alan Kay")
                        })), new ItemMismatch[]
                {
                    new RightOnlyItem(0, new JsonString("Adele Goldberg"))
                });
            Assert.True(diff2.IsArrayDiff);
            Assert.False(diff2.IsTypeDiff);
            Assert.False(diff2.IsValueDiff);
            Assert.False(diff2.IsObjectDiff);
            
            Assert.Equal(diff1, diff2);
            
            Assert.Equal("Items { Path = $, Left = Array [2 items], Right = Array [3 items] }", diff1.ToString());
        }
        
        [Fact]
        public void TestItemsDiffEquality()
        {
            var diff = new ArrayDiff(
                new DiffPoint("$",
                    new JsonArray(new JsonValue[]
                    {
                        new JsonString("Gödel, Escher, Bach"),
                        new JsonString("Metamagical Themas")
                    }),
                    new JsonArray(new JsonValue[]
                    {
                        new JsonString("Gödel, Escher, Bach")
                    })),
                new ItemMismatch[]
                {
                    new LeftOnlyItem(1, new JsonString("Metamagical Themas")) 
                });

            var sameDiff = new ArrayDiff(
                new DiffPoint("$",
                    new JsonArray(new JsonValue[]
                    {
                        new JsonString("Gödel, Escher, Bach"),
                        new JsonString("Metamagical Themas")
                    }),
                    new JsonArray(new JsonValue[]
                    {
                        new JsonString("Gödel, Escher, Bach")
                    })),
                new ItemMismatch[]
                {
                    new LeftOnlyItem(1, new JsonString("Metamagical Themas")) 
                });

            var anotherDiff = new ArrayDiff(
                new DiffPoint("$.books",
                    new JsonArray(new JsonValue[]
                    {
                        new JsonString("Gödel, Escher, Bach"),
                        new JsonString("Metamagical Themas")
                    }),
                    new JsonArray(new JsonValue[]
                    {
                        new JsonString("Gödel, Escher, Bach")
                    })),
                new ItemMismatch[]
                {
                    new LeftOnlyItem(1, new JsonString("Metamagical Themas")) 
                });

            var yetAnotherDiff = 
                new ArrayDiff(
                    new DiffPoint("$",
                        new JsonArray(new JsonValue[]
                        {
                            new JsonString("Gödel, Escher, Bach"),
                            new JsonString("Metamagical Themas")
                        }),
                        new JsonArray(new JsonValue[]
                        {
                            new JsonString("Metamagical Themas")
                        })),
                    new ItemMismatch[]
                    {
                        new LeftOnlyItem(0, new JsonString("Gödel, Escher, Bach")) 
                    });

            Assert.Equal(diff, sameDiff);
            Assert.NotEqual(diff, anotherDiff);
            Assert.NotEqual(diff, yetAnotherDiff);
            Assert.NotEqual(anotherDiff, yetAnotherDiff);
        }
        
        [Fact]
        public void TestPropertiesDiffEquality()
        {
            var diff = new ObjectDiff(new DiffPoint("$",
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")}
                    }),
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")},
                        {"edition", new JsonString("2nd")}
                    })), new PropertyMismatch[]
                {
                    new RightOnlyProperty("edition", new JsonString("2nd"))
                }
            );

            var sameDiff = new ObjectDiff(new DiffPoint("$",
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")}
                    }),
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")},
                        {"edition", new JsonString("2nd")}
                    })), new PropertyMismatch[]
                {
                    new RightOnlyProperty("edition", new JsonString("2nd"))
                }
            );

            var anotherDiff = new ObjectDiff(new DiffPoint("$.books[0]",
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")}
                    }),
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")},
                        {"edition", new JsonString("2nd")}
                    })), new PropertyMismatch[]
                {
                    new RightOnlyProperty("edition", new JsonString("2nd"))
                }
            );

            var yetAnotherDiff = new ObjectDiff(new DiffPoint("$",
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")}
                    }),
                    new JsonObject(new Dictionary<string, JsonValue>
                    {
                        {"author", new JsonString("William Kent")},
                        {"title", new JsonString("Data and Reality")},
                        {"edition", new JsonString("3rd")}
                    })), new PropertyMismatch[]
                {
                    new RightOnlyProperty("edition", new JsonString("3rd"))
                }
            );

            Assert.Equal(diff, sameDiff);
            Assert.NotEqual(diff, anotherDiff);
            Assert.NotEqual((Diff) diff, anotherDiff);
            Assert.NotEqual((object) diff, anotherDiff);

            Assert.NotEqual(diff, yetAnotherDiff);
            Assert.NotEqual(anotherDiff, yetAnotherDiff);

            Assert.Equal("Properties { Path = $, Left = Object {2 properties}, Right = Object {3 properties} }", diff.ToString());
        }
    }
}