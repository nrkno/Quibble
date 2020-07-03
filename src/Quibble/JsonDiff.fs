namespace Quibble

type PathElement =
    | PropertyPathElement of string
    | IndexPathElement of int

type PropertyMismatch =
    | LeftOnlyProperty of string * JsonValue
    | RightOnlyProperty of string * JsonValue

type DiffPoint =
    { Path: string
      Left: JsonValue
      Right: JsonValue }

type ItemMismatch =
    | LeftOnlyItem of int * JsonValue 
    | RightOnlyItem of int * JsonValue 

type Diff =
    | Type of DiffPoint
    | Value of DiffPoint
    | Properties of (DiffPoint * PropertyMismatch list)
    | Items of (DiffPoint * ItemMismatch list)

module JsonDiff =

    open Lcs
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
        
        
    let rec private findDiff (path: PathElement list) (value1: JsonValue) (value2: JsonValue): Diff list =
        match (value1, value2) with
        | (JsonUndefined, JsonUndefined) -> []
        | (JsonNull, JsonNull) -> []
        | (JsonTrue, JsonTrue) -> []
        | (JsonFalse, JsonFalse) -> []
        | (JsonNumber (n1, t1), JsonNumber (n2, t2)) ->
            if n1 = n2 then []
            else
                [ Value
                    { Path = toJsonPath path
                      Left = value1
                      Right = value2 } ]
        | (JsonString s1, JsonString s2) ->
            if s1 = s2 then []
            else
                [ Value
                    { Path = toJsonPath path
                      Left = value1
                      Right = value2 } ]
        | (JsonArray items1, JsonArray items2) ->
            if items1 = items2 then
                []
            else
                let commonSegments = findCommonSegments items1 items2
                let skewed = commonSegments |> List.exists (fun segment -> segment.StartIndex1 <> segment.StartIndex2)
                if skewed || List.length items1 <> List.length items2 then
                    // Common segments are skewed or arrays are not the same length:
                    // Treat mismatches at array level. 
                    let indexMismatches = toArrayItemIndexMismatches items1 items2 commonSegments
                    let toItemMismatch indexMismatch =
                        match indexMismatch with
                        | LeftOnlyItemIndex leftIndex -> LeftOnlyItem (leftIndex, List.item leftIndex items1)
                        | RightOnlyItemIndex rightIndex -> RightOnlyItem (rightIndex, List.item rightIndex items2)
                    let mismatches = indexMismatches |> List.map toItemMismatch
                    let diffPoint = { Path = toJsonPath path; Left = value1; Right = value2 }
                    [ Items (diffPoint, mismatches) ]
                else
                    // Same length and no offsets: 
                    // Treat mismatches at individual item level.
                    let itemDiff i e1 e2 = findDiff (path @ [ IndexPathElement i ]) e1 e2
                    let childDiffs =
                        List.mapi2 itemDiff items1 items2
                        |> List.collect id
                    childDiffs
                
        | (JsonObject leftProps, JsonObject rightProps) ->
            (* order doesn't matter. *)
            let keys (props : (string * JsonValue) list): string list =
                props |> List.map (fun (n, _) -> n) 

            let leftKeys = keys leftProps
            let rightKeys = keys rightProps
            
            let rightOnlyKeys = rightKeys |> List.except leftKeys
            let rightOnlyProperties =
                rightProps
                |> List.filter (fun (n, _) -> List.contains n rightOnlyKeys)
                |> List.map RightOnlyProperty
                
            let leftOnlyKeys = leftKeys |> List.except rightKeys
            let leftOnlyProperties =
                leftProps
                |> List.filter (fun (n, _) -> List.contains n leftOnlyKeys)
                |> List.map LeftOnlyProperty

            let mismatches = leftOnlyProperties @ rightOnlyProperties

            let objectDiff =
                match mismatches with
                | [] -> []
                | ms ->
                    [ Properties
                        ({ Path = toJsonPath path
                           Left = value1
                           Right = value2 },
                         ms) ]

            let sharedKeys: string list = rightKeys |> List.except rightOnlyKeys

            let selectValue (key: string) (props : (string * JsonValue) list) =
                List.pick (fun (k, v) -> if k = key then Some v else None) props
            
            let propDiff (key: string) =
               let child1 = leftProps |> selectValue key
               let child2 = rightProps |> selectValue key
               findDiff (path @ [ PropertyPathElement key ]) child1 child2

            let childDiffs = sharedKeys |> List.collect propDiff
            objectDiff @ childDiffs
        | _ -> 
            [ Type
                { Path = toJsonPath path
                  Left = value1
                  Right = value2 } ]

    let OfValues (v1: JsonValue) (v2: JsonValue) : Diff list =
        findDiff [] v1 v2
    