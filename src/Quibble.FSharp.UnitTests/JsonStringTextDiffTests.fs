namespace Quibble.FSharp.UnitTests

module JsonStringsTextDiffTests = 

    open Xunit
    open Quibble

    [<Fact>]
    let ``true = true``() = 
        let diffs = JsonStrings.textDiff "true" "true"
        Assert.Empty(diffs)

    [<Fact>]
    let ``false = false``() = 
        let diffs = JsonStrings.textDiff "false" "false"
        Assert.Empty(diffs)

    [<Fact>]
    let ``0 = 0``() = 
        let diffs = JsonStrings.textDiff "0" "0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``0 = -0``() = 
        let diffs = JsonStrings.textDiff "0" "-0"
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``1000 = 1E3``() = 
        let diffs = JsonStrings.textDiff "1000" "1E3"
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``123.4 = 1.234E2``() = 
        let diffs = JsonStrings.textDiff "123.4" "1.234E2"
        Assert.Empty(diffs)

    [<Fact>]
    let ``1 != 2 : number value mismatch``() = 
        let diff = JsonStrings.textDiff "1" "2" |> List.head
        Assert.Equal("Number value difference at $: 1 vs 2.", diff)
        
    [<Fact>]
    let ``1 != 1E1 : number value mismatch``() = 
        let diff = JsonStrings.textDiff "1" "1E1" |> List.head
        Assert.Equal("Number value difference at $: 1 vs 1E1.", diff)
        
    [<Fact>]
    let ``1 = 1.0``() =
        let diffs = JsonStrings.textDiff "1" "1.0"
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``true != 1 : Kind mismatch``() =
        let diffs = JsonStrings.textDiff "true" "1"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Type difference at $: the boolean true vs the number 1.", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``"foo" = "foo"``() = 
        let diffs = JsonStrings.textDiff "\"foo\"" "\"foo\""
        Assert.Empty(diffs)

    [<Fact>]
    let ``"foo" != "bar"``() = 
        let diffs = JsonStrings.textDiff "\"foo\"" "\"bar\""
        match diffs with
        | [ diff ] ->
            Assert.Equal("String value difference at $: foo vs bar.", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``null vs 1``() = 
        let diff = JsonStrings.textDiff "null" "1" |> List.head
        Assert.Equal("Type difference at $: null vs the number 1.", diff)

    [<Fact>]
    let ``Empty array vs null``() = 
        let diff = JsonStrings.textDiff "[]" "null" |> List.head
        Assert.Equal("Type difference at $: an empty array vs null.", diff)
            
    [<Fact>]
    let ``Empty array vs empty object``() = 
        let diffs = JsonStrings.textDiff "[]" "{}"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Type difference at $: an empty array vs an object.", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 2 ]``() = 
        let diffs = JsonStrings.textDiff "[ 1 ]" "[ 2 ]"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Number value difference at $[0]: 1 vs 2.", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 1, 2 ]``() =
        let diffs = JsonStrings.textDiff "[ 1 ]" "[ 1, 2 ]"
        match diffs with 
        | [ diff ] ->
            Assert.Equal("Array difference at $.\n + [1] (the number 2)", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] != [ 2, 1 ] : array length mismatch``() = 
        let diffs = JsonStrings.textDiff "[ 1 ]" "[ 2, 1 ]"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Array difference at $.\n + [0] (the number 2)", diff)
        | _ -> failwith "Wrong number of diffs"
            
    [<Fact>]
    let ``[ 2, 1 ] vs [ 1, 2 ]``() =
        let diffs = JsonStrings.textDiff "[ 2, 1 ]" "[ 1, 2 ]"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Array difference at $.\n - [0] (the number 2)\n + [1] (the number 2)", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``{} != { "count": 0 }``() =
        let diffs = JsonStrings.textDiff "{}" "{ \"count\": 0 }"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Object difference at $.\nRight only property: 'count' (number).", diff)
        | _ -> failwithf "Expected 1 diff but got %d." (List.length diffs)

    [<Fact>]
    let ``{ "count": 0 } != {}``() =
        let diffs = JsonStrings.textDiff "{ \"count\": 0 }" "{}"
        match diffs with
        | [ diff ] ->
            Assert.Equal("Object difference at $.\nLeft only property: 'count' (number).", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``{ "age": 20, "name": "Don" } = { "name": "Don", "age": 20 }``() =
        let diffs = JsonStrings.textDiff """{ "age": 20, "name": "Don" }""" """{ "name": "Don", "age": 20 }"""
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``Compare object with array property``() =
        let s1 = "{ \"my array\": [ 1, 2, 3 ] }"
        let s2 = "{ \"my array\": [ 1, 2, 4 ] }"
        let diffs = JsonStrings.textDiff s1 s2
        match diffs with
        | [ diff ] ->
            Assert.Equal("Number value difference at $['my array'][2]: 3 vs 4.", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

    [<Fact>]
    let ``Widget price example``() =
        let str1 = """{ "item": "widget", "price": 12.20 }"""
        let str2 = """{ "item": "widget" }"""
        let diffs = JsonStrings.textDiff str1 str2 
        match diffs with
        | [ diff ] ->
            Assert.Equal("Object difference at $.\nLeft only property: 'price' (number).", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``Person example with 'date of birth' property.``() =
        let str1 = """{ "name": "Maya", "date of birth": "1999-04-23" }"""
        let str2 = """{ "name": "Maya", "date of birth": "1999-04-24" }"""
        let diffs = JsonStrings.textDiff str1 str2 
        match diffs with
        | [ diff ] ->
            Assert.Equal("String value difference at $['date of birth']: 1999-04-23 vs 1999-04-24.", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

    [<Fact>]
    let ``Book example``() =
        let str1 = """{
    "books": [{
        "title": "Data and Reality",
        "author": "William Kent"
    }, {
        "title": "Thinking Forth",
        "author": "Leo Brodie"
    }]
}"""
        let str2 = """{
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
            Assert.Equal("Object difference at $.books[0].\nRight only property: 'edition' (string).", diff1)
            Assert.Equal("String value difference at $.books[1].author: Leo Brodie vs Chuck Moore.", diff2)
        | _ -> failwithf "Expected 2 diffs but was %d." (List.length diffs)

    [<Fact>]
    let ``Long array example - with modifications``() =
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
    "authors": [ "Daniel P. Friedman", "Matthias Felleisen" ]
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
    "authors": [ "Daniel P. Friedman", "Matthias Felleisen" ]
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
        let diffs = JsonStrings.textDiff str1 str2
        match diffs with
        | [ diff ] ->
            Assert.Equal("Array difference at $.\n - [2] (an object)\n + [2] (an object)\n + [7] (an object)", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
