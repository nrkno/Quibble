open Quibble

let runDiff example = 
    printfn "Example: %A" example
    JsonStrings.Diff example |> Seq.iter (printfn "%s")
    printfn ""

[<EntryPoint>]
let main _ =

    let examples = [
        ("1", "2")
        ("[ 1 ]", "[ 2, 1 ]")
        ("""{ "item": "widget", "price": 12.20 }""", """{ "item": "widget" }""")
        ("""{ "books": [ { "title": "Data and Reality", "author": "William Kent" }, { "title": "Thinking Forth", "author": "Chuck Moore" } ] }""", """{ "books": [ { "title": "Data and Reality", "author": "William Kent" }, { "title": "Thinking Forth", "author": "Leo Brodie" } ] }""")
    ]

    examples |> List.iter runDiff

    0 // return an integer exit code
