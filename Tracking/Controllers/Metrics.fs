namespace Tracking.Controllers

open System
open System.Configuration
open System.Web
open System.Web.Mvc
open Tracking.Models
open Newtonsoft.Json
open Tracking

type MetricsController() =
    inherit Controller()
    let connectionStr = (ConfigurationManager.ConnectionStrings.Item "DefaultConnection").ConnectionString
    let mu = new Metrics(connectionStr)

    // GET /metrics
    [<HttpGet>]
    member x.Index () = x.HandleGet (mu.GetAll ())

    // POST /metrics
    [<HttpPost>]
    member x.Index (postData : string) = x.HandlePost postData

    // GET /metrics/get/loadTime
    [<HttpGet>]
    member x.Get (name : string) = x.HandleGet (mu.Get name)

    // GET /metrics/getByCategory/system
    [<HttpGet>]
    member x.GetByCategory (name : string) = x.HandleGet (mu.GetByCategory name)
        
    member x.HandleGet obj =
        match x.Request.ContentType with
        | "application/xml" -> 
            Serialization.ToXml obj
        | "text/xml" -> 
            Serialization.ToXml obj
        | "application/json" -> 
            Serialization.ToJson obj
        | _ -> 
            Serialization.ToJson obj
        
    member x.HandlePost obj =
        let obj =
            match x.Request.ContentType with
            | "application/xml" -> 
                Serialization.FromXml obj
            | "text/xml" -> 
                Serialization.FromXml obj
            | "application/json" -> 
                Serialization.FromJson obj
            | _ -> 
                Serialization.FromJson obj
        
        mu.AddTo 
            obj.Name 
            obj.Value 
            (if obj.DateTime <> DateTime.MinValue then 
                obj.DateTime 
             else 
                DateTime.Now)
