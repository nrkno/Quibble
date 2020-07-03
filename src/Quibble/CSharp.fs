namespace Quibble.CSharp

open Quibble
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
    
type JsonUndefined private () =
    inherit JsonValue()

    static let instance = JsonUndefined() 
    static member Instance = instance
           
    override this.IsUndefined = true
           
    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? JsonUndefined -> true
        | _ -> false
        
    override this.ToString() = "undefined"

type JsonNull private () =
    inherit JsonValue()

    static let instance = JsonNull() 
    static member Instance = instance
           
    override this.IsNull = true

    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? JsonNull -> true
        | _ -> false
        
    override this.ToString() = "null"

type JsonTrue private () =
    inherit JsonValue()

    static let instance = JsonTrue() 
    static member Instance = instance
    
    override this.IsTrue = true
           
    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? JsonTrue -> true
        | _ -> false

    override this.ToString() = "true"

type JsonFalse private () =
    inherit JsonValue()

    static let instance = JsonFalse() 
    static member Instance = instance

    override this.IsFalse = true
           
    override this.GetHashCode() =
        hash <| this.GetType()

    override this.Equals(that) =
        match that with
        | :? JsonFalse -> true
        | _ -> false

    override this.ToString() = "false"

type JsonNumber (numericValue : double, textRepresentation : string)  =
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
        | :? JsonNumber as number ->
            number.NumericValue = this.NumericValue
        | _ -> false

    override this.ToString() = sprintf "%g (%s)" numericValue textRepresentation

type JsonString (text : string)  =
    inherit JsonValue()

    member this.Text = text

    override this.IsString = true
            
    override this.GetHashCode() =
        hash (this.GetType(), this.Text)

    override this.Equals(that) =
        match that with
        | :? JsonString as str ->
            str.Text = this.Text
        | _ -> false

    override this.ToString() = text

type JsonArray (items : IReadOnlyList<JsonValue>)  =
    inherit JsonValue()

    member this.Items = items

    override this.IsArray = true
            
    override this.GetHashCode() =
        let init = hash <| this.GetType()
        items |> Seq.fold (fun state item -> state * 31 + hash item) (487 + init)

    override this.Equals(that) =
        match that with
        | :? JsonArray as arr ->
            arr.Items.Count = this.Items.Count && Enumerable.SequenceEqual(arr.Items, this.Items)
        | _ -> false
        
    override this.ToString() =
        sprintf "Array [%d items]" (items.Count)
        
    interface IEnumerable<JsonValue> with
        member this.GetEnumerator() : IEnumerator<JsonValue> =
            items.GetEnumerator()
          
        member this.GetEnumerator() : System.Collections.IEnumerator =
            items.GetEnumerator() :> System.Collections.IEnumerator

type JsonObject (properties : IReadOnlyDictionary<string, JsonValue>)  =
    inherit JsonValue()
    
    let propSeq = properties |> Seq.map (fun kv -> kv.Key, kv.Value)
    
    member this.Item with get(propertyName : string) =
        match properties.TryGetValue(propertyName) with
        | (true, jv) -> jv
        | (false, _) -> JsonUndefined.Instance :> JsonValue

    override this.IsObject = true

    override this.GetHashCode() =
        let propList = Seq.toList propSeq
        hash (this.GetType(), propList)

    override this.Equals(that) =
        match that with
        | :? JsonObject as obj ->
            let asList (o : JsonObject) =
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
type ItemMismatch (itemIndex : int, itemValue : JsonValue) =
    member this.ItemIndex = itemIndex
    member this.ItemValue = itemValue
    
type LeftOnlyItem(itemIndex : int, itemValue : JsonValue) =
    inherit ItemMismatch(itemIndex, itemValue)
    
    override this.GetHashCode() =
        hash (typedefof<LeftOnlyItem>, itemIndex, itemValue)

    override this.Equals(thatObject) =
        match thatObject with
        | :? LeftOnlyItem as that ->
            this.ItemIndex = that.ItemIndex && this.ItemValue = that.ItemValue
        | _ -> false

type RightOnlyItem(itemIndex : int, itemValue : JsonValue) =
    inherit ItemMismatch(itemIndex, itemValue)

    override this.GetHashCode() =
        hash (typedefof<RightOnlyItem>, itemIndex, itemValue)

    override this.Equals(thatObject) =
        match thatObject with
        | :? RightOnlyItem as that ->
            this.ItemIndex = that.ItemIndex && this.ItemValue = that.ItemValue
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
    abstract member IsItems: bool
    default this.IsItems = false    
    abstract member IsProperties: bool
    default this.IsProperties = false
    
    override this.ToString() =
        sprintf "%s { Path = %s, Left = %s, Right = %s }" (this.GetType().Name) this.Path (this.Left.ToString()) (this.Right.ToString())


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

type Items(diffPoint : DiffPoint, mismatches : IReadOnlyList<ItemMismatch>) =
    inherit Diff(diffPoint)
    
    override this.IsItems = true

    member this.Mismatches = mismatches

    override this.GetHashCode() =
        hash (this.GetType(), diffPoint)

    override this.Equals(thatObject) =
        match thatObject with
        | :? Items as that ->
            let asList (ps : IReadOnlyList<ItemMismatch>) = ps :> IEnumerable<ItemMismatch> |> Seq.toList
            this.Path = that.Path && this.Left = that.Left && this.Right = that.Right && asList this.Mismatches = asList that.Mismatches
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
        | Quibble.JsonUndefined -> JsonUndefined.Instance :> JsonValue
        | Quibble.JsonNull -> JsonNull.Instance :> JsonValue
        | Quibble.JsonTrue -> JsonTrue.Instance :> JsonValue
        | Quibble.JsonFalse -> JsonFalse.Instance :> JsonValue
        | Quibble.JsonNumber (numericValue, textRepresentation) -> new JsonNumber(numericValue, textRepresentation) :> JsonValue
        | Quibble.JsonString text -> new JsonString(text) :> JsonValue
        | Quibble.JsonArray items ->
            let itemSeq = items |> List.map toCSharpJsonValue |> List.toSeq
            let array = new JsonArray(Enumerable.ToList(itemSeq))
            array :> JsonValue
        | Quibble.JsonValue.JsonObject props ->
            let dictionary = props |> List.map (fun (n, v) -> (n, toCSharpJsonValue v)) |> dict
            let readOnlyDictionary = new ReadOnlyDictionary<string, JsonValue>(dictionary)
            let object = new JsonObject(readOnlyDictionary)
            object :> JsonValue

    let private toCSharpDiffPoint (diffPoint : Quibble.DiffPoint) : DiffPoint =
        match diffPoint with
        | { Path = path; Left = left; Right = right } ->
            new DiffPoint(path, toCSharpJsonValue left, toCSharpJsonValue right)
            
    let private toCSharpPropertyMismatch (mismatch : Quibble.PropertyMismatch) : PropertyMismatch =
        match mismatch with
        | LeftOnlyProperty (n, v) -> new LeftOnlyProperty(n, toCSharpJsonValue v) :> PropertyMismatch
        | RightOnlyProperty (n, v) -> new RightOnlyProperty(n, toCSharpJsonValue v) :> PropertyMismatch

    let private toCSharpItemMismatch (mismatch : Quibble.ItemMismatch) : ItemMismatch =
        match mismatch with
        | LeftOnlyItem (n, v) -> new LeftOnlyItem(n, toCSharpJsonValue v) :> ItemMismatch
        | RightOnlyItem (n, v) -> new RightOnlyItem(n, toCSharpJsonValue v) :> ItemMismatch

    let private toCSharpDiff (diff : Quibble.Diff) : Diff =
        match diff with
        | Quibble.Diff.Type pt  -> Type(toCSharpDiffPoint pt) :> Diff
        | Quibble.Diff.Value pt -> Value(toCSharpDiffPoint pt) :> Diff
        | Quibble.Diff.Items (pt, mismatches) ->
            let mismatchList = mismatches |> List.map toCSharpItemMismatch :> IReadOnlyList<ItemMismatch>
            Items(toCSharpDiffPoint pt, mismatchList) :> Diff
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
