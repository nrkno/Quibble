[![Build status](https://ci.appveyor.com/api/projects/status/0v6946lhh480cgbk?svg=true)](https://ci.appveyor.com/project/NRKOpensource/quibble)
[![NuGet Status](https://img.shields.io/nuget/v/Quibble.svg?style=flat)](https://www.nuget.org/packages/Quibble/)

# Quibble

Quibble is a JSON diff tool for .NET. You give Quibble two text strings with JSON content and it will tell you what the differences are. 

Quibble distinguishes between four kinds of differences: 

* `Type`: e.g. `string` vs `number`.
* `Value`: same type but different value, e.g. the string `cat` vs the string `dog`.
* `Properties`: when two JSON objects have differences in their properties, e.g. the object `{ "name": "Quux" }` vs the object `{ "id": "1c3d" }`.
* `ItemCount`: when two JSON arrays have a different number of items, e.g. the array `[ 1, 2 ]` vs the array `[ 1, 2, 3 ]`.

Quibble makes a couple of [assumptions](#assumptions) that you might want to know about.

## TL;DR

* [F# Examples](#f-examples).
* [C# Examples](#c-examples).

# Why Quibble?

Quibble is useful whenever you need to compare two JSON documents to see if and how they're different. Since JSON is pretty much everywhere these days, that's really a basic feature.

Quibble uses [JsonPath](https://goessner.net/articles/JsonPath/) syntax to point you to the right elements. In JsonPath syntax, `$` indicates the root of the document, whereas something like `$.books[1].author` means "the author property of the second element of the books array".

If you're using [XUnit](https://xunit.net/) to write tests, you may want to check out [Quibble.Xunit](https://github.com/nrkno/json-quibble-xunit), an extension to XUnit that does asserts on text strings with JSON content.

# F# Examples

```
open Quibble
```

Use `JsonStrings.diff` to get a list of `Diff`-values that you can map, filter and pattern match as you like.

Use `JsonStrings.textDiff` to get a list of text descriptions of the differences.

If you read the examples and wonder what `$` means, note that Quibble uses [JsonPath](https://goessner.net/articles/JsonPath/) syntax to point you to differences. 

### Comparing numbers

#### Number example: 1 != 2

```
JsonStrings.textDiff "1" "2" 
|> List.head
|> printfn "%s"
```

prints 

```
Number value difference at $: 1 vs 2.
```

#### Number example: 1.0 == 1

```
JsonStrings.textDiff "1.0" "1" 
|> List.isEmpty
|> printfn "%b"
```

prints 

```
true
```

The reason is that JSON doesn't distinguish between integers and doubles, everything is just a number.

#### Number example: 123.4 vs 1.234E2

```
JsonStrings.textDiff "123.4" "1.234E2" 
|> List.isEmpty
|> printfn "%b"
```

prints 

```
true
```

The reason is that 123.4 and 1.234E2 are just different ways of writing the same number.

### Comparing arrays

#### Array example: Number of items

```
JsonStrings.textDiff "[ 1 ]" "[ 2, 1 ]"
|> List.head
|> printfn "%s"
```

prints

```
Array length difference at $: 1 vs 2.
```

#### Array example: Order matters

```
let diffs = JsonStrings.textDiff "[ 2, 1 ]" "[ 1, 2 ]"
match diffs with
| [ diff1; diff2 ] -> 
    printfn "%s" diff1
    printfn "%s" diff2
```

prints

```
Number value difference at $[0]: 2 vs 1.
Number value difference at $[1]: 1 vs 2.
```


### Comparing objects

#### Object example: Property differences

```
let str1 = """{ "item": "widget", "price": 12.20 }"""
let str2 = """{ "item": "widget", "quantity": 88, "inStock": true }"""

JsonStrings.textDiff str1 str2 
|> List.head
|> printfn "%s"
```

prints 

```
Object difference at $.
Left only property: price (number).
Right only properties: quantity (number), inStock (bool).
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

let diffs = JsonStrings.textDiff str1 str2 
match diffs with
| [ diff1; diff2 ] -> 
    printfn "%s" diff1
    printfn "%s" diff2
```

prints

```
Object difference at $.books[0].
Right only property: edition (string).
String value difference at $.books[1].author: Leo Brodie vs Chuck Moore.
```

# C# Examples

```
using Quibble.CSharp;
```

Use `JsonStrings.Diff` to get a read-only list of `Diff`-values that you can work with as you like.

Use `JsonStrings.TextDiff`to get a read-only list of text descriptions of the differences.

If you read the examples and wonder what `$` means, note that Quibble uses [JsonPath](https://goessner.net/articles/JsonPath/) syntax to point you to differences. 

### Comparing numbers

#### Number example: 1 != 2

```
var diffs = JsonStrings.TextDiff("1", "2");
Console.WriteLine(diff.Single());
```

prints 

```
Number value difference at $: 1 vs 2.
```

#### Number example: 1.0 == 1

```
var diffs = JsonStrings.TextDiff("1.0", "1");
Console.WriteLine(diffs.Any());
```

prints 

```
false
```

The reason is that JSON doesn't distinguish between integers and doubles, everything is just a number.

#### Number example: 123.4 vs 1.234E2

```
var diffs = JsonStrings.TextDiff("123.4", "1.234E2");
Console.WriteLine(diffs.Any());
```

prints 

```
false
```

The reason is that `123.4` and `1.234E2` are just different ways of writing the same number.

### Comparing arrays

#### Array example: Number of items

```
var diffs = JsonStrings.TextDiff("[ 1 ]", "[ 2, 1 ]");
Console.WriteLine(diffs.Single());
```

prints

```
Array length difference at $: 1 vs 2.
```

#### Array example: Item order matters

```
var diffs = JsonStrings.TextDiff("[ 2, 1 ]", "[ 1, 2 ]");
foreach (var diff in diffs) 
{
    Console.WriteLine(diff);
}
```

prints

```
Number value difference at $[0]: 2 vs 1.
Number value difference at $[1]: 1 vs 2.
```

### Comparing objects

#### Object example: Property differences

```
var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
var str2 = @"{ ""item"": ""widget"", ""quantity"": 88, ""inStock"": true }";
var diffs = JsonStrings.TextDiff(str1, str2);
Console.WriteLine(diffs.Single());
```

prints 

```
Object difference at $.
Left only property: price (number).
Right only properties: quantity (number), inStock (bool).
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

var diffs = JsonStrings.TextDiff(str1, str2);

foreach (var diff in diffs) 
{
    Console.WriteLine(diff);
}
```

prints 

```
Object difference at $.books[0].
Right only property: edition (string).
String value difference at $.books[1].author: Leo Brodie vs Chuck Moore.
```

# Assumptions

Quibble makes the following assumptions: 
* Order of items matters for arrays.
* Order of properties does not matter for objects.
* Different ways of writing the same number do not matter.
