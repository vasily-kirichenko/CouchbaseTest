open System
open System.Threading
open System.Diagnostics
open Couchbase
open Couchbase.Configuration.Client

[<EntryPoint>]
let main = function
    | [|count; startId; threads|] ->
        let count = int count
        let startId = int startId
        let threads = int threads
        use countdown = new CountdownEvent(threads)
        let json = """
{"menu": {
  "id": "file",
  "value": "File",
  "popup": {
    "menuitem": [
      {"value": "New", "onclick": "CreateNewDoc()"},
      {"value": "Open", "onclick": "OpenDoc()"},
      {"value": "Close", "onclick": "CloseDoc()"}
    ]
  }
}}"""
        let doc i = Document(Id = string i, Content = json)
        let config = ClientConfiguration()
        config.Servers.Add (Uri "http://127.0.0.1:8091/pools")
        config.PoolConfiguration.MaxSize <- 100
        let cluster = new Cluster(config)
        let worker from to' =
            Console.WriteLine (sprintf "Writing %d..%d..." from to')
            use bucket = cluster.OpenBucket "test"
            let put (key: int) =
                let res = bucket.Insert(doc key) //, ReplicateTo.Zero, PersistTo.Zero)
                if not res.Success then failwithf "Cannot insert: %s" res.Message
            for i in from..to' do put i
            countdown.Signal() |> ignore
    
        let sw = Stopwatch.StartNew()
        let chunk = count / threads
        for thread in 0..threads-1 do
            Thread(fun() -> worker (startId + (thread * chunk)) (startId + (thread * chunk + chunk - 1))).Start()
        countdown.Wait()
        sw.Stop()
        printfn "Done in %O, %0.1f /s" sw.Elapsed (float count / float sw.ElapsedMilliseconds * 1000.)
        0
    | _ -> failwith "Wrong arguments, expected <count> <start id> <threads>"
    