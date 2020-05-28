[![Build status](https://ci.appveyor.com/api/projects/status/0v6946lhh480cgbk?svg=true)](https://ci.appveyor.com/project/NRKOpensource/quibble)
[![NuGet Status](https://img.shields.io/nuget/v/Quibble.svg?style=flat)](https://www.nuget.org/packages/Quibble/)

# Quibble

Quibble is a JSON diff tool for .NET. You give Quibble two text strings with JSON content and it will tell you what the differences are. 

Quibble distinguishes between four kinds of differences: 

* `Kind`: difference in the kind of JSON value, e.g. `string` vs `number`.
* `Value`: difference in the value itself, e.g. the string `cat` vs the string `dog`.
* `Properties`: when two JSON objects have differences in their properties, e.g. the object `{ "name": "Quux" }` vs the object `{ "id": "1c3d" }`.
* `ItemCount`: when two JSON arrays have a different number of items, e.g. the array `[ 1, 2 ]` vs the array `[ 1, 2, 3 ]`.

# Why Quibble?

Quibble is useful whenever you need to compare two JSON documents to see if and how they're different. Since JSON is pretty much everywhere these days, that's really a basic feature.

Quibble uses [JsonPath](https://goessner.net/articles/JsonPath/) syntax to point you to the right elements. In JsonPath syntax, `$` indicates the root of the document, whereas something like `$.books[1].author` means "the author property of the second element of the books array".

If you're using [XUnit](https://xunit.net/) to write tests, you may want to check out [Quibble.Xunit](https://github.com/nrkno/json-quibble-xunit), an extension to XUnit that does asserts on text strings with JSON content.

# Examples 

## F#

```
open Quibble
```

### Comparing numbers

```
JsonStrings.verify "1" "2" |> List.iter (printfn "%s")
```

yields

```
Number value mismatch at $.
Expected 2 but was 1.
```

### Comparing arrays

```
JsonStrings.verify "[ 1 ]" "[ 2, 1 ]" |> List.iter (printfn "%s")
```

yields

```
Array length mismatch at $.
Expected 2 items but was 1.
```

### Comparing objects

```
let str1 = """{ "item": "widget", "price": 12.20 }"""
let str2 = """{ "item": "widget" }"""

JsonStrings.verify str1 str2 |> List.iter (printfn "%s")
```

yields

```
Object mismatch at $.
Additional property:
price (number).
```

### Composite example

```
let str1 = """{ "books": [ { "title": "Data and Reality", "author": "William Kent" }, { "title": "Thinking Forth", "author": "Chuck Moore" } ] }"""
let str2 = """{ "books": [ { "title": "Data and Reality", "author": "William Kent" }, { "title": "Thinking Forth", "author": "Leo Brodie" } ] }"""

JsonStrings.verify str1 str2 |> List.iter (printfn "%s")
```

yields

```
String value mismatch at $.books[1].author.
Expected Leo Brodie but was Chuck Moore.
```

## C#

```
using Quibble.CSharp;
```

### Comparing numbers

```
var diffs = JsonStrings.Verify("1", "2");
foreach (var diff in diffs)
{
    Console.WriteLine(diff);
}
```

yields

```
Number value mismatch at $.
Expected 2 but was 1.
```

### Comparing arrays

```
var diffs = JsonStrings.Verify("[ 1 ]", "[ 2, 1 ]");
foreach (var diff in diffs)
{
    Console.WriteLine(diff);
}
```

yields

```
Array length mismatch at $.
Expected 2 items but was 1.
```

### Comparing objects

```
var str1 = @"{ ""item"": ""widget"", ""price"": 12.20 }";
var str2 = @"{ ""item"": ""widget"" }";

var diffs = JsonStrings.Verify(str1, str2);
foreach (var diff in diffs)
{
    Console.WriteLine(diff);
}
```

yields

```
Object mismatch at $.
Additional property:
price (number).
```

### Composite example

```
var str1 = @"{ ""books"": [ { ""title"": ""Data and Reality"", ""author"": ""William Kent"" }, { ""title"": ""Thinking Forth"", ""author"": ""Chuck Moore"" } ] }";
var str2 = @"{ ""books"": [ { ""title"": ""Data and Reality"", ""author"": ""William Kent"" }, { ""title"": ""Thinking Forth"", ""author"": ""Leo Brodie"" } ] }";

var diffs = JsonStrings.Verify(str1, str2);
foreach (var diff in diffs)
{
    Console.WriteLine(diff);
}
```

yields

```
String value mismatch at $.books[1].author.
Expected Leo Brodie but was Chuck Moore.
```

# Assumptions

Quibble makes the following assumptions: 
* Order of elements matters for arrays.
* Order of elements does not matter for properties in an object.
