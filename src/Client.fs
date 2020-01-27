namespace Lmc.Neo4j

[<RequireQualifiedAccess>]
module Client =
    open System

    type Connected = Neo4jClient.GraphClient  // if this would be hidden by private Connected of GraphClient -> then user cant use `use` with inner Disposable

    type Connection = {
        UserName: string
        Password: string
        Host: string
        Port: int
    }

    [<RequireQualifiedAccess>]
    module Connection =
        open FSharp.Data

        let internal toUri { UserName = userName; Password = password; Host = host; Port = port } =
            sprintf "http://%s:%s@%s:%i" userName password host port |> Uri

        let internal toBaseUrl { Host = host; Port = port } =
            sprintf "http://%s:%i/db/data" host port |> Uri

        type private ConfigurationSchema = JsonProvider<"src/Schema/configuration.json", SampleIsList = true>

        let parse path =
            let parsed = path |> ConfigurationSchema.Parse

            {
                UserName = parsed.UserName
                Password = parsed.Password
                Host = parsed.Host
                Port = parsed.Port
            }

    let connectClient debug connection: Connected =
        let uri =
            connection
            |> Connection.toBaseUrl

        async {
            let client = new Neo4jClient.GraphClient(uri, connection.UserName, connection.Password)

            do! client.ConnectAsync() |> Async.AwaitTask
            debug <| sprintf "Neo4j is connected: %A" client.IsConnected

            return client
        }
        |> Async.RunSynchronously
