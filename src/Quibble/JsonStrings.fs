namespace Quibble

module JsonStrings =

    let diff (jsonString1: string) (jsonString2: string): Diff list =
        let v1 = JsonParse.Parse(jsonString1)
        let v2 = JsonParse.Parse(jsonString2)
        JsonDiff.OfValues v1 v2
        
    let verify (actualJsonString: string) (expectedJsonString: string): string list =
        diff actualJsonString expectedJsonString
        |> List.map AssertMessage.toDiffMessage
