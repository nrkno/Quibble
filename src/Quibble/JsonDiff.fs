namespace Quibble

type PathElement =
    | PropertyPathElement of string
    | IndexPathElement of int

type PropertyMismatch =
    | MissingProperty of string * JsonValue
    | AdditionalProperty of string * JsonValue

type DiffPoint =
    { Path: string
      Left: JsonValue
      Right: JsonValue }

type Diff =
    | Kind of DiffPoint
    | Value of DiffPoint
    | Properties of (DiffPoint * PropertyMismatch list)
    | ItemCount of DiffPoint

    member x.Path =
        match x with
        | Kind pt -> pt.Path
        | Value pt -> pt.Path
        | Properties (pt, _) -> pt.Path
        | ItemCount pt -> pt.Path
        
    member x.Left =
        match x with
        | Kind pt -> pt.Left
        | Value pt -> pt.Left
        | Properties (pt, _) -> pt.Left
        | ItemCount pt -> pt.Left

    member x.Right =
        match x with
        | Kind pt -> pt.Right
        | Value pt -> pt.Right
        | Properties (pt, _) -> pt.Right
        | ItemCount pt -> pt.Right

module JsonDiff =

    open System.Text.RegularExpressions
    
    let private toJsonPath (path: PathElement list): string =
        let (|Dot|Bracket|) s =
            if Regex.IsMatch(s, "^[a-zA-Z0-9]+$") then Dot else Bracket

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

    let rec private findDiff (path: PathElement list) (element1: JsonValue) (element2: JsonValue): Diff list =
        match (element1, element2) with
        | (Undefined, Undefined) -> []
        | (Null, Null) -> []
        | (True, True) -> []
        | (False, False) -> []
        | (Number (n1, t1), Number (n2, t2)) ->
            if n1 = n2 then []
            else
                [ Value
                    { Path = toJsonPath path
                      Left = element1
                      Right = element2 } ]
        | (String s1, String s2) ->
            if s1 = s2 then []
            else
                [ Value
                    { Path = toJsonPath path
                      Left = element1
                      Right = element2 } ]
        | (Array items1, Array items2) ->
            (* order matters. *)
            if List.length items1 <> List.length items2 then
                [ ItemCount
                    { Path = toJsonPath path
                      Left = element1
                      Right = element2 } ]
            else
                let itemDiff i e1 e2 = findDiff (path @ [ IndexPathElement i ]) e1 e2
                let childDiffs =
                    List.mapi2 itemDiff items1 items2
                    |> List.collect id
                childDiffs
        | (Object props1, Object props2) ->
            (* order doesn't matter. *)
            let keys (props : (string * JsonValue) list): string list =
                props |> List.map (fun (n, v) -> n) 

            let keys1 = keys props1
            let keys2 = keys props2
            
            let missingKeys = keys2 |> List.except keys1
            let missingProperties =
                props2
                |> List.filter (fun (n, _) -> List.contains n missingKeys)
                |> List.map MissingProperty
                
            let additionalKeys = keys1 |> List.except keys2
            let additionalProperties =
                props1
                |> List.filter (fun (n, _) -> List.contains n additionalKeys)
                |> List.map AdditionalProperty

            let mismatches = missingProperties @ additionalProperties

            let objectDiff =
                match mismatches with
                | [] -> []
                | ms ->
                    [ Properties
                        ({ Path = toJsonPath path
                           Left = element1
                           Right = element2 },
                         ms) ]

            let sharedKeys: string list = keys2 |> List.except missingKeys

            let selectValue (key: string) (props : (string * JsonValue) list) =
                List.pick (fun (k, v) -> if k = key then Some v else None) props
            
            let propDiff (key: string) =
               let child1 = props1 |> selectValue key
               let child2 = props2 |> selectValue key
               findDiff (path @ [ PropertyPathElement key ]) child1 child2

            let childDiffs = sharedKeys |> List.collect propDiff
            objectDiff @ childDiffs
        | _ -> 
            [ Kind
                { Path = toJsonPath path
                  Left = element1
                  Right = element2 } ]

    let OfValues (v1: JsonValue) (v2: JsonValue) : Diff list =
        findDiff [] v1 v2
    