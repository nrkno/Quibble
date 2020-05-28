namespace Quibble.CSharp

module JsonStrings =

    open Quibble
    open System.Collections.Generic
    
    let Diff (leftJsonString: string, rightJsonString: string): IReadOnlyList<Diff> =
        let diffs = Quibble.JsonStrings.diff leftJsonString rightJsonString
        diffs :> IReadOnlyList<Diff>
        
    let TextDiff (leftJsonString: string, rightJsonString: string): IReadOnlyList<string> =
        let diffs = Quibble.JsonStrings.textDiff leftJsonString rightJsonString
        diffs :> IReadOnlyList<string>
