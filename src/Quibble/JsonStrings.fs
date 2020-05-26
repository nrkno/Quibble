namespace Quibble

module JsonStrings =

    open System.Collections.Generic

    let Diff (s1: string, s2: string): IEnumerable<Diff> =
        let v1 = JsonParse.Parse(s1)
        let v2 = JsonParse.Parse(s2)
        JsonDiff.OfValues v1 v2 |> List.toSeq
