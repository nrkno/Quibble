namespace Quibble.CSharp

open Quibble
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Linq
    
[<AbstractClass>]
type JsonValue() =
    abstract member IsUndefined : bool
    default this.IsUndefined = false
    abstract member IsNull : bool
    default this.IsNull = false
    abstract member IsTrue : bool
    default this.IsTrue = false
    abstract member IsFalse : bool
    default this.IsFalse = false 
    abstract member IsNumber : bool
    default this.IsNumber = false 
    abstract member IsString : bool
    default this.IsString = false 
    abstract member IsArray : bool
    default this.IsArray = false
    abstract member IsObject : bool
    default this.IsObject = false
    
type Undefined private () =
    inherit JsonValue()

    static let instance = Undefined() 
    static member Instance = instance
           
    override this.IsUndefined = true
           
    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? Undefined -> true
        | _ -> false
        
    override this.ToString() = "undefined"

type Null private () =
    inherit JsonValue()

    static let instance = Null() 
    static member Instance = instance
           
    override this.IsNull = true

    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? Null -> true
        | _ -> false
        
    override this.ToString() = "null"

type True private () =
    inherit JsonValue()

    static let instance = True() 
    static member Instance = instance
    
    override this.IsTrue = true
           
    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? True -> true
        | _ -> false

    override this.ToString() = "true"

type False private () =
    inherit JsonValue()

    static let instance = False() 
    static member Instance = instance

    override this.IsFalse = true
           
    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? False -> true
        | _ -> false

    override this.ToString() = "false"

type Number (numericValue : double, textRepresentation : string)  =
    inherit JsonValue()

    member this.NumericValue = numericValue
    
    member this.TextRepresentation = textRepresentation

    override this.IsNumber = true
            
    // Only numeric value counts for equality.
    override this.GetHashCode() =
        hash (this.GetType(), this.NumericValue)

    // Only numeric value counts for equality.
    override this.Equals(that) =
        match that with
        | :? Number as number ->
            number.NumericValue = this.NumericValue
        | _ -> false

    override this.ToString() = sprintf "%g (%s)" numericValue textRepresentation

type String (text : string)  =
    inherit JsonValue()

    member this.Text = text

    override this.IsString = true
            
    override this.GetHashCode() =
        hash (this.GetType(), this.Text)

    override this.Equals(that) =
        match that with
        | :? String as str ->
            str.Text = this.Text
        | _ -> false

    override this.ToString() = text

type Array (items : IReadOnlyList<JsonValue>)  =
    inherit JsonValue()

    member this.Items = items

    override this.IsArray = true
            
    override this.GetHashCode() =
        let init = hash <| this.GetType()
        items |> Seq.fold (fun state item -> state * 31 + hash item) (487 + init)

    override this.Equals(that) =
        match that with
        | :? Array as arr ->
            arr.Items.Count = this.Items.Count && Enumerable.SequenceEqual(arr.Items, this.Items)
        | _ -> false
        
    override this.ToString() =
        sprintf "Array [%d items]" (items.Count)
        
    interface IEnumerable<JsonValue> with
        member this.GetEnumerator() : IEnumerator<JsonValue> =
            items.GetEnumerator()
          
        member this.GetEnumerator() : System.Collections.IEnumerator =
            items.GetEnumerator() :> System.Collections.IEnumerator

type Object (properties : IReadOnlyDictionary<string, JsonValue>)  =
    inherit JsonValue()
    
    let propSeq = properties |> Seq.map (fun kv -> kv.Key, kv.Value)
    
    member this.Item with get(propertyName : string) =
        match properties.TryGetValue(propertyName) with
        | (true, jv) -> jv
        | (false, _) -> Undefined.Instance :> JsonValue

    override this.IsObject = true

    override this.GetHashCode() =
        let propList = Seq.toList propSeq
        hash (this.GetType(), propList)

    override this.Equals(that) =
        match that with
        | :? Object as obj ->
            let asList (o : Object) =
                o :> IEnumerable<string * JsonValue> |> Seq.toList
            asList this = asList obj
        | _ -> false

    override this.ToString() =
        sprintf "Object {%d properties}" (Seq.length propSeq)
            
    interface IEnumerable<string * JsonValue> with
      member this.GetEnumerator() : IEnumerator<string * JsonValue> =
          propSeq.GetEnumerator()
          
      member this.GetEnumerator() : System.Collections.IEnumerator =
          propSeq.GetEnumerator() :> System.Collections.IEnumerator

type DiffPoint(path : string, left: JsonValue, right : JsonValue) =
    
    member this.Path = path
    member this.Left = left
    member this.Right = right
    
    override this.GetHashCode() =
        hash (path, left, right)

    override this.Equals(thatObject) =
        match thatObject with
        | :? DiffPoint as that ->
            this.Path = that.Path && this.Left = that.Left && this.Right = that.Right
        | _ -> false

[<AbstractClass>]        
type PropertyMismatch (propertyName : string, propertyValue : JsonValue) =
    member this.PropertyName = propertyName
    member this.PropertyValue = propertyValue
    
type LeftOnlyProperty(propertyName : string, propertyValue : JsonValue) =
    inherit PropertyMismatch(propertyName, propertyValue)
    
    override this.GetHashCode() =
        hash (typedefof<LeftOnlyProperty>, propertyName, propertyValue)

    override this.Equals(thatObject) =
        match thatObject with
        | :? LeftOnlyProperty as that ->
            this.PropertyName = that.PropertyName && this.PropertyValue = that.PropertyValue
        | _ -> false

type RightOnlyProperty(propertyName : string, propertyValue : JsonValue) =
    inherit PropertyMismatch(propertyName, propertyValue)

    override this.GetHashCode() =
        hash (typedefof<RightOnlyProperty>, propertyName, propertyValue)

    override this.Equals(thatObject) =
        match thatObject with
        | :? RightOnlyProperty as that ->
            this.PropertyName = that.PropertyName && this.PropertyValue = that.PropertyValue
        | _ -> false
        
[<AbstractClass>]
type Diff(diffPoint : DiffPoint) =
    member this.Path = diffPoint.Path
    member this.Left = diffPoint.Left    
    member this.Right = diffPoint.Right
    abstract member IsType: bool
    default this.IsType = false
    abstract member IsValue: bool
    default this.IsValue = false
    abstract member IsItemCount: bool
    default this.IsItemCount = false    
    abstract member IsProperties: bool
    default this.IsProperties = false

type Type(diffPoint : DiffPoint) =
    inherit Diff(diffPoint)
    
    override this.IsType = true
    
    override this.GetHashCode() =
        hash (this.GetType(), diffPoint)
        
    override this.Equals(thatObject) =
        match thatObject with
        | :? Type as that ->
            this.Path = that.Path && this.Left = that.Left && this.Right = that.Right
        | _ -> false

type Value(diffPoint : DiffPoint) =
    inherit Diff(diffPoint)

    override this.IsValue = true
    
    override this.GetHashCode() =
        hash (this.GetType(), diffPoint)

    override this.Equals(thatObject) =
        match thatObject with
        | :? Value as that ->
            this.Path = that.Path && this.Left = that.Left && this.Right = that.Right
        | _ -> false
        
type ItemCount(diffPoint : DiffPoint) =
    inherit Diff(diffPoint)
    
    override this.IsItemCount = true


    override this.GetHashCode() =
        hash (this.GetType(), diffPoint)

    override this.Equals(thatObject) =
        let foo = 17
        match thatObject with
        | :? ItemCount as that ->
            this.Path = that.Path && this.Left = that.Left && this.Right = that.Right
        | _ ->
            false

type Properties(diffPoint : DiffPoint, mismatches : IReadOnlyList<PropertyMismatch>) =
    inherit Diff(diffPoint)
    
    override this.IsProperties = true

    member this.Mismatches = mismatches

    override this.GetHashCode() =
        let list = mismatches |> Seq.toList
        hash (this.GetType(), diffPoint, list)

    override this.Equals(thatObject) =
        match thatObject with
        | :? Properties as that ->
            let asList (ps : IReadOnlyList<PropertyMismatch>) = ps :> IEnumerable<PropertyMismatch> |> Seq.toList
            this.Path = that.Path && this.Left = that.Left && this.Right = that.Right && asList this.Mismatches = asList that.Mismatches
        | _ -> false
                    
module JsonStrings =

    let rec private toCSharpJsonValue (jsonValue : Quibble.JsonValue) : JsonValue =
        match jsonValue with
        | Quibble.JsonValue.Undefined -> Undefined.Instance :> JsonValue
        | Quibble.JsonValue.Null -> Null.Instance :> JsonValue
        | Quibble.JsonValue.True -> True.Instance :> JsonValue
        | Quibble.JsonValue.False -> False.Instance :> JsonValue
        | Quibble.JsonValue.Number (numericValue, textRepresentation) -> new Number(numericValue, textRepresentation) :> JsonValue
        | Quibble.JsonValue.String text -> new String(text) :> JsonValue
        | Quibble.JsonValue.Array items ->
            let itemSeq = items |> List.map toCSharpJsonValue |> List.toSeq
            let array = new Array(Enumerable.ToList(itemSeq))
            array :> JsonValue
        | Quibble.JsonValue.Object props ->
            let dictionary = props |> List.map (fun (n, v) -> (n, toCSharpJsonValue v)) |> dict
            let readOnlyDictionary = new ReadOnlyDictionary<string, JsonValue>(dictionary)
            let object = new Object(readOnlyDictionary)
            object :> JsonValue

    let private toCSharpDiffPoint (diffPoint : Quibble.DiffPoint) : DiffPoint =
        match diffPoint with
        | { Path = path; Left = left; Right = right } ->
            new DiffPoint(path, toCSharpJsonValue left, toCSharpJsonValue right)
            
    let private toCSharpPropertyMismatch (mismatch : Quibble.PropertyMismatch) : PropertyMismatch =
        match mismatch with
        | LeftOnlyProperty (n, v) -> new LeftOnlyProperty(n, toCSharpJsonValue v) :> PropertyMismatch
        | RightOnlyProperty (n, v) -> new RightOnlyProperty(n, toCSharpJsonValue v) :> PropertyMismatch
                
    let private toCSharpDiff (diff : Quibble.Diff) : Diff =
        match diff with
        | Quibble.Diff.Type pt  -> Type(toCSharpDiffPoint pt) :> Diff
        | Quibble.Diff.Value pt -> Value(toCSharpDiffPoint pt) :> Diff
        | Quibble.Diff.ItemCount pt -> ItemCount(toCSharpDiffPoint pt) :> Diff
        | Quibble.Diff.Properties (pt, mismatches) ->
            let mismatchList = mismatches |> List.map toCSharpPropertyMismatch :> IReadOnlyList<PropertyMismatch> 
            Properties(toCSharpDiffPoint pt, mismatchList) :> Diff
    
    let Diff (leftJsonString: string, rightJsonString: string): IReadOnlyList<Diff> =
        let diffs = Quibble.JsonStrings.diff leftJsonString rightJsonString
        let csharpDiffs = diffs |> List.map toCSharpDiff :> IReadOnlyList<Diff>
        csharpDiffs
        
    let TextDiff (leftJsonString: string, rightJsonString: string): IReadOnlyList<string> =
        let diffs = Quibble.JsonStrings.textDiff leftJsonString rightJsonString
        diffs :> IReadOnlyList<string>
