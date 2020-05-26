namespace Quibble

open System.Text.Json

type PathElement =
    | PropertyPathElement of string
    | IndexPathElement of int

type PropertyMismatch =
    | MissingProperty of string
    | AdditionalProperty of string

type DiffPoint =
        {
            Path : string
            Left : JsonElement
            Right : JsonElement
        }

type Diff =
    | Kind of DiffPoint
    | Value of DiffPoint
    | Properties of (DiffPoint * PropertyMismatch list)
    | ItemCount of (DiffPoint)
    
        member x.Path =
            match x with
            | Kind pt -> pt.Path
            | Value pt -> pt.Path
            | Properties (pt, _) -> pt.Path
            | ItemCount pt -> pt.Path

module JsonDiff =

    open System.Collections.Generic
    open System.Text.RegularExpressions
    
    let private toJsonPath (path : PathElement list) : string =
        let (|Dot|Bracket|) s =
            if Regex.IsMatch(s, "^[a-zA-Z0-9]+$") then Dot
            else Bracket
    
        let elementToString =
            function
            | PropertyPathElement p ->
                match p with
                | Dot -> sprintf ".%s" p
                | Bracket -> sprintf "['%s']" p
            | IndexPathElement i -> sprintf "[%d]" i
    
        path
        |> List.fold (fun str elm -> sprintf "%s%s" str (elementToString elm)) ""
        |> sprintf "$%s"
    
    let rec private findDiff (path : PathElement list) (element1 : JsonElement)
            (element2 : JsonElement) : Diff list =
        if (element1.ValueKind <> element2.ValueKind) then
            [ Kind { Path = toJsonPath path; Left = element1; Right = element2 } ]
        else
            match element1.ValueKind with
            | JsonValueKind.Array ->
                (* order matters. *)
                let itemsOf (e : JsonElement) : JsonElement list =
                    e.EnumerateArray() |> Seq.toList
                let children1 = itemsOf element1
                let children2 = itemsOf element2
                if List.length children1 <> List.length children2 then
                    [ ItemCount { Path = toJsonPath path; Left = element1; Right = element2 } ]
                else
                    let itemDiff i e1 e2 =
                        findDiff (path @ [ IndexPathElement i ]) e1 e2
                    let childDiffs =
                        List.mapi2 itemDiff children1 children2 |> List.collect id
                    childDiffs
            | JsonValueKind.Object ->
                (* order doesn't matter. *)
                let keys (e : JsonElement) : string list =
                    e.EnumerateObject()
                    |> Seq.map (fun jp -> jp.Name)
                    |> Seq.toList
    
                let keys1 = keys element1
                let keys2 = keys element2
                let missingKeys = keys2 |> List.except keys1
                let missingProperties = missingKeys |> List.map MissingProperty
                let additionalKeys = keys1 |> List.except keys2
                let additionalProperties =
                    additionalKeys |> List.map AdditionalProperty
                let mismatches = missingProperties @ additionalProperties
    
                let objectDiff =
                    match mismatches with
                    | [] -> []
                    | ms ->
                        [ Properties ( { Path = toJsonPath path; Left = element1; Right = element2 }, ms) ]
    
                let sharedKeys : string list = keys2 |> List.except missingKeys
    
                let propDiff (key : string) =
                    let child1 = element1.GetProperty(key)
                    let child2 = element2.GetProperty(key)
                    findDiff (path @ [ PropertyPathElement key ]) child1 child2
    
                let childDiffs = sharedKeys |> List.collect propDiff
                objectDiff @ childDiffs
            | JsonValueKind.Number ->
                let representsSameInt32 (el1 : JsonElement) (el2 : JsonElement) : bool =
                    match (el1.TryGetInt32(), el2.TryGetInt32()) with
                    | ((true, n1), (true, n2)) -> n1 = n2
                    | _ -> false
                let representsSameDouble (el1 : JsonElement) (el2 : JsonElement) : bool =
                    match (el1.TryGetDouble(), el2.TryGetDouble()) with
                    | ((true, n1), (true, n2)) -> n1 = n2
                    | _ -> false
                let representsSameNumber (el1 : JsonElement) (el2 : JsonElement) : bool =
                    representsSameInt32 el1 el2 || representsSameDouble el1 el2
                if representsSameNumber element1 element2 then []
                else [ Value { Path = toJsonPath path; Left = element1; Right = element2 } ]
            | JsonValueKind.String ->
                let string1 = element1.GetString()
                let string2 = element2.GetString()
                if string1 = string2 then []
                else [ Value { Path = toJsonPath path; Left = element1; Right = element2 } ]
            | _ -> []
    
    let OfElements(e1 : JsonElement, e2 : JsonElement) : IEnumerable<Diff> = 
        findDiff [] e1 e2 |> List.toSeq
        
    let OfDocuments(d1 : JsonDocument, d2 : JsonDocument) : IEnumerable<Diff> = 
        OfElements(d1.RootElement, d2.RootElement)

        
