namespace Quibble 

module Lcs =

    type ItemIndexMismatch = 
    | LeftOnlyItemIndex of int // Index in left array.
    | RightOnlyItemIndex of int // Index in right array.
    
    type Position = int * int
    
    type LcsResult = {
        Length : int 
        EndIndex1 : int 
        EndIndex2 : int
    }

    type Segment = {
        Length : int 
        StartIndex1 : int 
        StartIndex2 : int
    }

    let createMatrix list1 list2 =
        let len1 = List.length list1 
        let len2 = List.length list2 
        let matrix = [| for _ in 0 .. len1 -> [| for _ in 0 .. len2 -> 0 |] |]
        for j in 1 .. len2 do 
            for i in 1 .. len1 do
                let e1 = list1.[i-1]
                let e2 = list2.[j-1]
                if e1 = e2 then matrix.[i].[j] <- matrix.[i-1].[j-1] + 1
        matrix
                
    let findMax (m : int[][]) (istart, iend) (jstart, jend) : LcsResult option = 
        let mutable longest : int = 0
        let mutable position : Position option = None
        for j in jstart .. jend do 
            for i in istart .. iend do
                let length = m.[i].[j]
                if length > longest then 
                    position <- Some (i, j) 
                    longest <- length
        let length = longest
        let result = position |> Option.map (fun (i, j) -> { Length = length; EndIndex1 = i; EndIndex2 = j })
        result

    let findBlocks (m : int[][]) (len1 : int) (len2 : int) : LcsResult list = 
        let ismallest = 1 
        let jsmallest = 1
        let imax = len1 
        let jmax = len2
        let rec findAux (istart, iend) (jstart, jend) : LcsResult list = 
            let maxResult = findMax m (istart, iend) (jstart, jend) 
            match maxResult with 
            | None -> []
            | Some lcsResult -> 
                let iendLeft = lcsResult.EndIndex1 - lcsResult.Length
                let istartRight = lcsResult.EndIndex1 + 1
                let jendLeft = lcsResult.EndIndex2 - lcsResult.Length
                let jstartRight = lcsResult.EndIndex2 + 1
                let leftResult : LcsResult list = 
                    if iendLeft >= ismallest && jendLeft >= jsmallest then 
                        findAux (istart, iendLeft) (jstart, jendLeft) 
                    else
                        []
                let rightResult : LcsResult list = 
                    if istartRight <= imax && jstartRight <= jmax then 
                        findAux (istartRight, iend) (jstartRight, jend) 
                    else 
                        []
                leftResult @ [ lcsResult ] @ rightResult
        findAux (ismallest, imax) (jsmallest, jmax)

    let findCommonSegments list1 list2 : Segment list = 
        let matrix = createMatrix list1 list2
        let blocks = findBlocks matrix (List.length list1) (List.length list2)
        let toSegment lcsResult = 
            match lcsResult with 
            | { Length = length; EndIndex1 = endIndex1; EndIndex2 = endIndex2 } -> 
                // Matrix index is offset by one due to zero-padding, compensate by subtracting by one.
                { StartIndex1 = endIndex1 - length; StartIndex2 = endIndex2 - length; Length = length }
        let segments = blocks |> List.map toSegment
        segments

    let toArrayItemIndexMismatches list1 list2 (commonSegments : Segment list) : ItemIndexMismatch list = 
        let dummyStartSegment = 
            { StartIndex1 = 0
              StartIndex2 = 0
              Length = 0 }
        let dummyEndSegment = 
            { StartIndex1 = List.length list1
              StartIndex2 = List.length list2 
              Length = 0 }
        let paddedCommonSegments = [ dummyStartSegment ] @ commonSegments @ [ dummyEndSegment ]
        let pairwise = Seq.pairwise paddedCommonSegments |> Seq.toList
        let toMismatches (segment1 : Segment, segment2 : Segment) : ItemIndexMismatch list = 
            let startIndex1 = segment1.StartIndex1 + segment1.Length
            let startIndex2 = segment1.StartIndex2 + segment1.Length
            let len1 = segment2.StartIndex1 - startIndex1 
            let len2 = segment2.StartIndex2 - startIndex2 
            let leftOnlies = [ for i in 0 .. len1 - 1 -> LeftOnlyItemIndex (startIndex1 + i) ]
            let rightOnlies = [ for i in 0 .. len2 - 1 -> RightOnlyItemIndex (startIndex2 + i) ]
            let lst = leftOnlies @ rightOnlies
            lst
        pairwise |> List.collect toMismatches
