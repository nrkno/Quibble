namespace Quibble

module DiffMessage =

    let private toValueDescription (jv: JsonValue): string =
        match jv with
        | JsonTrue -> "the boolean true"
        | JsonFalse -> "the boolean false"
        | JsonString s -> sprintf "the string %s" s
        | JsonNumber (_, t) -> sprintf "the number %s" t
        | JsonArray items ->
            let itemCount = items |> List.length
            match itemCount with
            | 0 -> "an empty array"
            | 1 -> "an array with 1 item"
            | _ -> sprintf "an array with %i items" itemCount
        | JsonObject _ -> "an object"
        | JsonNull -> "null"
        | _ -> "something else"

    let toDiffMessage (diff: Diff): string =
        match diff with
        | ObjectDiff ({ Path = path; Left = _; Right = _ }, mismatches) ->
            let propString (p: string, v: JsonValue): string =
                let typeStr =
                    match v with
                    | JsonTrue
                    | JsonFalse -> "bool"
                    | JsonString _ -> "string"
                    | JsonNumber _ -> "number"
                    | JsonObject _ -> "object"
                    | JsonArray _ -> "array"
                    | JsonNull -> "null"
                    | JsonUndefined
                    | _ -> "undefined"

                sprintf "'%s' (%s)" p typeStr

            let lefts: string list =
                let justLeft =
                    function
                    | RightOnlyProperty _ -> None
                    | LeftOnlyProperty (n, v) -> Some (n, v)
                mismatches
                |> List.choose justLeft
                |> List.map propString

            let rights: string list =
                let justRight =
                    function
                    | RightOnlyProperty (n, v) -> Some (n, v)
                    | LeftOnlyProperty _ -> None
                mismatches
                |> List.choose justRight
                |> List.map propString

            let maybeLeftOnlyStr =
                if lefts.IsEmpty then
                    None
                else
                    let text =
                        if List.length lefts = 1 then "property" else "properties"

                    Some
                    <| sprintf "Left only %s: %s." text (String.concat ", " lefts)

            let maybeRightOnlyStr =
                if rights.IsEmpty then
                    None
                else
                    let text =
                        if List.length rights = 1 then "property" else "properties"

                    Some
                    <| sprintf "Right only %s: %s." text (String.concat ", " rights)

            let details =
                [ maybeLeftOnlyStr
                  maybeRightOnlyStr ]
                |> List.choose id
                |> String.concat "\n"

            sprintf "Object difference at %s.\n%s" path details
        | ValueDiff { Path = path; Left = left; Right = right } ->
            match (left, right) with
            | (JsonString leftStr, JsonString rightStr) ->    
                let maxStrLen =
                    max (String.length rightStr) (String.length leftStr)
                let comparisonStr =
                    if maxStrLen > 30
                    then sprintf "    %s\nvs\n    %s" leftStr rightStr
                    else sprintf "%s vs %s." leftStr rightStr
                sprintf "String value difference at %s: %s" path comparisonStr
            | (JsonNumber (_, leftNumberText), JsonNumber (_, rightNumberText)) ->
                sprintf "Number value difference at %s: %s vs %s." path leftNumberText rightNumberText
            | _ -> sprintf "Some other value difference at %s." path
        | TypeDiff { Path = path; Left = left; Right = right } ->
            match (left, right) with
            | (JsonTrue, JsonFalse) ->
                sprintf "Boolean value difference at %s: true vs false." path
            | (JsonFalse, JsonTrue) ->
               sprintf "Boolean value difference at %s: false vs true." path
            | (_, _) ->
                let rightValueDescription = toValueDescription right
                let leftValueDescription = toValueDescription left
                sprintf "Type difference at %s: %s vs %s." path leftValueDescription rightValueDescription
        | ArrayDiff ({ Path = path; Left = left; Right = right }, mismatches) ->
            let toModification (itemMismatch : ItemMismatch) : string =
                let typeStr jv =
                    match jv with
                    | JsonTrue -> "the boolean true"
                    | JsonFalse -> "the boolean false"
                    | JsonString s ->
                        let truncate (maxlen : int) (str : string) =
                            if String.length str > maxlen then
                                let ellipses = "..."
                                let truncateAt = maxlen - String.length ellipses
                                sprintf "%s%s" (str.Substring(0, truncateAt)) ellipses
                            else str                                
                        sprintf "the string %s" (truncate 30 s)
                    | JsonNumber (_, t) -> sprintf "the number %s" t
                    | JsonObject _ -> "an object"
                    | JsonArray _ -> "an array"
                    | JsonNull -> "null"
                    | JsonUndefined
                    | _ -> "undefined"

                let toModificationLine (op : string) (ix : int) (jv : JsonValue) : string =
                    sprintf " %s [%d] (%s)" op ix (typeStr jv)
                    
                match itemMismatch with
                | LeftOnlyItem (ix, jv) -> toModificationLine "-" ix jv 
                | RightOnlyItem (ix, jv) -> toModificationLine "+" ix jv 
                        
            let details =
                mismatches |> List.map toModification |> String.concat "\n"

            sprintf "Array difference at %s.\n%s" path details
            
