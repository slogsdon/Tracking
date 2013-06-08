namespace Tracking

open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open System
open System.IO
open System.Runtime.Serialization
open System.Text
open System.Xml
open System.Xml.Serialization
open Tracking

module Serialization = 
    let FromJson<'a> obj =
        let converters : JsonConverter[] = [| UnionConverter() |]
        let settings = JsonSerializerSettings(Converters = converters)
        JsonConvert.DeserializeObject<'a> (obj, settings)
        
    let FromXml<'a> obj =
        let serializer = new DataContractSerializer(typedefof<'a>)
        use stream = new MemoryStream ()
        serializer.ReadObject stream :?> 'a
        
    let ToJson obj =
        let converters : JsonConverter[] = [| UnionConverter() |]
        let settings = JsonSerializerSettings(Converters = converters)
        JsonConvert.SerializeObject (obj, settings)
        
    let ToString = System.Text.Encoding.ASCII.GetString
    
    let ToXml<'a> obj =
        let serializer = new DataContractSerializer(typedefof<'a>)
        use stream = new MemoryStream ()
        serializer.WriteObject(stream, obj)
        ToString <| stream.ToArray ()