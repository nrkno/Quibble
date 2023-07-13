namespace Quibble

module JsonStrings =

    open System
    
    let parse (s : string) (paramName : string) : JsonValue =
        try
            JsonParse.Parse(s)
        with
            ex ->
                let message = sprintf "Failed to parse the value passed for %s as a JSON value. See inner exception for details." paramName
                raise (new ArgumentException(message, paramName, ex))
    
    let diff (leftJsonString: string) (rightJsonString: string): Diff list =
        let leftValue = parse leftJsonString "leftJsonString"
        let rightValue = parse rightJsonString "rightJsonString"
        JsonDiff.OfValues leftValue rightValue

    let textDiff (leftJsonString: string) (rightJsonString: string): string list =
        diff leftJsonString rightJsonString |> List.map DiffMessage.toDiffMessage
        