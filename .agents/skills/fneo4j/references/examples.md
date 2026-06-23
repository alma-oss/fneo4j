# Examples

All example code for this skill lives here. Examples are ordered by increasing complexity and each is self-contained. Neutral placeholders (`ServiceA`, `ExampleApi`, `DemoSystem`, `NodeA`, etc.) are used throughout.

## Basic — Connect a client

```fsharp
open Alma.Neo4j
open Feather.ErrorHandling

let connectExample (configJson: string): AsyncResult<Client.Connected, _> =
    asyncResult {
        let! connection = Client.Connection.parse configJson |> AsyncResult.ofResult
        let! client = connection |> Client.connect (printfn "%s")
        return client
    }
```

The configuration JSON supplies `userName`, `password`, `host`, optional `port`, `version` (e.g. `4`), and `type` (`"http"`, `"bolt"`/`"neo4j"`, or omitted for Http). When `port` is omitted it defaults to 7474 for Http and 7687 for Bolt.

## Basic — A node DTO

```fsharp
open Newtonsoft.Json

type ExampleNode () =
    let mutable nameValue = ""

    [<JsonProperty(PropertyName = "name")>]
    member __.Name
        with get() = nameValue
        and set (value) = nameValue <- value
```

## Realistic — Match and fetch with functions and operators

```fsharp
open Alma.Neo4j
open Alma.Neo4j.CypherFluentQuery
open Alma.Neo4j.CypherFluentQuery.NodeOperators
open Feather.ErrorHandling

// Match by raw pattern text
let fetchByText (client: Client.Connected): AsyncResult<string list, _> =
    let nodeId = NodeId.ofString "n"
    client.Cypher
    |> matchText (nodeId @ NodeType.create "ServiceA")
    |> fetchResults<string> nodeId

// Match a typed node with an inline property
let fetchByProperty (client: Client.Connected): AsyncResult<string list, _> =
    let nodeId = NodeId.ofString "n"
    let nodeType = NodeType.create "ServiceA"
    let name = Name.ofString "demo-instance"

    client.Cypher
    |> matchText ((nodeId @<*> nodeType) (Name.Property => (name |> Name.value)))
    |> fetchResults<string> nodeId

// Match with a placeholder, then bind its value
let fetchByPlaceholder (client: Client.Connected): AsyncResult<string list, _> =
    let nodeId = NodeId.ofString "n"
    let nodeType = NodeType.create "ServiceA"
    let namePlaceholder = Placeholder.withId nodeId "name"

    client.Cypher
    |> (fun cypher -> cypher <?=> (namePlaceholder => box "demo-instance"))
    |> matchText ((nodeId @<?> nodeType) (Name.Property => namePlaceholder))
    |> fetchResults<string> nodeId
```

## Integration — Build a typed node and link, run as one Cypher

```fsharp
open Alma.Neo4j
open Alma.Neo4j.Link.Operators
open Feather.ErrorHandling

let nodeA : GraphQueryNode<string> = {
    Type = NodeType.create "ServiceA"
    Name = Name.ofString "alpha"
    Dto = Dto.withName (Name.ofString "alpha")
}

let nodeB : GraphQueryNode<string> = {
    Type = NodeType.create "ExampleApi"
    Name = Name.ofString "beta"
    Dto = Dto.withName (Name.ofString "beta")
}

// A relationship: (alpha)-[:RELATES_TO { Since: '2024' }]->(beta)
let link =
    (nodeA -| "RELATES_TO")
    &> [ PropertyName.create "Since", "2024" ]
    |-> nodeB

let writeGraph (client: Client.Connected) =
    let query =
        Cypher.Multi [
            Cypher.fromNode nodeA
            Cypher.fromNode nodeB
            Cypher.fromLink link
        ]

    query |> Cypher.execute client Cypher.ExecuteEvents.ignore
```

## Test — Dump generated Cypher without a database

```fsharp
open Alma.Neo4j

let dumpGraph (client: Client.Connected) (node: GraphQueryNode<string>) =
    let log = {
        LogQuery = fun runable -> runable |> CypherRunableQuery.dump |> printfn "%s"
        LogMessage = printfn "%s"
    }

    node
    |> Cypher.fromNode
    |> Cypher.dump log client
```

`CypherRunableQuery.dump` renders the query plus parameters for logs; `CypherRunableQuery.format` instead emits a `:params { ... }`-prefixed single line suitable for pasting into the Neo4j browser.

## Full Workflow — Connect, compose, execute with events

```fsharp
open Alma.Neo4j
open Alma.Neo4j.Link.Operators
open Feather.ErrorHandling

let run (configJson: string) =
    asyncResult {
        let! connection = Client.Connection.parse configJson |> AsyncResult.ofResult
        let! client = connection |> Client.connect ignore

        let serviceNode : GraphQueryNode<string> = {
            Type = NodeType.create "DemoSystem"
            Name = Name.ofString "worker-1"
            Dto = Dto.withName (Name.ofString "worker-1")
        }

        let events = {
            Cypher.ExecuteEvents.OnBeforeExecute =
                fun runable -> runable |> CypherRunableQuery.dump |> printfn "running: %s"
            Cypher.ExecuteEvents.OnAfterExecute =
                function
                | Ok () -> printfn "ok"
                | Error e -> e |> ExecuteCypherError.format |> printfn "failed: %s"
        }

        return
            serviceNode
            |> Cypher.fromNode
            |> Cypher.execute client events
    }
```
