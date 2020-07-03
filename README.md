[![Build status](https://ci.appveyor.com/api/projects/status/0v6946lhh480cgbk?svg=true)](https://ci.appveyor.com/project/NRKOpensource/quibble)
[![NuGet Status](https://img.shields.io/nuget/v/Quibble.svg?style=flat)](https://www.nuget.org/packages/Quibble/)

# Quibble

Quibble is a JSON diff tool for .NET. You give Quibble two text strings with JSON content and it will tell you what the differences are. 

Quibble distinguishes between four kinds of differences: 

* `Type`: e.g. `string` vs `number`.
* `Value`: same type but different value, e.g. the string `cat` vs the string `dog`.
* `Properties`: when two JSON objects have differences in their properties, e.g. the object `{ "name": "Quux" }` vs the object `{ "id": "1c3d" }`.
* `Items`: when two JSON arrays have a different items, e.g. the array `[ 1, 2, 3 ]` vs the array `[ 2, 3, 4 ]`.

Quibble makes the following assumptions: 

* Order of items matters for arrays.
* Order of properties does not matter for objects.
* Different ways of writing the same number do not matter.

## TL;DR

* [F# Examples](#f-examples).
* [C# Examples](#c-examples).


# Why Quibble?

Quibble is useful whenever you need to compare two JSON documents to see if and how they're different. Since JSON is pretty much everywhere these days, that's really a basic feature.

Quibble uses [JSONPath](https://goessner.net/articles/JsonPath/) syntax to point you to the right elements. In JSONPath syntax, `$` indicates the root of the document, whereas something like `$.books[1].author` means "the author property of the second element of the books array".

If you're using [xUnit.net](https://xunit.net/) to write tests, you may want to check out [Quibble.Xunit](https://github.com/nrkno/json-quibble-xunit), an extension to XUnit that does asserts on text strings with JSON content.


# F# Examples

```
open Quibble
```

Use `JsonStrings.diff` to get a list of `Diff`-values that you can map, filter and pattern match as you like.

Use `JsonStrings.textDiff` to get a list of text descriptions of the differences.

If you read the examples and wonder what `$` means, note that Quibble uses [JSONPath](https://goessner.net/articles/JsonPath/) syntax to point you to differences. 


### Comparing numbers

#### Number example: 1 != 2

```
JsonStrings.diff "1" "2" 
```

yields the following list of diffs: 

```
[ Value { 
    Path = "$"; 
    Left = Number (1., "1"); 
    Right = Number (2., "2") } ]
```

The `Number` type contains both the parsed double value of the number and the original text representation from the JSON string. The reason is that there can be several text representations of the same number (e.g. `1.0` and `1` are the same number in JSON). Quibble keeps both in order to compare double values for differences, yet report any differences using the original text representations.

To get a text description of the difference between the JSON numbers `1` and `2`:

```
JsonStrings.textDiff "1" "2" 
|> List.head
|> printfn "%s"
```

prints 

```
Number value difference at $: 1 vs 2.
```

If you instead compare the JSON numbers `1.0` and `2`, you get this:

```
JsonStrings.textDiff "1.0" "2" 
|> List.head
|> printfn "%s"
```

prints 

```
Number value difference at $: 1.0 vs 2.
```

The numbers `1` and `1.0` are parsed into the same JSON number, yet Quibble reports the difference using the original text representation. 

#### Number example: 1.0 == 1

To make this very explicit:

```
JsonStrings.diff "1.0" "1" 
```

yields an empty list of diffs: 

```
[]
```

The reason is that JSON doesn't distinguish between integers and doubles, everything is just a number. Hence `1.0` and `1` are the same number written in different ways.

#### Number example: 123.4 vs 1.234E2

```
JsonStrings.diff "123.4" "1.234E2"
```

also yields an empty list of diffs: 

```
[]
```

The reason is that 123.4 and 1.234E2 are just different ways of writing the same number.

### Comparing arrays

Quibble uses [Ratcliff/Obershelp pattern recognition](https://en.wikipedia.org/wiki/Gestalt_Pattern_Matching) to describe the differences between arrays. It is based on finding the longest common sub-arrays. 

#### Array example: Number of items

```
JsonStrings.diff "[ 3 ]" "[ 3, 7 ]"
|> List.head
|> printfn "%s"
```

yields the following list of diffs: 

```
[ Items ({ Path = "$"
           Left = Array [ Number(3., "3") ]
           Right = Array [ Number(3., "3"); Number(7., "7") ] }, 
         [ RightOnlyItem (1, Number(7., "7")) ]
```

For a text description:

```
JsonStrings.textDiff "[ 3 ]" "[ 3, 7 ]"
|> List.head
|> printfn "%s"
```

prints

```
Array difference at $.
 + [1] (the number 7)
```

#### Array example: Order matters

```
JsonStrings.diff "[ 24, 12 ]" "[ 12, 24 ]"
```

yields the following list of diffs: 

```
[ Items ({ Path = "$"
           Left = Array [ Number(24., "24"); Number(12., "12") ]
           Right = Array [ Number(12., "12"); Number(24., "24") ] }, 
         [ LeftOnlyItem (0, Number(24., "24"))
           RightOnlyItem (1, Number(24., "24")) ]) ]
```

Quibble identifies `[12]` as the longest common sub-array, and treats the leading `24` in the left array and the trailing `24` in the right array as extra elements.

For a text description:

```
JsonStrings.textDiff "[ 24, 12 ]" "[ 12, 24 ]"
|> List.head 
|> printfn "%s"
```

prints

```
Array difference at $.
 - [0] (the number 24)
 + [1] (the number 24)
```

#### Array example: More elements

The benefits of using longest common sub-array for creating the diff are more apparent for longer arrays that are almost the same.

```
let str1 = """[{
    "title": "Data and Reality",
    "author": "William Kent"
}, {
    "title": "Thinking Forth",
    "author": "Leo Brodie"
}, {
    "title": "Programmers at Work",
    "author": "Susan Lammers"
}, {
    "title": "The Little Schemer",
    "authors": [ "Daniel P. Friedman", "Mattias Felleisen" ]
}, {
    "title": "Object Design",
    "authors": [ "Rebecca Wirfs-Brock", "Alan McKean" ]
}, {
    "title": "Domain Modelling made Functional",
    "author": "Scott Wlaschin"
}, {
    "title": "The Psychology of Computer Programming",
    "author": "Gerald M. Weinberg"
}, {
    "title": "Exercises in Programming Style",
    "author": "Cristina Videira Lopes"
}, {
    "title": "Land of Lisp",
    "author": "Conrad Barski"
}]"""

let str2 = """[{
    "title": "Data and Reality",
    "author": "William Kent"
}, {
    "title": "Thinking Forth",
    "author": "Leo Brodie"
}, {
    "title": "Coders at Work",
    "author": "Peter Seibel"
}, {
    "title": "The Little Schemer",
    "authors": [ "Daniel P. Friedman", "Mattias Felleisen" ]
}, {
    "title": "Object Design",
    "authors": [ "Rebecca Wirfs-Brock", "Alan McKean" ]
}, {
    "title": "Domain Modelling made Functional",
    "author": "Scott Wlaschin"
}, {
    "title": "The Psychology of Computer Programming",
    "author": "Gerald M. Weinberg"
}, {
    "title": "Turtle Geometry",
    "authors": [ "Hal Abelson", "Andrea diSessa" ]
}, {
    "title": "Exercises in Programming Style",
    "author": "Cristina Videira Lopes"
}, {
    "title": "Land of Lisp",
    "author": "Conrad Barski"
}]"""

JsonStrings.diff str1 str2 
```

yields the following list of diffs: 

```
[ Items ({ Path = "$"
           Left = Array [ Object [("title", String "Data and Reality"); ("author", String "William Kent")];
                          Object [("title", String "Thinking Forth"); ("author", String "Leo Brodie")];
                          Object [("title", String "Programmers at Work"); ("author", String "Susan Lammers")];
                          Object [("title", String "The Little Schemer"); ("authors", Array [String "Daniel P. Friedman"; String "Matthias Felleisen"])];
                          Object [("title", String "Object Design"); ("authors", Array [String "Rebecca Wirfs-Brock"; String "Alan McKean"])];
                          Object [("title", String "Domain Modelling made Functional"); ("author", String "Scott Wlaschin")];
                          Object [("title", String "The Psychology of Computer Programming"); ("author", String "Gerald M. Weinberg")];
                          Object [("title", String "Exercises in Programming Style"); ("author", String "Cristina Videira Lopes")];
                          Object [("title", String "Land of Lisp"); ("author", String "Conrad Barski")]]
           Right = Array [ Object [("title", String "Data and Reality"); ("author", String "William Kent")];
                           Object [("title", String "Thinking Forth"); ("author", String "Leo Brodie")];
                           Object [("title", String "Coders at Work"); ("author", String "Peter Seibel")];
                           Object [("title", String "The Little Schemer"); ("authors", Array [String "Daniel P. Friedman"; String "Matthias Felleisen"])];
                           Object [("title", String "Object Design"); ("authors", Array [String "Rebecca Wirfs-Brock"; String "Alan McKean"])];
                           Object [("title", String "Domain Modelling made Functional"); ("author", String "Scott Wlaschin")];
                           Object [("title", String "The Psychology of Computer Programming"); ("author", String "Gerald M. Weinberg")];
                           Object [("title", String "Turtle Geometry"); ("authors", Array [String "Hal Abelson"; String "Andrea diSessa"])];
                           Object [("title", String "Exercises in Programming Style"); ("author", String "Cristina Videira Lopes")];
                           Object [("title", String "Land of Lisp"); ("author", String "Conrad Barski")]] },
         [ LeftOnlyItem (2, Object [("title", String "Programmers at Work"); ("author", String "Susan Lammers")]);
           RightOnlyItem (2, Object [("title", String "Coders at Work"); ("author", String "Peter Seibel")]);
           RightOnlyItem (7, Object [("title", String "Turtle Geometry"); ("authors", Array [String "Hal Abelson"; String "Andrea diSessa"])])]) ]
```

For a text description: 

```
JsonStrings.diff str1 str2 
|> List.head 
|> printfn "%s"
```

prints

```
Array difference at $.
 - [2] (an object)
 + [2] (an object)
 + [7] (an object)
```


### Comparing objects

#### Object example: Property differences

```
let str1 = """{ "item": "widget", "price": 12.20 }"""
let str2 = """{ "item": "widget", "quantity": 88, "in stock": true }"""

JsonStrings.diff str1 str2
```

yields the following list of diffs: 

```
[ Properties
    ({ Path = "$"
       Left =
           Object
               [ ("item", String "widget")
                 ("price", Number(12.2, "12.20")) ]
       Right =
           Object
               [ ("item", String "widget")
                 ("quantity", Number(88.0, "88"))
                 ("in stock", True) ] },
      [ LeftOnlyProperty("price", Number(12.2, "12.20"))
        RightOnlyProperty("quantity", Number(88.0, "88"))
        RightOnlyProperty("in stock", True) ]) ]
```

Quibble treats it as a single difference with three mismatching properties.

For a text description:

```
let str1 = """{ "item": "widget", "price": 12.20 }"""
let str2 = """{ "item": "widget", "quantity": 88, "in stock": true }"""

JsonStrings.textDiff str1 str2 
|> List.head
|> printfn "%s"
```

prints 

```
Object difference at $.
Left only property: 'price' (number).
Right only properties: 'quantity' (number), 'in stock' (bool).
```

#### Object example: Property with spaces 

```
let str1 = """{ "name": "Maya", "date of birth": "1999-04-23" }"""
let str2 = """{ "name": "Maya", "date of birth": "1999-04-24" }"""

JsonStrings.diff str1 str2
```

yields the following list of diffs: 

```
[ Value
    { Path = "$['date of birth']"
      Left = String "1999-04-23"
      Right = String "1999-04-24" } ]
```

JSONPath handles spaces in property names by using the alternative bracket-and-quotes syntax shown.

For a text description:

```
let str1 = """{ "name": "Maya", "date of birth": "1999-04-23" }"""
let str2 = """{ "name": "Maya", "date of birth": "1999-04-24" }"""

JsonStrings.textDiff str1 str2 
|> List.head 
|> printfn "%s"
```

prints 

```
String value difference at $['date of birth']: 1999-04-23 vs 1999-04-24.
```


### Composite example

```
let str1 =
   """{
    "books": [{
        "title": "Data and Reality",
        "author": "William Kent"
    }, {
        "title": "Thinking Forth",
        "author": "Leo Brodie"
    }]
}"""

let str2 =
    """{
    "books": [{
        "title": "Data and Reality",
        "author": "William Kent",
        "edition": "2nd"
    }, {
        "title": "Thinking Forth",
        "author": "Chuck Moore"
    }]
}"""

JsonStrings.diff str1 str2 
```

yields the following list of differences:

```
[ Properties
     ({ Path = "$.books[0]"
        Left =
            Object
                [ ("title", String "Data and Reality")
                  ("author", String "William Kent") ]
        Right =
            Object
                [ ("title", String "Data and Reality")
                  ("author", String "William Kent")
                  ("edition", String "2nd") ] },
      [ RightOnlyProperty("edition", String "2nd") ])
  Value
     { Path = "$.books[1].author"
       Left = String "Leo Brodie"
       Right = String "Chuck Moore" } ]
```

For a text description:

```
let str1 =
   """{
    "books": [{
        "title": "Data and Reality",
        "author": "William Kent"
    }, {
        "title": "Thinking Forth",
        "author": "Leo Brodie"
    }]
}"""

let str2 =
    """{
    "books": [{
        "title": "Data and Reality",
        "author": "William Kent",
        "edition": "2nd"
    }, {
        "title": "Thinking Forth",
        "author": "Chuck Moore"
    }]
}"""

let diffs = JsonStrings.textDiff str1 str2 
match diffs with
| [ diff1; diff2 ] -> 
    printfn "%s" diff1
    printfn "%s" diff2
```

prints

```
Object difference at $.books[0].
Right only property: 'edition' (string).
String value difference at $.books[1].author: Leo Brodie vs Chuck Moore.
```


# C# Examples

```
using Quibble.CSharp;
```

Use `JsonStrings.Diff` to get a read-only list of `Diff`-values that you can work with as you like - loop, branch, pattern match etc.

Use `JsonStrings.TextDiff` to get a read-only list of text descriptions of the differences.

If you read the examples and wonder what `$` means, note that Quibble uses [JSONPath](https://goessner.net/articles/JsonPath/) syntax to point you to differences. 

### Comparing numbers

#### Number example: 1 != 2

```
JsonStrings.Diff("1", "2");
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
{
    new Value(
        new DiffPoint("$",
            new Number(1, "1"),
            new Number(2, "2")))
};
```

For a text description:

```
var diffs = JsonStrings.TextDiff("1", "2");
Console.WriteLine(diff.Single());
```

prints 

```
Number value difference at $: 1 vs 2.
```

If you instead compare the JSON numbers `1.0` and `2`, you get this:

```
var diffs = JsonStrings.TextDiff("1.0", "2");
Console.WriteLine(diff.Single());
```

prints 

```
Number value difference at $: 1.0 vs 2.
```

The numbers `1` and `1.0` are parsed into the same JSON number, yet Quibble reports the difference using the original text representation. 

#### Number example: 1.0 == 1

To make this very explicit:

```
JsonStrings.Diff("1.0", "1");
```

yields an empty list of diffs. 

The reason is that JSON doesn't distinguish between integers and doubles, everything is just a number. Hence `1.0` and `1` are the same number written in different ways.


#### Number example: 123.4 vs 1.234E2

```
JsonStrings.Diff("123.4", "1.234E2");
```

also yields an empty list of diffs. The reason is that `123.4` and `1.234E2` are just different ways of writing the same number.

### Comparing arrays

Quibble uses [Ratcliff/Obershelp pattern recognition](https://en.wikipedia.org/wiki/Gestalt_Pattern_Matching) to describe the differences between arrays. It is based on finding the longest common sub-arrays. 

#### Array example: Number of items

```
JsonStrings.Diff("[ 3 ]", "[ 3, 7 ]");
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
{
    new Items(new DiffPoint("$",
            new Array(new JsonValue[]
            {
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Data and Reality")},
                        {"author", new String("William Kent")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Thinking Forth")},
                        {"author", new String("Leo Brodie")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Programmers at Work")},
                        {"author", new String("Susan Lammers")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("The Little Schemer")},
                        {
                            "authors",
                            new Array(new JsonValue[]
                                {new String("Daniel P. Friedman"), new String("Matthias Felleisen")})
                        }
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Object Design")},
                        {
                            "authors",
                            new Array(new JsonValue[]
                                {new String("Rebecca Wirfs-Brock"), new String("Alan McKean")})
                        }
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Domain Modelling made Functional")},
                        {"author", new String("Scott Wlaschin")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("The Psychology of Computer Programming")},
                        {"author", new String("Gerald M. Weinberg")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Exercises in Programming Style")},
                        {"author", new String("Cristina Videira Lopes")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Land of Lisp")},
                        {"author", new String("Conrad Barski")}
                    })
            }),
            new Array(new JsonValue[]
            {
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Data and Reality")},
                        {"author", new String("William Kent")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Thinking Forth")},
                        {"author", new String("Leo Brodie")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Coders at Work")},
                        {"author", new String("Peter Seibel")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("The Little Schemer")},
                        {
                            "authors",
                            new Array(new JsonValue[]
                                {new String("Daniel P. Friedman"), new String("Matthias Felleisen")})
                        }
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Object Design")},
                        {
                            "authors",
                            new Array(new JsonValue[]
                                {new String("Rebecca Wirfs-Brock"), new String("Alan McKean")})
                        }
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Domain Modelling made Functional")},
                        {"author", new String("Scott Wlaschin")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("The Psychology of Computer Programming")},
                        {"author", new String("Gerald M. Weinberg")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Turtle Geometry")},
                        {
                            "authors",
                            new Array(new JsonValue[]
                                {new String("Hal Abelson"), new String("Andrea diSessa")})
                        }
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Exercises in Programming Style")},
                        {"author", new String("Cristina Videira Lopes")}
                    }),
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Land of Lisp")},
                        {"author", new String("Conrad Barski")}
                    })
            })),
        new ItemMismatch[]
        {
            new LeftOnlyItem(2,
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Programmers at Work")},
                        {"author", new String("Susan Lammers")}
                    })),
            new RightOnlyItem(2,
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Coders at Work")},
                        {"author", new String("Peter Seibel")}
                    })),
            new RightOnlyItem(7,
                new Object(
                    new Dictionary<string, JsonValue>
                    {
                        {"title", new String("Turtle Geometry")},
                        {
                            "authors",
                            new Array(new JsonValue[]
                                {new String("Hal Abelson"), new String("Andrea diSessa")})
                        }
                    }))
        })
};
```

The most interesting part is the `ItemMismatch` array, which contains the items that are present either in just the left array ("Programmers at work" by Susan Lammers, at index 2) or just in the right array ("Coders at Work" by Peter Seibel, at index 2, and "Turtle Geometry" by Hal Abelson and Andrea diSessa, at index 7).

For a text description:

```
var diffs = JsonStrings.Diff(str1, str2);
Console.WriteLine(diffs.Single());
```

prints

```
Array difference at $.
 - [2] (an object)
 + [2] (an object)
 + [7] (an object)
```

#### Array example: Item order matters

```
JsonStrings.Diff("[ 24, 12 ]", "[ 12, 24 ]");
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
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
```

For a text description:

```
var diffs = JsonStrings.TextDiff("[ 24, 12 ]", "[ 12, 24 ]");
foreach (var diff in diffs) 
{
    Console.WriteLine(diff);
}
```

prints

```
Number value difference at $[0]: 24 vs 12.
Number value difference at $[1]: 12 vs 24.
```

### Comparing objects

#### Object example: Property differences

```
var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
var str2 = @"{ ""item"": ""widget"", ""quantity"": 88, ""in stock"": true }";
JsonStrings.Diff(str1, str2);
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
{
    new Properties(
        new DiffPoint("$",
            new Object(
                new Dictionary<string, JsonValue>
                {
                    { "item", new String("widget") },
                    { "price", new Number(12.20, "12.20") }
                }), 
            new Object(
                new Dictionary<string, JsonValue>
                {
                    { "item", new String("widget") },
                    { "quantity", new Number(88, "88") },
                    { "in stock", True.Instance }
                })), 
        new List<PropertyMismatch>
        {
            new LeftOnlyProperty("price", new Number(12.20, "12.20")),
            new RightOnlyProperty("quantity", new Number(88, "88")),
            new RightOnlyProperty("in stock", True.Instance)
        })
};
```

Quibble treats it as a single difference with three mismatching properties.

For a text description:

```
var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
var str2 = @"{ ""item"": ""widget"", ""quantity"": 88, ""in stock"": true }";
var diffs = JsonStrings.TextDiff(str1, str2);
Console.WriteLine(diffs.Single());
```

prints 

```
Object difference at $.
Left only property: 'price' (number).
Right only properties: 'quantity' (number), 'in stock' (bool).
```

#### Object example: Property with spaces 

```
var str1 = @"{ ""name"": ""Maya"", ""date of birth"": ""1999-04-23"" }";
var str2 = @"{ ""name"": ""Maya"", ""date of birth"": ""1999-04-24"" }";
JsonStrings.Diff(str1, str2);
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
{
    new Value(
        new DiffPoint("$['date of birth']",
            new String("1999-04-23"), 
            new String("1999-04-24")))
};
```

JSONPath handles spaces in property names by using the alternative bracket-and-quotes syntax shown.

For a text description:

```
var str1 = @"{ ""name"": ""Maya"", ""date of birth"": ""1999-04-23"" }";
var str2 = @"{ ""name"": ""Maya"", ""date of birth"": ""1999-04-24"" }";
var diffs = JsonStrings.TextDiff(str1, str2);
Console.WriteLine(diffs.Single());
```

prints 

```
String value difference at $['date of birth']: 1999-04-23 vs 1999-04-24.
```

### Composite example

```
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

JsonStrings.Diff(str1, str2);
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
{
    new Properties(
        new DiffPoint("$.books[0]",
            new Object(
                new Dictionary<string, JsonValue>
                {
                    { "title", new String("Data and Reality") },
                    { "author", new String("William Kent") }
                }), 
            new Object(
                new Dictionary<string, JsonValue>
                {
                    { "title", new String("Data and Reality") },
                    { "author", new String("William Kent") },
                    { "edition", new String("2nd") }
                })), 
        new List<PropertyMismatch>
        {
            new RightOnlyProperty("edition", new String("2nd"))
        }),
    new Value(
        new DiffPoint("$.books[1].author",
            new String("Leo Brodie"), 
            new String("Chuck Moore")))
};
```

For a text description:

```
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

foreach (var diff in diffs) 
{
    Console.WriteLine(diff);
}
```

prints 

```
Object difference at $.books[0].
Right only property: 'edition' (string).
String value difference at $.books[1].author: Leo Brodie vs Chuck Moore.
```
