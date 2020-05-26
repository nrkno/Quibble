namespace Quibble

module JsonStrings =

    open System.Collections.Generic
    open System.Text.Json

    let Diff (s1: string, s2: string): IEnumerable<string> =
        use d1 = JsonDocument.Parse(s1)
        use d2 = JsonDocument.Parse(s2)
        let diffs = JsonDiff.OfDocuments(d1, d2)

        let messages =
            diffs
            |> Seq.map DiffMessage.toDiffMessage
            |> Seq.toList // Ensure evaluation.

        messages |> List.toSeq // Interop
