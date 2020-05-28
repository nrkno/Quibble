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
        Assert.Equal("LOL", diff)
        
    [<Fact>]
    let ``1 != 1E1 : number value mismatch``() = 
        let diff = JsonStrings.textDiff "1" "1E1" |> List.head
        Assert.Equal("LOL", diff)
        
    [<Fact>]
    let ``1 = 1.0``() =
        let diffs = JsonStrings.textDiff "1" "1.0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``true != 1 : Kind mismatch``() =
        let diffs = JsonStrings.textDiff "true" "1"
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
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
            Assert.Equal("LOL", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``null vs 1``() = 
        let diff = JsonStrings.textDiff "null" "1" |> List.head
        Assert.Equal("LOL", diff)

    [<Fact>]
    let ``Empty array vs null``() = 
        let diff = JsonStrings.textDiff "[]" "null" |> List.head
        Assert.Equal("LOL", diff)
            
    [<Fact>]
    let ``Empty array vs empty object``() = 
        let diffs = JsonStrings.textDiff "[]" "{}"
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 2 ]``() = 
        let diffs = JsonStrings.textDiff "[ 1 ]" "[ 2 ]"
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 1, 2 ]``() =
        let diffs = JsonStrings.textDiff "[ 1 ]" "[ 1, 2 ]"
        match diffs with 
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] != [ 2, 1 ] : array length mismatch``() = 
        let diffs = JsonStrings.textDiff "[ 1 ]" "[ 2, 1 ]"
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwith "Wrong number of diffs"
            
    [<Fact>]
    let ``[ 2, 1 ] vs [ 1, 2 ]``() =
        let diffs = JsonStrings.textDiff "[ 2, 1 ]" "[ 1, 2 ]"
        match diffs with
        | [ diff1; diff2 ] ->
            Assert.Equal("LOL", diff1)
            Assert.Equal("LOL", diff2)
        | _ -> failwithf "Expected 2 diffs but was %d." (List.length diffs)
        
    [<Fact>]
    let ``{} != { "count": 0 }``() =
        let diffs = JsonStrings.textDiff "{}" "{ \"count\": 0 }"
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwithf "Expected 1 diff but got %d." (List.length diffs)

    [<Fact>]
    let ``{ "count": 0 } != {}``() =
        let diffs = JsonStrings.textDiff "{ \"count\": 0 }" "{}"
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``{ "age": 20, "name": "Don" } = { "name": "Don", "age": 20 }``() =
        let diffs = JsonStrings.textDiff """{ "age": 20, "name": "Don" }""" """{ "name": "Don", "age": 20 }"""
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``Compare object with array``() =
        let s1 = "{ \"my array\": [ 1, 2, 3 ] }"
        let s2 = "{ \"my array\": [ 1, 2, 4 ] }"
        let diffs = JsonStrings.textDiff s1 s2
        match diffs with
        | [ diff ] ->
            Assert.Equal("LOL", diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

