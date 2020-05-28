namespace Quibble

module JsonStrings =

    let diff (jsonString1: string) (jsonString2: string): Diff list =
        let v1 = JsonParse.Parse(jsonString1)
        let v2 = JsonParse.Parse(jsonString2)
        JsonDiff.OfValues v1 v2

    let textDiff (jsonString1: string) (jsonString2: string): string list =
        diff jsonString1 jsonString2 |> List.map DiffMessage.toDiffMessage
        