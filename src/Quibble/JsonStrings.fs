namespace Quibble

module JsonStrings =

    open System.Collections.Generic

    let Diff (jsonString1: string, jsonString2: string): IEnumerable<Diff> =
        let v1 = JsonParse.Parse(jsonString1)
        let v2 = JsonParse.Parse(jsonString2)
        JsonDiff.OfValues v1 v2 |> List.toSeq
        
    let Verify (actualJsonString: string, expectedJsonString: string): IEnumerable<string> =
        Diff(actualJsonString, expectedJsonString)
        |> Seq.map DiffMessage.toDiffMessage
