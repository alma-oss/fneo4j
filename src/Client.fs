namespace Lmc.Neo4j

open Lmc.ErrorHandling

[<RequireQualifiedAccess>]
module Neo4jServer =
    type Version =
        | Version4x

    type ConnectionType =
        | Http
        | Bolt

    [<RequireQualifiedAccess>]
    module Version =
        let parse = function
            | 4 -> Ok Version4x
            | unsupported -> Error unsupported

    [<RequireQualifiedAccess>]
    module ConnectionType =
        let parse = function
            | Some "http" | None -> Ok Http
            | Some "bolt" | Some "neo4j" -> Ok Bolt
            | Some invalid -> Error invalid

type ConfigurationError =
    | UnsupportedVersion of int
    | InvalidConnectionType of string
    | ParseError of exn

[<RequireQualifiedAccess>]
module ConfigurationError =
    let format = function
        | UnsupportedVersion unsupported -> sprintf "Unsupported version %A." unsupported
        | InvalidConnectionType invalid -> sprintf "Invalid connection type %A." invalid
        | ParseError e -> sprintf "Configration file could not be parsed due to:\n%A" e

[<RequireQualifiedAccess>]
module Client =
    open System

    type Connected = Neo4jClient.IGraphClient  // if this would be hidden by private Connected of GraphClient -> then user cant use `use` with inner Disposable

    type Server = {
        Version: Neo4jServer.Version
        Type: Neo4jServer.ConnectionType
    }

    type Connection = {
        UserName: string
        Password: string
        Host: string
        Port: int
        Server: Server
    }

    [<RequireQualifiedAccess>]
    module Connection =
        open FSharp.Data
        open Lmc.ErrorHandling.Result.Operators

        let internal toBaseUrl { Host = host; Port = port; Server = server } =
            // see https://github.com/DotNet4Neo4j/Neo4jClient#graphclient
            match server with
            | { Type = Neo4jServer.Bolt } -> sprintf "neo4j://%s:%i" host port |> Uri
            | { Type = Neo4jServer.Http; Version = Neo4jServer.Version4x } -> sprintf "http://%s:%i/" host port |> Uri

        type private ConfigurationSchema = JsonProvider<"src/Schema/configuration.json", SampleIsList = true>

        let parse path = result {
            try
                let parsed = path |> ConfigurationSchema.Parse

                let! connectionType = parsed.Type |> Neo4jServer.ConnectionType.parse <@> InvalidConnectionType
                let! version = parsed.Version |> Neo4jServer.Version.parse <@> UnsupportedVersion

                return {
                    UserName = parsed.UserName
                    Password = parsed.Password
                    Host = parsed.Host
                    Port =
                        match connectionType, parsed.Port with
                        | _, Some port -> port
                        | Neo4jServer.Http, _ -> 7474
                        | Neo4jServer.Bolt, _ -> 7687
                    Server = {
                        Version = version
                        Type = connectionType
                    }
                }
            with e -> return! Error (ParseError e)
        }

    let connect debug connection: AsyncResult<Connected, _> =
        asyncResult {
            let uri =
                connection
                |> Connection.toBaseUrl

            let client =
                match connection.Server.Type with
                | Neo4jServer.Http -> new Neo4jClient.GraphClient(uri, connection.UserName, connection.Password) :> Connected
                | Neo4jServer.Bolt -> new Neo4jClient.BoltGraphClient(uri, connection.UserName, connection.Password) :> Connected

            do!
                client.ConnectAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsyncCatch id

            debug <| sprintf "Neo4j is connected: %A" client.IsConnected

            return client
        }

    let connectOrFail debug connection: Connected =
        async {
            let! client = connection |> connect debug

            return client |> Result.orFail
        }
        |> Async.RunSynchronously
