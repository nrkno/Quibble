[![Build status](https://ci.appveyor.com/api/projects/status/0v6946lhh480cgbk?svg=true)](https://ci.appveyor.com/project/NRKOpensource/quibble)
[![NuGet Status](https://img.shields.io/nuget/v/Quibble.svg?style=flat)](https://www.nuget.org/packages/Quibble/)

# Quibble

Quibble is a JSON diff tool for .NET. You give Quibble two text strings with JSON content and it will tell you what the differences are. 

Quibble distinguishes between four kinds of differences: 

* `TypeDiff`: e.g. `string` vs `number`.
* `ValueDiff`: same type but different value, e.g. the string `cat` vs the string `dog`.
* `ObjectDiff`: when two JSON objects have differences in their properties, e.g. the object `{ "name": "Quux" }` vs the object `{ "id": "1c3d" }`.
* `ArrayDiff`: when two JSON arrays have a different items, e.g. the array `[ 1, 2, 3 ]` vs the array `[ 2, 3, 4 ]`.

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
    Left = JsonNumber (1., "1"); 
    Right = JsonNumber (2., "2") } ]
```

The `JsonNumber` type contains both the parsed double value of the number and the original text representation from the JSON string. The reason is that there can be several text representations of the same number (e.g. `1.0` and `1` are the same number in JSON). Quibble keeps both in order to compare double values for differences, yet report any differences using the original text representations.

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
           Left = JsonArray [ JsonNumber(3., "3") ]
           Right = JsonArray [ JsonNumber(3., "3"); JsonNumber(7., "7") ] }, 
         [ RightOnlyItem (1, JsonNumber(7., "7")) ]
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
           Left = JsonArray [ JsonNumber(24., "24"); JsonNumber(12., "12") ]
           Right = JsonArray [ JsonNumber(12., "12"); JsonNumber(24., "24") ] }, 
         [ LeftOnlyItem (0, JsonNumber(24., "24"))
           RightOnlyItem (1, JsonNumber(24., "24")) ]) ]
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

#### Array example: More items

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
[ ArrayDiff ({ Path = "$"; Left = JsonArray [ ... ]; Right = JsonArray [ ... ] },
             [ LeftOnlyItem (2, JsonObject [("title", JsonString "Programmers at Work")
                                            ("author", JsonString "Susan Lammers")])
               RightOnlyItem (2, JsonObject [("title", JsonString "Coders at Work")
                                             ("author", JsonString "Peter Seibel")]);
               RightOnlyItem (7, JsonObject [("title", JsonString "Turtle Geometry")
                                             ("authors", JsonArray [ JsonString "Hal Abelson"; JsonString "Andrea diSessa" ])])]) ]
```

The most interesting part is the list of item mismatches, which contains the items that are present either in just the left JSON array ("Programmers at work" by Susan Lammers, at index 2) or just in the right JSON array ("Coders at Work" by Peter Seibel, at index 2, and "Turtle Geometry" by Hal Abelson and Andrea diSessa, at index 7).

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
           JsonObject
               [ ("item", JsonString "widget")
                 ("price", JsonNumber(12.2, "12.20")) ]
       Right =
           JsonObject
               [ ("item", JsonString "widget")
                 ("quantity", JsonNumber(88.0, "88"))
                 ("in stock", JsonTrue) ] },
      [ LeftOnlyProperty("price", JsonNumber(12.2, "12.20"))
        RightOnlyProperty("quantity", JsonNumber(88.0, "88"))
        RightOnlyProperty("in stock", JsonTrue) ]) ]
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
      Left = JsonString "1999-04-23"
      Right = JsonString "1999-04-24" } ]
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
            JsonObject
                [ ("title", JsonString "Data and Reality")
                  ("author", JsonString "William Kent") ]
        Right =
            JsonObject
                [ ("title", JsonString "Data and Reality")
                  ("author", JsonString "William Kent")
                  ("edition", JsonString "2nd") ] },
      [ RightOnlyProperty("edition", JsonString "2nd") ])
  Value
     { Path = "$.books[1].author"
       Left = JsonString "Leo Brodie"
       Right = JsonString "Chuck Moore" } ]
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
    new ValueDiff(
        new DiffPoint("$",
            new JsonNumber(1, "1"),
            new JsonNumber(2, "2")))
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
    new ArrayDiff(
        new DiffPoint("$",
            new JsonArray (
                new JsonValue []
                {
                    new JsonNumber(3, "3")
                }),
            new JsonArray (
                new JsonValue []
                {
                    new JsonNumber(3, "3"),
                    new JsonNumber(7, "7")
            })), 
        new ItemMismatch[]
        {
            new RightOnlyItem(1, new JsonNumber(7, "7")), 
        })
};
```

For a text description:

```
var diffs = JsonStrings.TextDiff("[ 3 ]", "[ 3, 7 ]");
Console.WriteLine(diffs.Single());
```

prints

```
Array difference at $.
 + [1] (the number 7)
```

The most interesting part is the `ItemMismatch` array, which contains the items that are present either in just the left array ("Programmers at work" by Susan Lammers, at index 2) or just in the right array ("Coders at Work" by Peter Seibel, at index 2, and "Turtle Geometry" by Hal Abelson and Andrea diSessa, at index 7).


#### Array example: Item order matters

```
JsonStrings.Diff("[ 24, 12 ]", "[ 12, 24 ]");
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
{
    new ArrayDiff(
        new DiffPoint("$", 
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
```

For a text description:

```
var diffs = JsonStrings.TextDiff("[ 24, 12 ]", "[ 12, 24 ]");
Console.WriteLine(diffs.Single());
```

prints

```
Array difference at $.
 - [0] (the number 24)
 + [1] (the number 24)
```

#### Array example: More items

The benefits of using longest common sub-array for creating the diff are more apparent for longer arrays that are almost the same.

```
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
```

yields the following list of diffs: 

```
new List<Diff>
{
    new ArrayDiff(new DiffPoint("$", new JsonArray(...), new JsonArray(...)),
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
                            new JsonArray(new JsonValue[] { 
                                new JsonString("Hal Abelson"), 
                                new JsonString("Andrea diSessa")
                            })
                        }
                    }))
        })
};
```

The most interesting part is the `ItemMismatch` array, which contains the items that are present either in just the left array ("Programmers at work" by Susan Lammers, at index 2) or just in the right array ("Coders at Work" by Peter Seibel, at index 2, and "Turtle Geometry" by Hal Abelson and Andrea diSessa, at index 7).

For a text description:

```
var diffs = JsonStrings.TextDiff(str1, str2);
Console.WriteLine(diffs.Single());
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
var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
var str2 = @"{ ""item"": ""widget"", ""quantity"": 88, ""in stock"": true }";
JsonStrings.Diff(str1, str2);
```

yields a list of diffs equivalent to this: 

```
new List<Diff>
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
    new ValueDiff(
        new DiffPoint("$['date of birth']",
            new JsonString("1999-04-23"), 
            new JsonString("1999-04-24")))
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
