namespace Quibble

open System.Runtime.CompilerServices
open System.Text.Json

[<Extension>]
type JsonExtensions =
    [<Extension>]
    static member inline Diff(je1 : JsonElement, je2 : JsonElement) = JsonDiff.OfElements(je1, je2)
    
    [<Extension>]
    static member inline Diff(jd1 : JsonDocument, jd2 : JsonDocument) = JsonDiff.OfDocuments(jd1, jd2)

