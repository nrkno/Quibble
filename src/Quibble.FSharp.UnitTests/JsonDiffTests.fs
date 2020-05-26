namespace Quibble.FSharp.UnitTests

module JsonDiffTests =

    open System.Text.Json
    open Xunit
    open Quibble

    [<Fact>]
    let ``Test OfElements true vs false`` () =
        use d1 = JsonDocument.Parse("true")
        use d2 = JsonDocument.Parse("false")
        let e1 = d1.RootElement
        let e2 = d2.RootElement

        let diffs =
            JsonDiff.OfElements(e1, e2) |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.True(kindDiff.Left.GetBoolean())
            Assert.False(kindDiff.Right.GetBoolean())
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfDocuments true vs false`` () =
        use d1 = JsonDocument.Parse("true")
        use d2 = JsonDocument.Parse("false")

        let diffs =
            JsonDiff.OfDocuments(d1, d2) |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.Equal(JsonValueKind.True, kindDiff.Left.ValueKind)
            Assert.Equal(JsonValueKind.False, kindDiff.Right.ValueKind)
            Assert.True(kindDiff.Left.GetBoolean())
            Assert.False(kindDiff.Right.GetBoolean())
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfDocuments 1 vs 2`` () =
        use d1 = JsonDocument.Parse("1")
        use d2 = JsonDocument.Parse("2")

        let diffs =
            JsonDiff.OfDocuments(d1, d2) |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Value valueDiff ->
            Assert.Equal("$", valueDiff.Path)
            Assert.Equal(JsonValueKind.Number, valueDiff.Left.ValueKind)
            Assert.Equal(JsonValueKind.Number, valueDiff.Right.ValueKind)
            Assert.Equal(1, valueDiff.Left.GetInt32())
            Assert.Equal(2, valueDiff.Right.GetInt32())
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfDocuments true vs 1`` () =
        use d1 = JsonDocument.Parse("true")
        use d2 = JsonDocument.Parse("1")

        let diffs =
            JsonDiff.OfDocuments(d1, d2) |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.Equal(JsonValueKind.True, kindDiff.Left.ValueKind)
            Assert.Equal(JsonValueKind.Number, kindDiff.Right.ValueKind)
        | _ -> failwith "Wrong diff"

    [<Fact>]
    let ``Test OfDocuments null vs 1`` () =
        use d1 = JsonDocument.Parse("null")
        use d2 = JsonDocument.Parse("1")

        let diffs =
            JsonDiff.OfDocuments(d1, d2) |> Seq.toList

        Assert.NotEmpty(diffs)
        match diffs.Head with
        | Kind kindDiff ->
            Assert.Equal("$", kindDiff.Path)
            Assert.Equal(JsonValueKind.Null, kindDiff.Left.ValueKind)
            Assert.Equal(JsonValueKind.Number, kindDiff.Right.ValueKind)
        | _ -> failwith "Wrong diff"
