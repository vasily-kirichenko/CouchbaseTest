open System
open System.Threading
open System.Diagnostics
open Couchbase
open Couchbase.Configuration.Client

[<EntryPoint>]
let main = function
    | [|count; minId; maxId; threads|] ->
        let count = int count
        let minId = int minId
        let maxId = int maxId
        let threads = int threads
        use countdown = new CountdownEvent(threads)
        let config = ClientConfiguration()
        config.Servers.Add (Uri "http://127.0.0.1:8091/pools")
        let cluster = new Cluster(config)
        let worker count =
            Console.WriteLine (sprintf "Getting %d docs..." count)
            use bucket = cluster.OpenBucket "test"
            let get (key: int) =
                let res = bucket.Get (string key)
                if not res.Success then printfn "Cannot get: %s" res.Message
            let rnd = Random()
            for i in 1..count do get (rnd.Next (minId, maxId))
            countdown.Signal() |> ignore
    
        let sw = Stopwatch.StartNew()
        let chunk = count / threads
        for thread in 0..threads-1 do
            Thread(fun() -> worker (count / threads)).Start()
        countdown.Wait()
        sw.Stop()
        printfn "Done in %O, %0.1f /s" sw.Elapsed (float count / float sw.ElapsedMilliseconds * 1000.)
        0
    | _ -> failwith "Wrong arguments, expected <count> <min id> <max id> <threads>"
    