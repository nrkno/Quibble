namespace Quibble

open System.Text.Json

type JsonValue =
    | JsonUndefined
    | JsonNull
    | JsonTrue
    | JsonFalse
    | JsonNumber of (double * string)
    | JsonString of string
    | JsonArray of JsonValue list
    | JsonObject of (string * JsonValue) list
        
module JsonParse =

    let rec private toJsonValue (element : JsonElement) : JsonValue =
        match element.ValueKind with
        | JsonValueKind.True -> JsonTrue
        | JsonValueKind.False -> JsonFalse
        | JsonValueKind.Number ->
            let number = element.GetDouble()
            let rawText = element.GetRawText()
            JsonNumber (number, rawText)
        | JsonValueKind.String ->
            element.GetString() |> JsonString
        | JsonValueKind.Array ->
            element.EnumerateArray()
            |> Seq.map toJsonValue
            |> Seq.toList
            |> JsonArray
        | JsonValueKind.Object ->
            element.EnumerateObject()
            |> Seq.map (fun prop -> (prop.Name, toJsonValue prop.Value))
            |> Seq.toList
            |> JsonObject
        | JsonValueKind.Null -> JsonNull
        | JsonValueKind.Undefined
        | _ -> JsonUndefined
        
    let Parse (s : string) : JsonValue =
         let d = JsonDocument.Parse(s)
         d.RootElement |> toJsonValue 
