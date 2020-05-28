namespace Quibble

module JsonStrings =

    let diff (leftJsonString: string) (rightJsonString: string): Diff list =
        let leftValue = JsonParse.Parse(leftJsonString)
        let rightValue = JsonParse.Parse(rightJsonString)
        JsonDiff.OfValues leftValue rightValue

    let textDiff (leftJsonString: string) (rightJsonString: string): string list =
        diff leftJsonString rightJsonString |> List.map DiffMessage.toDiffMessage
        