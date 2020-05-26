namespace Quibble.FSharp.UnitTests

module JsonDiffTests =

    open System.Text.Json
    open Xunit
    open Quibble

    [<Fact>]
    let ``Test OfValues true vs false`` () =
        let v1 = JsonParse.Parse("true")
        let v2 = JsonParse.Parse("false")

        let diffs = JsonDiff.OfValues v1 v2 |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.Equal(JsonValue.True, kindDiff.Left)
            Assert.Equal(JsonValue.False, kindDiff.Right)
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfValues 1 vs 2`` () =
        let v1 = JsonParse.Parse("1")
        let v2 = JsonParse.Parse("2")

        let diffs = JsonDiff.OfValues v1 v2 |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Value valueDiff ->
            Assert.Equal("$", valueDiff.Path)
            Assert.Equal(JsonValue.Number (1., "1"), valueDiff.Left)
            Assert.Equal(JsonValue.Number (2., "2"), valueDiff.Right)
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfDocuments true vs 1`` () =
        let v1 = JsonParse.Parse("true")
        let v2 = JsonParse.Parse("1")

        let diffs = JsonDiff.OfValues v1 v2 |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.Equal(JsonValue.True, kindDiff.Left)
            Assert.Equal(JsonValue.Number (1., "1"), kindDiff.Right)
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfDocuments null vs 1`` () =
        let v1 = JsonParse.Parse("null")
        let v2 = JsonParse.Parse("1")

        let diffs = JsonDiff.OfValues v1 v2 |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.Equal(JsonValue.Null, kindDiff.Left)
            Assert.Equal(JsonValue.Number (1., "1"), kindDiff.Right)
        | _ -> failwith "Wrong diff"
