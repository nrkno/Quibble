namespace Quibble.FSharp.UnitTests

module JsonStringsDiffTests =

    open Xunit
    open Xunit.Sdk
    open Quibble

    [<Fact>]
    let ``true = true`` () =
        let diffs = JsonStrings.diff "true" "true"
        Assert.Empty(diffs)

    [<Fact>]
    let ``false = false`` () =
        let diffs = JsonStrings.diff "false" "false"
        Assert.Empty(diffs)

    [<Fact>]
    let ``0 = 0`` () =
        let diffs = JsonStrings.diff "0" "0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``0 = -0`` () =
        let diffs = JsonStrings.diff "0" "-0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``1000 = 1E3`` () =
        let diffs = JsonStrings.diff "1000" "1E3"
        Assert.Empty(diffs)

    [<Fact>]
    let ``123.4 = 1.234E2`` () =
        let diffs = JsonStrings.diff "123.4" "1.234E2"
        Assert.Empty(diffs)

    [<Fact>]
    let ``1 != 2 : JsonNumber ValueDiff mismatch`` () =
        let diff = JsonStrings.diff "1" "2" |> List.head
        match diff with
        | ValueDiff { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(JsonNumber(1., "1"), left)
            Assert.Equal(JsonNumber(2., "2"), right)
        | _ ->
            raise
            <| XunitException(sprintf "Unexpected type of diff! %A" diff)

    [<Fact>]
    let ``1 != 1E1 : JsonNumber ValueDiff mismatch`` () =
        let diff = JsonStrings.diff "1" "1E1" |> List.head
        match diff with
        | ValueDiff { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(JsonNumber(1., "1"), left)
            Assert.Equal(JsonNumber(10., "1E1"), right)
        | _ ->
            raise
            <| XunitException(sprintf "Unexpected type of diff! %A" diff)

    [<Fact>]
    let ``1 = 1.0`` () =
        let diffs = JsonStrings.diff "1" "1.0"
        Assert.Empty(diffs)

    [<Fact>]
    let ``true != 1 : Kind mismatch`` () =
        let diffs = JsonStrings.diff "true" "1"
        match diffs with
        | [ diff ] ->
            match diff with
            | TypeDiff { Path = path; Left = left; Right = right } ->
                Assert.Equal("$", path)
                Assert.Equal(JsonTrue, left)
                Assert.Equal(JsonNumber(1., "1"), right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong JsonNumber of diffs"

    [<Fact>]
    let ``"foo" = "foo"`` () =
        let diffs = JsonStrings.diff "\"foo\"" "\"foo\""
        Assert.Empty(diffs)

    [<Fact>]
    let ``"foo" != "bar"`` () =
        let diffs = JsonStrings.diff "\"foo\"" "\"bar\""
        match diffs with
        | [ diff ] ->
            match diff with
            | ValueDiff { Path = path; Left = left; Right = right } ->
                Assert.Equal("$", path)
                Assert.Equal(JsonString "foo", left)
                Assert.Equal(JsonString "bar", right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

    [<Fact>]
    let ``null vs 1`` () =
        let diff = JsonStrings.diff "null" "1" |> List.head
        match diff with
        | TypeDiff { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(JsonNull, left)
            Assert.Equal(JsonNumber(1., "1"), right)
        | _ ->
            raise
            <| XunitException(sprintf "Unexpected type of diff! %A" diff)

    [<Fact>]
    let ``Empty JsonArray vs null`` () =
        let diff =
            JsonStrings.diff "[]" "null" |> List.head

        match diff with
        | TypeDiff { Path = path; Left = left; Right = right } ->
            Assert.Equal("$", path)
            Assert.Equal(JsonArray [], left)
            Assert.Equal(JsonNull, right)
        | _ ->
            raise
            <| XunitException(sprintf "Unexpected type of diff! %A" diff)

    [<Fact>]
    let ``Empty JsonArray vs empty JsonObject`` () =
        let diffs = JsonStrings.diff "[]" "{}"
        match diffs with
        | [ diff ] ->
            match diff with
            | TypeDiff { Path = path; Left = left; Right = right } ->
                Assert.Equal("$", path)
                Assert.Equal(JsonArray [], left)
                Assert.Equal(JsonObject [], right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong JsonNumber of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 2 ]`` () =
        let diffs = JsonStrings.diff "[ 1 ]" "[ 2 ]"
        match diffs with
        | [ diff ] ->
            match diff with
            | ValueDiff { Path = path; Left = left; Right = right } ->
                Assert.Equal("$[0]", path)
                Assert.Equal(JsonNumber(1., "1"), left)
                Assert.Equal(JsonNumber(2., "2"), right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong JsonNumber of diffs"

    [<Fact>]
    let ``[ 1 ] vs [ 1, 2 ]`` () =
        let diffs = JsonStrings.diff "[ 1 ]" "[ 1, 2 ]"
        match diffs with
        | [ diff ] ->
            match diff with
            | ArrayDiff ({ Path = path; Left = left; Right = right }, mismatches) ->
                Assert.Equal("$", path)
                Assert.Equal(JsonArray [ JsonNumber(1., "1") ], left)
                Assert.Equal(JsonArray [ JsonNumber(1., "1"); JsonNumber(2., "2") ], right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong JsonNumber of diffs"

    [<Fact>]
    let ``[ 1 ] != [ 2, 1 ] : JsonArray length mismatch`` () =
        let diffs = JsonStrings.diff "[ 1 ]" "[ 2, 1 ]"
        match diffs with
        | [ diff ] ->
            match diff with
            | ArrayDiff ({ Path = path; Left = left; Right = right }, mismatches) ->
                Assert.Equal("$", path)
                Assert.Equal(JsonArray [ JsonNumber(1., "1") ], left)
                Assert.Equal(JsonArray [ JsonNumber(2., "2"); JsonNumber(1., "1") ], right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwith "Wrong JsonNumber of diffs"

    [<Fact>]
    let ``[ 2, 1 ] vs [ 1, 2 ]`` () =
        let diffs = JsonStrings.diff "[ 2, 1 ]" "[ 1, 2 ]"
        match diffs with
        | [ diff1 ] ->
            match diff1 with
            | ArrayDiff ({ Path = path; Left = left; Right = right }, mismatches) ->
                Assert.Equal("$", path)
                Assert.Equal(JsonArray [ JsonNumber(2., "2"); JsonNumber(1., "1") ], left)
                Assert.Equal(JsonArray [ JsonNumber(1., "1"); JsonNumber(2., "2") ], right)
                // TODO: Compare mismatches as well.
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff1)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

    [<Fact>]
    let ``{} != { "count": 0 }`` () =
        let diffs = JsonStrings.diff "{}" "{ \"count\": 0 }"
        match diffs with
        | [ diff ] ->
            match diff with
            | ObjectDiff ({ Path = path; Left = left; Right = right }, properties) ->
                Assert.Equal("$", path)
                Assert.Equal(JsonObject [], left)
                Assert.Equal(JsonObject [ ("count", JsonNumber(0., "0")) ], right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but got %d." (List.length diffs)

    [<Fact>]
    let ``{ "count": 0 } != {}`` () =
        let diffs = JsonStrings.diff "{ \"count\": 0 }" "{}"
        match diffs with
        | [ diff ] ->
            match diff with
            | ObjectDiff ({ Path = path; Left = left; Right = right }, properties) ->
                Assert.Equal("$", path)
                Assert.Equal(JsonObject [ ("count", JsonNumber(0., "0")) ], left)
                Assert.Equal(JsonObject [], right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

    [<Fact>]
    let ``{ "age": 20, "name": "Don" } = { "name": "Don", "age": 20 }`` () =
        let diffs =
            JsonStrings.diff """{ "age": 20, "name": "Don" }""" """{ "name": "Don", "age": 20 }"""

        Assert.Empty(diffs)

    [<Fact>]
    let ``Compare JsonObject with JsonArray`` () =
        let s1 = "{ \"my JsonArray\": [ 1, 2, 3 ] }"
        let s2 = "{ \"my JsonArray\": [ 1, 2, 4 ] }"
        let diffs = JsonStrings.diff s1 s2
        match diffs with
        | [ diff ] ->
            match diff with
            | ValueDiff { Path = path; Left = left; Right = right } ->
                Assert.Equal("$['my JsonArray'][2]", path)
                Assert.Equal(JsonNumber(3., "3"), left)
                Assert.Equal(JsonNumber(4., "4"), right)
            | _ ->
                raise
                <| XunitException(sprintf "Unexpected type of diff! %A" diff)
        | _ -> failwithf "Expected 1 diff but was %d." (List.length diffs)

    [<Fact>]
    let ``Number example: 1 != 2`` () =
        let actualDiffs = JsonStrings.diff "1" "2"

        let expectedDiffs =
            [ ValueDiff
                { Path = "$"
                  Left = JsonNumber(1., "1")
                  Right = JsonNumber(2., "2") } ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))

    [<Fact>]
    let ``Array example: JsonNumber of ArrayDiff`` () =
        let actualDiffs = JsonStrings.diff "[ 3 ]" "[ 3, 7 ]"

        let expectedDiffs =
            [ ArrayDiff ({ Path = "$"
                           Left = JsonArray [ JsonNumber(3., "3") ]
                           Right = JsonArray [ JsonNumber(3., "3"); JsonNumber(7., "7") ] }, 
                     [ RightOnlyItem (1, JsonNumber(7., "7")) ]) ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))

    [<Fact>]
    let ``Array example: order matters`` () =
        let actualDiffs =
            JsonStrings.diff "[ 24, 12 ]" "[ 12, 24 ]"

        let expectedDiffs =
            let expectedDiffPoint =
                  { Path = "$"
                    Left = JsonArray [ JsonNumber(24., "24"); JsonNumber(12., "12") ]
                    Right = JsonArray [ JsonNumber(12., "12"); JsonNumber(24., "24") ] }
            let expectedMismatches = [
                LeftOnlyItem (0, JsonNumber(24., "24"))
                RightOnlyItem (1, JsonNumber(24., "24"))
            ]
            [ ArrayDiff (expectedDiffPoint, expectedMismatches) ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))

    [<Fact>]
    let ``Array example: more ArrayDiff``() =
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
        let actualDiffs = JsonStrings.diff str1 str2
        
        let expectedDiffs =
            [ ArrayDiff ({ Path = "$"
                           Left = JsonArray [ JsonObject [("title", JsonString "Data and Reality"); ("author", JsonString "William Kent")];
                                              JsonObject [("title", JsonString "Thinking Forth"); ("author", JsonString "Leo Brodie")];
                                              JsonObject [("title", JsonString "Programmers at Work"); ("author", JsonString "Susan Lammers")];
                                              JsonObject [("title", JsonString "The Little Schemer"); ("authors", JsonArray [ JsonString "Daniel P. Friedman"; JsonString "Matthias Felleisen"])];
                                              JsonObject [("title", JsonString "Object Design"); ("authors", JsonArray [JsonString "Rebecca Wirfs-Brock"; JsonString "Alan McKean"])];
                                              JsonObject [("title", JsonString "Domain Modelling made Functional"); ("author", JsonString "Scott Wlaschin")];
                                              JsonObject [("title", JsonString "The Psychology of Computer Programming"); ("author", JsonString "Gerald M. Weinberg")];
                                              JsonObject [("title", JsonString "Exercises in Programming Style"); ("author", JsonString "Cristina Videira Lopes")];
                                              JsonObject [("title", JsonString "Land of Lisp"); ("author", JsonString "Conrad Barski")]]
                           Right = JsonArray [ JsonObject [("title", JsonString "Data and Reality"); ("author", JsonString "William Kent")];
                                               JsonObject [("title", JsonString "Thinking Forth"); ("author", JsonString "Leo Brodie")];
                                               JsonObject [("title", JsonString "Coders at Work"); ("author", JsonString "Peter Seibel")];
                                               JsonObject [("title", JsonString "The Little Schemer"); ("authors", JsonArray [JsonString "Daniel P. Friedman"; JsonString "Matthias Felleisen"])];
                                               JsonObject [("title", JsonString "Object Design"); ("authors", JsonArray [JsonString "Rebecca Wirfs-Brock"; JsonString "Alan McKean"])];
                                               JsonObject [("title", JsonString "Domain Modelling made Functional"); ("author", JsonString "Scott Wlaschin")];
                                               JsonObject [("title", JsonString "The Psychology of Computer Programming"); ("author", JsonString "Gerald M. Weinberg")];
                                               JsonObject [("title", JsonString "Turtle Geometry"); ("authors", JsonArray [JsonString "Hal Abelson"; JsonString "Andrea diSessa"])];
                                               JsonObject [("title", JsonString "Exercises in Programming Style"); ("author", JsonString "Cristina Videira Lopes")];
                                               JsonObject [("title", JsonString "Land of Lisp"); ("author", JsonString "Conrad Barski")]] },
                     [ LeftOnlyItem (2, JsonObject [("title", JsonString "Programmers at Work"); ("author", JsonString "Susan Lammers")]);
                       RightOnlyItem (2, JsonObject [("title", JsonString "Coders at Work"); ("author", JsonString "Peter Seibel")]);
                       RightOnlyItem (7, JsonObject [("title", JsonString "Turtle Geometry"); ("authors", JsonArray [JsonString "Hal Abelson"; JsonString "Andrea diSessa"])])]) ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))
        
    [<Fact>]
    let ``Object example: property differences`` () =
        let str1 =
            """{ "item": "widget", "price": 12.20 }"""

        let str2 =
            """{ "item": "widget", "quantity": 88, "in stock": true }"""

        let actualDiffs = JsonStrings.diff str1 str2

        let expectedDiffs =
            [ ObjectDiff
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

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))

    [<Fact>]
    let ``Object example: property with spaces`` () =
        let str1 =
            """{ "name": "Maya", "date of birth": "1999-04-23" }"""

        let str2 =
            """{ "name": "Maya", "date of birth": "1999-04-24" }"""

        let actualDiffs = JsonStrings.diff str1 str2

        let expectedDiffs =
            [ ValueDiff
                { Path = "$['date of birth']"
                  Left = JsonString "1999-04-23"
                  Right = JsonString "1999-04-24" } ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))


    [<Fact>]
    let ``Composite example: books`` () =
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

        let actualDiffs = JsonStrings.diff str1 str2

        let expectedDiffs =
            [ ObjectDiff
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
              ValueDiff
                  { Path = "$.books[1].author"
                    Left = JsonString "Leo Brodie"
                    Right = JsonString "Chuck Moore" } ]

        Assert.Equal(List.length expectedDiffs, List.length actualDiffs)
        List.zip expectedDiffs actualDiffs
        |> List.iter (fun (expected, actual) -> Assert.Equal(expected, actual))
