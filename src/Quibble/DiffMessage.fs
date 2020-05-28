namespace Quibble

module DiffMessage =

    let private toValueDescription (e: JsonValue): string =
        match e with
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

                sprintf "%s (%s)" p typeStr

            let justMissing =
                function
                | RightOnlyProperty (n, v) -> Some (n, v)
                | LeftOnlyProperty _ -> None

            let justAdditional =
                function
                | RightOnlyProperty _ -> None
                | LeftOnlyProperty (n, v) -> Some (n, v)

            let additionals: string list =
                mismatches
                |> List.choose justAdditional
                |> List.map propString

            let missings: string list =
                mismatches
                |> List.choose justMissing
                |> List.map propString

            let maybeAdditionalsStr =
                if additionals.IsEmpty then
                    None
                else
                    let text =
                        if List.length additionals = 1 then "property" else "properties"

                    Some
                    <| sprintf "Additional %s:\n%s." text (String.concat "\n" additionals)

            let maybeMissingsStr =
                if missings.IsEmpty then
                    None
                else
                    let text =
                        if List.length missings = 1 then "property" else "properties"

                    Some
                    <| sprintf "Missing %s:\n%s." text (String.concat "\n" missings)

            let details =
                [ maybeAdditionalsStr
                  maybeMissingsStr ]
                |> List.choose id
                |> String.concat "\n"

            sprintf "Object mismatch at %s.\n%s" path details
        | Value { Path = path; Left = actual; Right = expected } ->
            match (actual, expected) with
            | (JsonValue.String actualStr, JsonValue.String expectedStr) -> 
                let maxStrLen =
                    max (String.length expectedStr) (String.length actualStr)
                let comparisonStr =
                    if maxStrLen > 30
                    then sprintf "Expected\n    %s\nbut was\n    %s" expectedStr actualStr
                    else sprintf "Expected %s but was %s." expectedStr actualStr
                sprintf "String value mismatch at %s.\n%s" path comparisonStr
            | (JsonValue.Number (_, actualNumberText), JsonValue.Number (_, expectedNumberText)) ->
                sprintf "Number value mismatch at %s.\nExpected %s but was %s." path expectedNumberText actualNumberText
            | _ -> sprintf "Some other value mismatch at %s." path
        | Type { Path = path; Left = actual; Right = expected } ->
            match (actual, expected) with
            | (JsonValue.True, JsonValue.False) ->
                sprintf "Boolean value mismatch at %s.\nExpected false but was true." path
            | (JsonValue.False, JsonValue.True) ->
               sprintf "Boolean value mismatch at %s.\nExpected true but was false." path
            | (_, _) ->
                let expectedMessage = toValueDescription expected
                let actualMessage = toValueDescription actual
                sprintf "Type mismatch at %s.\nExpected %s but was %s." path expectedMessage actualMessage
        | ItemCount { Path = path; Left = actual; Right = expected } ->
            match (actual, expected) with
            | (Array actualItems, Array expectedItems) ->
                let expectedLength = expectedItems |> List.length
                let actualLength = actualItems |> List.length 

                let itemsStr =
                    if expectedLength = 1 then "item" else "items"

                sprintf "Array length mismatch at %s.\nExpected %d %s but was %d." path expectedLength itemsStr actualLength
            | _ ->
                failwith "A bug."
