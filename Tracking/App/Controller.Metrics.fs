namespace Tracking.Controllers

open System
open System.Configuration
open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open Tracking.Models

type MetricsController() =
    inherit ApiController()
    let connectionStr = ConfigurationManager.ConnectionStrings.Item "DefaultConnection"
    let mu = new Metrics(connectionStr.ConnectionString)

    // GET /metrics
    [<HttpGet>]
    member x.Index () = 
        mu.GetAll ()

    // POST /metrics
    [<HttpPost>]
    member x.Index (m : Metric) =
        mu.AddTo 
            m.Name 
            m.Value 
            (if m.DateTime <> DateTime.MinValue then 
                m.DateTime 
             else 
                DateTime.Now)

    // GET /metrics/get/loadTime
    [<HttpGet>]
    member x.Get (name : string) = 
        mu.Get name

    // GET /metrics/getByCategory/system
    [<HttpGet>]
    member x.GetByCategory (name : string) =
        mu.GetByCategory name
