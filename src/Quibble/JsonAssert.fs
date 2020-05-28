namespace Quibble

module JsonAssert =

    let diff (actualJsonString: string) (expectedJsonString: string): string list =
        JsonStrings.diff actualJsonString expectedJsonString
        |> List.map AssertMessage.toDiffMessage
