namespace Quibble

module JsonVerify =

    open System.Collections.Generic

    let Diff (s1: string, s2: string): IEnumerable<string> =
        JsonStrings.Diff(s1, s2) |> Seq.map DiffMessage.toDiffMessage
