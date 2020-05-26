[![Build status](https://ci.appveyor.com/api/projects/status/0v6946lhh480cgbk?svg=true)](https://ci.appveyor.com/project/NRKOpensource/json-quibble)
[![NuGet Status](https://img.shields.io/nuget/v/Quibble.svg?style=flat)](https://www.nuget.org/packages/Quibble/)

# Quibble

Quibble is a JSON diff tool for .NET. You give Quibble two text strings with JSON content and it will tell you what the differences are.

# Why Quibble?

We often want to verify that a JSON text string matches our expectations or not. A typical use case is writing tests for a web api that serves JSON responses. Without a JSON diff tool, we have two options: compare the JSON text as strings or deserialize the JSON into a data structure and compare the data structure with your expectations. Treating the JSON as a generic string yields a poor experience when looking for differences. Deserializing the response before comparing means that you have to write deserialization code (which may or may not be trivial) and in addition means you're comparing something else than what you really wanted to compare. In contrast, Quibble understands JSON and will point you directly to the differences in your JSON documents. Quibble uses [JsonPath](https://goessner.net/articles/JsonPath/) syntax to point you to the right elements. In JsonPath syntax, `$` indicates the root of the document, whereas something like `$.books[1].author` means "the author property of the second element of the books array".

# Examples 

## F#

### Comparing numbers

```
JsonVerify.Diff("1", "2") |> Seq.iter (printfn "%s")
```

yields

```
Number value mismatch at $.
Expected 2 but was 1.
```

### Comparing arrays

```
JsonVerify.Diff("[ 1 ]", "[ 2, 1 ]") |> Seq.iter (printfn "%s")
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

JsonVerify.Diff(str1, str2) |> Seq.iter (printfn "%s")
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

JsonVerify.Diff(str1, str2) |> Seq.iter (printfn "%s")
```

yields

```
String value mismatch at $.books[1].author.
Expected Leo Brodie but was Chuck Moore.
```

## C#

### Comparing numbers

```
var diffs = JsonVerify.Diff("1", "2");
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
var diffs = JsonVerify.Diff("[ 1 ]", "[ 2, 1 ]");
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

var diffs = JsonVerify.Diff(str1, str2);
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

var diffs = JsonVerify.Diff(str1, str2);
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
