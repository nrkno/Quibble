namespace Quibble.FSharp.UnitTests

module JsonStringsDiffTests = 

    open Xunit
    open Xunit.Sdk
    open Quibble

    [<Fact>]
    let ``true = true``() = 
        let diffs = JsonStrings.diff "true" "true"
        Assert.Empty(diffs)

    [<Fact>]
    let ``false = false``() = 
        let diffs = JsonStrings.diff "false" "false"
        Assert.Empty(diffs)

    [<Fact>]
    let ``0 = 0``() = 
        let diffs = JsonStrings.diff "0" "0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``0 = -0``() = 
        let diffs = JsonStrings.diff "0" "-0"
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``1000 = 1E3``() = 
        let diffs = JsonStrings.diff "1000" "1E3"
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``123.4 = 1.234E2``() = 
        let diffs = JsonStrings.diff "123.4" "1.234E2"
        Assert.Empty(diffs)

    [<Fact>]
    let ``1 != 2 : number value mismatch``() = 
        let diff = JsonStrings.diff "1" "2" |> List.head
        match diff with
        | Value { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(Number (1., "1"), left)
            Assert.Equal(Number (2., "2"), right)
        | _ ->
            raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        
    [<Fact>]
    let ``1 != 1E1 : number value mismatch``() = 
        let diff = JsonStrings.diff "1" "1E1" |> List.head
        match diff with
        | Value { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(Number (1., "1"), left)
            Assert.Equal(Number (10., "1E1"), right)
        | _ ->
            raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        
    [<Fact>]
    let ``1 = 1.0``() =
        let diffs = JsonStrings.diff "1" "1.0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``true != 1 : Kind mismatch``() =
        let diffs = JsonStrings.diff "true" "1"
        match diffs with
        | [ diff ] ->
            match diff with
            | Type { Path = path; Left = left; Right = right } ->
               Assert.Equal("$", path)
               Assert.Equal(True, left)
               Assert.Equal(Number (1., "1"), right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``"foo" = "foo"``() = 
        let diffs = JsonStrings.diff "\"foo\"" "\"foo\""
        Assert.Empty(diffs)

    [<Fact>]
    let ``"foo" != "bar"``() = 
        let diffs = JsonStrings.diff "\"foo\"" "\"bar\""
        match diffs with
        | [ diff ] ->
            match diff with
            | Value { Path = path; Left = left; Right = right } ->
               Assert.Equal("$", path)
               Assert.Equal(String "foo", left)
               Assert.Equal(String "bar", right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``null vs 1``() = 
        let diff = JsonStrings.diff "null" "1" |> List.head
        match diff with
        | Type { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(Null, left)
            Assert.Equal(Number (1., "1"), right)
        | _ ->
            raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)

    [<Fact>]
    let ``Empty array vs null``() = 
        let diff = JsonStrings.diff "[]" "null" |> List.head
        match diff with
        | Type { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(Array [], left)
            Assert.Equal(Null, right)
        | _ ->
            raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
            
    [<Fact>]
    let ``Empty array vs empty object``() = 
        let diffs = JsonStrings.diff "[]" "{}"
        match diffs with
        | [ diff ] ->
            match diff with
            | Type { Path = path; Left = left; Right = right } ->
                Assert.Equal("$", path)
                Assert.Equal(Array [], left)
                Assert.Equal(Object [], right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 2 ]``() = 
        let diffs = JsonStrings.diff "[ 1 ]" "[ 2 ]"
        match diffs with
        | [ diff ] ->
            match diff with
            | Value { Path = path; Left = left; Right = right } ->
                Assert.Equal("$[0]", path)
                Assert.Equal(Number (1., "1"), left)
                Assert.Equal(Number (2., "2"), right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 1, 2 ]``() =
        let diffs = JsonStrings.diff "[ 1 ]" "[ 1, 2 ]"
        match diffs with 
        | [ diff ] ->
            match diff with
            | ItemCount { Path = path; Left = left; Right = right } ->
                Assert.Equal("$", path)
                Assert.Equal(Array [ Number (1., "1") ], left)
                Assert.Equal(Array [ Number (1., "1"); Number (2., "2") ], right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong number of diffs"

    [<Fact>]
    let ``[ 1 ] != [ 2, 1 ] : array length mismatch``() = 
        let diffs = JsonStrings.diff "[ 1 ]" "[ 2, 1 ]"
        match diffs with
        | [ diff ] ->
            match diff with
            | ItemCount { Path = path; Left = left; Right = right } ->
                Assert.Equal("$", path)
                Assert.Equal(Array [ Number (1., "1") ], left)
                Assert.Equal(Array [ Number (2., "2"); Number (1., "1") ], right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong number of diffs"
            
    [<Fact>]
    let ``[ 2, 1 ] vs [ 1, 2 ]``() =
        let diffs = JsonStrings.diff "[ 2, 1 ]" "[ 1, 2 ]"
        match diffs with
        | [ diff1; diff2 ] ->
            match diff1 with
            | Value { Path = path; Left = left; Right = right } ->
                Assert.Equal("$[0]", path)
                Assert.Equal(Number (2., "2"), left)
                Assert.Equal(Number (1., "1"), right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff1)
            match diff2 with
            | Value { Path = path; Left = left; Right = right } ->
                Assert.Equal("$[1]", path)
                Assert.Equal(Number (1., "1"), left)
                Assert.Equal(Number (2., "2"), right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff2)            
        | _ -> failwithf "Expected 2 diffs but was %d." (List.length diffs)
        
    [<Fact>]
    let ``{} != { "count": 0 }``() =
        let diffs = JsonStrings.diff "{}" "{ \"count\": 0 }"
        match diffs with
        | [ diff ] ->
            match diff with
            | Properties ({ Path = path; Left = left; Right = right }, properties) ->
                Assert.Equal("$", path)
                Assert.Equal(Object [], left)
                Assert.Equal(Object [ ("count", Number (0., "0")) ], right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but got %d." (List.length diffs)

    [<Fact>]
    let ``{ "count": 0 } != {}``() =
        let diffs = JsonStrings.diff "{ \"count\": 0 }" "{}"
        match diffs with
        | [ diff ] ->
            match diff with
            | Properties ({ Path = path; Left = left; Right = right }, properties) ->
                Assert.Equal("$", path)
                Assert.Equal(Object [ ("count", Number (0., "0")) ], left)
                Assert.Equal(Object [], right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)
        
    [<Fact>]
    let ``{ "age": 20, "name": "Don" } = { "name": "Don", "age": 20 }``() =
        let diffs = JsonStrings.diff """{ "age": 20, "name": "Don" }""" """{ "name": "Don", "age": 20 }"""
        Assert.Empty(diffs)
        
    [<Fact>]
    let ``Compare object with array``() =
        let s1 = "{ \"my array\": [ 1, 2, 3 ] }"
        let s2 = "{ \"my array\": [ 1, 2, 4 ] }"
        let diffs = JsonStrings.diff s1 s2
        match diffs with
        | [ diff ] ->
            match diff with
            | Value { Path = path; Left = left; Right = right } ->
                Assert.Equal("$['my array'][2]", path)
                Assert.Equal(Number (3., "3"), left)
                Assert.Equal(Number (4., "4"), right)
            | _ ->
                raise <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

