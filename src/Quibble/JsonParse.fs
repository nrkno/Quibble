namespace Quibble

open System.Text.Json

type JsonValue =
    | Undefined
    | Null
    | True
    | False
    | Number of (double * string)
    | String of string
    | Array of JsonValue list
    | Object of (string * JsonValue) list 

module JsonParse =

    let rec private toJsonValue (element : JsonElement) : JsonValue =
        match element.ValueKind with
        | JsonValueKind.True -> JsonValue.True
        | JsonValueKind.False -> JsonValue.False
        | JsonValueKind.Number ->
            let number = element.GetDouble()
            let rawText = element.GetRawText()
            JsonValue.Number (number, rawText)
        | JsonValueKind.String ->
            element.GetString() |> JsonValue.String
        | JsonValueKind.Array ->
            element.EnumerateArray()
            |> Seq.map toJsonValue
            |> Seq.toList
            |> JsonValue.Array
        | JsonValueKind.Object ->
            element.EnumerateObject()
            |> Seq.map (fun prop -> (prop.Name, toJsonValue prop.Value))
            |> Seq.toList
            |> JsonValue.Object
        | JsonValueKind.Null -> JsonValue.Null
        | JsonValueKind.Undefined
        | _ -> JsonValue.Undefined
        
    let Parse (s : string) : JsonValue =
         let d = JsonDocument.Parse(s)
         d.RootElement |> toJsonValue 
