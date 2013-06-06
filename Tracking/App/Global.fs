namespace Tracking

open System
open System.Web
open System.Web.Mvc
open System.Web.Routing
open System.Web.Http

type Route = { controller : string; action : string; name : UrlParameter }
type ApiRoute = { id : RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

    static member RegisterGlobalFilters (filters:GlobalFilterCollection) =
        filters.Add(new HandleErrorAttribute())

    static member RegisterRoutes(routes:RouteCollection) =
        routes.IgnoreRoute( "{resource}.axd/{*pathInfo}" )
        routes.MapHttpRoute( "DefaultApi", "{controller}/{action}/{name}", 
            { controller = "Metrics"; action = "index"; name = UrlParameter.Optional } ) |> ignore
//        routes.MapRoute("Default", "{controller}/{action}/{id}", 
//            { controller = "Home"; action = "Index"; id = UrlParameter.Optional } ) |> ignore

    member this.Start() =
        AreaRegistration.RegisterAllAreas()
        Global.RegisterRoutes RouteTable.Routes
        Global.RegisterGlobalFilters GlobalFilters.Filters
