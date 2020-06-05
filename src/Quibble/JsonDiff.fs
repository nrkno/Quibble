﻿namespace Quibble

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

type Diff =
    | Type of DiffPoint
    | Value of DiffPoint
    | Properties of (DiffPoint * PropertyMismatch list)
    | ItemCount of DiffPoint

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

    let rec private findDiff (path: PathElement list) (value1: JsonValue) (value2: JsonValue): Diff list =
        match (value1, value2) with
        | (Undefined, Undefined) -> []
        | (Null, Null) -> []
        | (True, True) -> []
        | (False, False) -> []
        | (Number (n1, t1), Number (n2, t2)) ->
            if n1 = n2 then []
            else
                [ Value
                    { Path = toJsonPath path
                      Left = value1
                      Right = value2 } ]
        | (String s1, String s2) ->
            if s1 = s2 then []
            else
                [ Value
                    { Path = toJsonPath path
                      Left = value1
                      Right = value2 } ]
        | (Array items1, Array items2) ->
            (* order matters. *)
            if List.length items1 <> List.length items2 then
                [ ItemCount
                    { Path = toJsonPath path
                      Left = value1
                      Right = value2 } ]
            else
                let itemDiff i e1 e2 = findDiff (path @ [ IndexPathElement i ]) e1 e2
                let childDiffs =
                    List.mapi2 itemDiff items1 items2
                    |> List.collect id
                childDiffs
        | (Object leftProps, Object rightProps) ->
            (* order doesn't matter. *)
            let keys (props : (string * JsonValue) list): string list =
                props |> List.map (fun (n, v) -> n) 

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
    