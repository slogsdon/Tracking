namespace Tracking.Models

open System
open System.Data
open System.Runtime.Serialization
open MySql.Data.MySqlClient
open Newtonsoft.Json
open Microsoft.FSharp.Reflection
open System.Reflection

[<DataContract>]
[<CustomComparison>]
[<CustomEquality>]
type Metric = 
    {
        [<field: DataMember(Name="id")>]
        Id: int 
        [<field: DataMember(Name="name")>]
        Name: string
        [<field: DataMember(Name="value")>]
        Value: double
        [<field: DataMember(Name="datetime")>]
        DateTime: DateTime
    }

    static member Empty =
        Metric.EmptyFromValue 0.0

    static member EmptyFromValue value =
        {
            Id = 0
            Name = ""
            Value = value
            DateTime = DateTime.MinValue
        }

    // Add two Metrics with like names
    static member (+) (m1 : Metric, m2 : Metric) =
        Metric.EmptyFromValue (m1.Value + m2.Value)

    // Subtract two Metrics with like names
    static member (-) (m1 : Metric, m2 : Metric) =
        Metric.EmptyFromValue (m1.Value - m2.Value)

    // Multiply two Metrics with like names
    static member (*) (m1 : Metric, m2 : Metric) =
        Metric.EmptyFromValue (m1.Value * m2.Value)

    // Divide two Metrics with like names
    static member (/) (m1: Metric, m2 : Metric) =
        Metric.EmptyFromValue (m1.Value / m2.Value)

    static member DivideByInt (m1 : Metric, i1 : int) =
        Metric.EmptyFromValue (m1.Value / (i1 |> double))

    static member op_Explicit (m : Metric) : double = 
        m.Value

    static member Zero =
        Metric.Empty
        
    interface IComparable with
        member this.CompareTo o =
            match o with
                | :? Metric as m -> compare this.Value m.Value
                | _ -> -1
     
    override this.Equals (o : obj) =
        match o with
            | :? Metric as m ->
                this.Value.Equals m.Value &&
                this.Name.Equals m.Name
            | _ -> false

    override this.GetHashCode () =
        this.Value |> int

// End Metric type
// Begin Result Types

[<DataContract>]
type StatisticsResult = 
    {
        [<field: DataMember(Name="data")>]
        Data: Metric seq
        [<field: DataMember(Name="mean")>]
        Mean: double 
        [<field: DataMember(Name="median")>]
        Median: double 
        [<field: DataMember(Name="mode")>]
        Mode: double 
        [<field: DataMember(Name="maximum")>]
        Maximum: double 
        [<field: DataMember(Name="minimum")>]
        Minimum: double 
        [<field: DataMember(Name="range")>]
        Range: double 
    }

[<DataContract>]
type PlainResult = 
    {
        [<field: DataMember(Name="data")>]
        Data: Metric seq
    }

[<DataContract>]
type ErrorResult = 
    {
        [<field: DataMember(Name="code")>]
        Code: Metric seq
        [<field: DataMember(Name="message")>]
        Message: string
    }

[<KnownType("KnownTypes")>]
type Result =
    | StatisticsResult of StatisticsResult
    | PlainResult of PlainResult
    | ErrorResult of ErrorResult

    static member KnownTypes () =
        typeof<Result>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic) 
            |> Array.filter FSharpType.IsUnion

// End Result types

type Metrics (connStr:string) =
    let conn = new MySqlConnection (connStr)
    do
        conn.Open()

    // Alias for Object.Get ()
    member this.GetAll () =
        this.Get ()

    member this.AddTo metric value datetime =
        use cmd = new MySqlCommand ()
        cmd.Connection  <- conn
        cmd.CommandType <- CommandType.Text
        cmd.CommandText <- "select id from metric_name where name = @name limit 1"
        cmd.Parameters.AddWithValue ("@name",  metric  ) |> ignore

        let id = cmd.ExecuteScalar().ToString() |> int

        cmd.CommandText <- "insert into metric (metric_name_id, value, dt) " +
            "values (@id, @value, @dt)"
        cmd.Parameters.AddWithValue ("@id",  id  ) |> ignore
        cmd.Parameters.AddWithValue ("@value", value   ) |> ignore
        cmd.Parameters.AddWithValue ("@dt",    datetime) |> ignore

        cmd.ExecuteNonQuery ()
        
    
    member this.AddFromJson json =
        let m = JsonConvert.DeserializeObject<Metric> json
        this.AddTo 
            m.Name 
            m.Value 
            (if m.DateTime <> DateTime.MinValue then 
                m.DateTime 
             else 
                DateTime.Now)

    member this.Get (?metric) =
        let met = defaultArg metric String.Empty

        use cmd = new MySqlCommand ()
        cmd.Connection  <- conn
        cmd.CommandType <- CommandType.Text
        cmd.CommandText <- "select m.id as MetricId, " +
            "mn.name as Name, m.value as Value, m.dt as Dt, mn.sort_desc " +
            "as SortDesc from metric m " +
            "inner join metric_name mn on m.metric_name_id = mn.id "
        if met <> String.Empty then
            cmd.CommandText <- cmd.CommandText + " where mn.name = @metricName"
            cmd.Parameters.AddWithValue ("@metricName", met) |> ignore
        cmd.CommandText <- cmd.CommandText + " order by " +
            "m.dt, m.value * (-1 * mn.sort_desc)"
        
        let s = seq {
            use reader = cmd.ExecuteReader()
            while reader.Read() do
                yield {
                    Id       = reader.GetInt32    (reader.GetOrdinal "MetricId")
                    Name     = reader.GetString   (reader.GetOrdinal "Name")
                    Value    = reader.GetDouble   (reader.GetOrdinal "Value")
                    DateTime = reader.GetDateTime (reader.GetOrdinal "Dt")
                }}

        this.SeqToResult ( s, 
                (s |> Seq.toArray).Length <> 0 && 
                met <> String.Empty)
    
    member this.GetByCategory (category) =
        use cmd = new MySqlCommand()
        cmd.Connection  <- conn
        cmd.CommandType <- CommandType.Text
        cmd.CommandText <- "select m.id as MetricId, " +
            "mn.name as Name, m.value as Value, m.dt as Dt from metric m " +
            "inner join metric_name mn on m.metric_name_id = mn.id " +
            "where instr(mn.name, concat(@categoryName, '.')) > 0 order by " +
            "m.dt, m.value * (-1 * mn.sort_desc)"
        cmd.Parameters.AddWithValue("@categoryName", category) |> ignore
        
        let s = seq {
            use reader = cmd.ExecuteReader()
            while reader.Read() do
                yield {
                    Id       = reader.GetInt32    (reader.GetOrdinal "MetricId")
                    Name     = reader.GetString   (reader.GetOrdinal "Name")
                    Value    = reader.GetDouble   (reader.GetOrdinal "Value")
                    DateTime = reader.GetDateTime (reader.GetOrdinal "Dt")
                }}

        this.SeqToResult (s, 
            (s |> Seq.toArray).Length <> 0 && 
            (s |> Seq.distinctBy (fun x -> x.Name) |> Seq.toArray).Length = 1 )

    // Transform type Metric seq to type Result
    member this.SeqToResult (sequence, ?isSingleMetric) =
        match defaultArg isSingleMetric false with
        | false -> PlainResult ({Data = sequence |> Seq.sortBy (fun x -> x.DateTime)})
        | true  -> StatisticsResult (
                    {
                        Data    = sequence |> Seq.sortBy (fun x -> x.DateTime)
                        Mean    = sequence |> this.Mean   
                        Median  = sequence |> this.Median 
                        Mode    = sequence |> this.Mode   
                        Maximum = sequence |> this.Maximum
                        Minimum = sequence |> this.Minimum
                        Range   = sequence |> this.Range  
                    })

    // Gets maximum value of Metric seq
    member this.Maximum sequence =
        sequence 
            |> Seq.max
            |> double 

    // Gets minimum value of Metric seq
    member this.Minimum sequence =
        sequence 
            |> Seq.min 
            |> double

    // Gets mean of Metric seq
    member this.Mean sequence =
        sequence 
            |> Seq.average 
            |> double

    // Gets median of Metric Seq
    member this.Median sequence =
        let sorted = 
            sequence 
                |> Seq.toArray 
                |> Array.sort
        let (m1, m2) =
            let len = sorted.Length - 1 |> float
            (len / 2. |> floor |> int), (len / 2. |> ceil |> int)
        (sorted.[m1] + sorted.[m2] |> float) / 2.

    // Gets mode of Metric Seq
    member this.Mode sequence =
        let uniques = [for x in sequence |> Seq.distinctBy (fun y -> y.Value) -> x.Value] |> List.sort
        let getCount (value : double) =
            let c : double = Convert.ToDouble((sequence 
                                                   |> Seq.filter (fun x -> x.Value = value) 
                                                   |> Seq.toArray).Length)
            [value; c]
        uniques 
            |> List.map getCount 
            |> List.sortBy (fun x -> x.Tail) 
            |> List.rev 
            |> List.head 
            |> List.head

    // Gets range of Metric seq
    member this.Range sequence =
        let (m1, m2) = 
            (sequence |> Seq.max |> double), (sequence |> Seq.min |> double)
        m1 - m2

// End Metrics class