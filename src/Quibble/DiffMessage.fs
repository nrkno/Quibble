namespace Quibble

module DiffMessage =

    let private toValueDescription (jv: JsonValue): string =
        match jv with
        | JsonValue.True -> "the boolean true"
        | JsonValue.False -> "the boolean false"
        | JsonValue.String s -> sprintf "the string %s" s
        | JsonValue.Number (_, t) -> sprintf "the number %s" t
        | JsonValue.Array items ->
            let itemCount = items |> List.length
            match itemCount with
            | 0 -> "an empty array"
            | 1 -> "an array with 1 item"
            | _ -> sprintf "an array with %i items" itemCount
        | JsonValue.Object _ -> "an object"
        | JsonValue.Null -> "null"
        | _ -> "something else"

    let toDiffMessage (diff: Diff): string =
        match diff with
        | Properties ({ Path = path; Left = _; Right = _ }, mismatches) ->
            let propString (p: string, v: JsonValue): string =
                let typeStr =
                    match v with
                    | JsonValue.True
                    | JsonValue.False -> "bool"
                    | JsonValue.String _ -> "string"
                    | JsonValue.Number _ -> "number"
                    | JsonValue.Object _ -> "object"
                    | JsonValue.Array _ -> "array"
                    | JsonValue.Null -> "null"
                    | JsonValue.Undefined
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
        | Value { Path = path; Left = left; Right = right } ->
            match (left, right) with
            | (JsonValue.String leftStr, JsonValue.String rightStr) ->    
                let maxStrLen =
                    max (String.length rightStr) (String.length leftStr)
                let comparisonStr =
                    if maxStrLen > 30
                    then sprintf "    %s\nvs\n    %s" leftStr rightStr
                    else sprintf "%s vs %s." leftStr rightStr
                sprintf "String value difference at %s: %s" path comparisonStr
            | (JsonValue.Number (_, leftNumberText), JsonValue.Number (_, rightNumberText)) ->
                sprintf "Number value difference at %s: %s vs %s." path leftNumberText rightNumberText
            | _ -> sprintf "Some other value difference at %s." path
        | Type { Path = path; Left = left; Right = right } ->
            match (left, right) with
            | (JsonValue.True, JsonValue.False) ->
                sprintf "Boolean value difference at %s: true vs false." path
            | (JsonValue.False, JsonValue.True) ->
               sprintf "Boolean value difference at %s: false vs true." path
            | (_, _) ->
                let rightValueDescription = toValueDescription right
                let leftValueDescription = toValueDescription left
                sprintf "Type difference at %s: %s vs %s." path leftValueDescription rightValueDescription
        | ItemCount { Path = path; Left = left; Right = right } ->
            match (left, right) with
            | (Array leftItems, Array rightItems) ->
                let rightArrayLength = rightItems |> List.length
                let leftArrayLength = leftItems |> List.length 
                sprintf "Array length difference at %s: %d vs %d." path leftArrayLength rightArrayLength
            | _ ->
                failwith "A bug."
