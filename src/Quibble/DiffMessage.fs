namespace Quibble

module DiffMessage =

    open System.Text.Json

    let private toValueDescription (e: JsonElement): string =
        match e.ValueKind with
        | JsonValueKind.True -> "the boolean true"
        | JsonValueKind.False -> "the boolean false"
        | JsonValueKind.String -> sprintf "the string %s" (e.GetString())
        | JsonValueKind.Number -> sprintf "the number %s" (e.GetRawText())
        | JsonValueKind.Array ->
            let itemCount = e.GetArrayLength()
            match itemCount with
            | 0 -> "an empty array"
            | 1 -> "an array with 1 item"
            | _ -> sprintf "an array with %i items" itemCount
        | JsonValueKind.Object -> "an object"
        | JsonValueKind.Null -> "null"
        | _ -> "something else"

    let toDiffMessage (diff: Diff): string =
        match diff with
        | Properties ({ Path = path; Left = actual; Right = expected }, mismatches) ->
            let propString (e: JsonElement) (p: string): string =
                let typeStr =
                    match e.GetProperty(p).ValueKind with
                    | JsonValueKind.True
                    | JsonValueKind.False -> "bool"
                    | JsonValueKind.String -> "string"
                    | JsonValueKind.Number -> "number"
                    | JsonValueKind.Object -> "object"
                    | JsonValueKind.Array -> "array"
                    | JsonValueKind.Null -> "null"
                    | JsonValueKind.Undefined
                    | _ -> "undefined"

                sprintf "%s (%s)" p typeStr

            let justMissing =
                function
                | MissingProperty p -> Some p
                | AdditionalProperty _ -> None

            let justAdditional =
                function
                | MissingProperty _ -> None
                | AdditionalProperty p -> Some p

            let additionals: string list =
                mismatches
                |> List.choose justAdditional
                |> List.map (propString actual)

            let missings: string list =
                mismatches
                |> List.choose justMissing
                |> List.map (propString expected)

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
            match actual.ValueKind with
            | JsonValueKind.String ->
                let expectedStr = expected.GetString()
                let actualStr = actual.GetString()

                let maxStrLen =
                    max (String.length expectedStr) (String.length actualStr)

                let comparisonStr =
                    if maxStrLen > 30
                    then sprintf "Expected\n    %s\nbut was\n    %s" expectedStr actualStr
                    else sprintf "Expected %s but was %s." expectedStr actualStr

                sprintf "String value mismatch at %s.\n%s" path comparisonStr
            | JsonValueKind.Number ->
                sprintf "Number value mismatch at %s.\nExpected %s but was %s." path (expected.GetRawText())
                    (actual.GetRawText())
            | _ -> sprintf "Some other value mismatch at %s." path
        | Kind { Path = path; Left = actual; Right = expected } ->
            match (actual.ValueKind, expected.ValueKind) with
            | (JsonValueKind.True, JsonValueKind.False)
            | (JsonValueKind.False, JsonValueKind.True) ->
                sprintf "Boolean value mismatch at %s.\nExpected %b but was %b." path (expected.GetBoolean())
                    (actual.GetBoolean())
            | (_, _) ->
                let expectedMessage = toValueDescription expected
                let actualMessage = toValueDescription actual
                sprintf "Kind mismatch at %s.\nExpected %s but was %s." path expectedMessage actualMessage
        | ItemCount { Path = path; Left = actual; Right = expected } ->
            let expectedLength = expected.GetArrayLength()
            let actualLength = actual.GetArrayLength()

            let itemsStr =
                if expectedLength = 1 then "item" else "items"

            sprintf "Array length mismatch at %s.\nExpected %d %s but was %d." path expectedLength itemsStr actualLength
