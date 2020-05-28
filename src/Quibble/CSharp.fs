namespace Quibble.CSharp

module JsonStrings =

    open Quibble
    open System.Collections.Generic
    
    let Diff (jsonString1: string, jsonString2: string): IReadOnlyList<Diff> =
        let diffs = Quibble.JsonStrings.diff jsonString1 jsonString2
        diffs :> IReadOnlyList<Diff>
        