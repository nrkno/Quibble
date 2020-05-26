namespace Quibble

module JsonStrings =

    open System.Collections.Generic

    let Diff (s1: string, s2: string): IEnumerable<string> =
        let v1 = JsonParse.Parse(s1)
        let v2 = JsonParse.Parse(s2)
        let diffs = JsonDiff.OfValues v1 v2
        diffs |> Seq.map DiffMessage.toDiffMessage
