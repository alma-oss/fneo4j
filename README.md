F-Neo4j
=======

[![NuGet](https://img.shields.io/nuget/v/Alma.Neo4j.svg)](https://www.nuget.org/packages/Alma.Neo4j)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Alma.Neo4j.svg)](https://www.nuget.org/packages/Alma.Neo4j)
[![Tests](https://github.com/alma-oss/fneo4j/actions/workflows/tests.yaml/badge.svg)](https://github.com/alma-oss/fneo4j/actions/workflows/tests.yaml)

This library is just a tinny layer above `Neo4jClient`.

## Install

Add following into `paket.references`
```
Alma.Neo4j
```

## Usage

First you need a Node-like class (_this on has one property - `name`_)
```fs
type Node () =
    let mutable nameValue = ""

    [<JsonProperty(PropertyName = "name")>]
    member __.Name
        with get() = nameValue
        and set (value) = nameValue <- value
```

There are some basic DTOs prepared [here](src/Dto.fs).

### Fetching

Single node
```fs
let fetchAllWithClient (client: ConnectedClient) =
    client.Cypher
        .Match("(m:MOVIE{ name: \"Reservoir Dogs\"})")
        .Return<Node>("m")
        // or more verbosely
        //.Return(fun () -> Return.As<Node>("m"))

        .Results
    |> Seq.map (fun m ->
        printfn "Movie.Name: %A" m.Name
        m
    )
    |> Seq.toList
```

Single node with functions
```fs
let simpleExample (client: Client.Connected): Async<string list> =
    client.Cypher
    |> matchText "(m:MOVIE)"
    |> fetchResults<string> id
```

```fs
let exampleOfMatchNode (client: Client.Connected): Async<string list> =
    let id = NodeId.ofString "m"
    let movieType = NodeType.create "movie"

    client.Cypher
    |> matchNode movieType id
    |> fetchResults<string> id
```

```fs
open CypherFluentQuery.NodeOperators

let exampleOfMatchWithOperators (client: Client.Connected): Async<string list> =
    let id = NodeId.ofString "m"
    let movieType = NodeType.create "movie"

    client.Cypher
    |> matchText (id @ movieType)
    |> fetchResults<string> id
```

```fs
open CypherFluentQuery.NodeOperators

let exampleOfMatchWithOperatorsAndParameters (client: Client.Connected): Async<string list> =
    let nodeId = NodeId.ofString "m"
    let movieType = NodeType.create "movie"

    client.Cypher
    |> matchText (
        (nodeId @<*> movieType) (Name.Property => "Reservoir Dogs")
    )
    |> fetchResults<string> nodeId
```

```fs
open CypherFluentQuery.NodeOperators

let exampleOfMatchWithOperatorsAndParametersTypeSafe (client: Client.Connected): Async<string list> =
    let nodeId = NodeId.ofString "m"
    let movieType = NodeType.create "movie"
    let movieName = Name.ofString "Reservoir Dogs"

    client.Cypher
    |> matchText (
        (nodeId @<*> movieType) (Name.Property => (movieName |> Name.value))
    )
    |> fetchResults<string> nodeId
```

More nodes
> It also _partially_ allows to use a functions shown above.

```fs
let fetchAllWithClient (client: ConnectedClient) =
    client.Cypher
        .Match("(m:MOVIE{ name: \"Reservoir Dogs\"}), (d:DIRECTOR{ name: \"Tarantino\"})")
        .Return(fun () -> Return.As<Node>("m"), Return.As<Node>("d") )

        .Results
    |> Seq.map (fun (m, d) ->
        printfn "Movie.Name: %A" m.Name
        printfn "Director.Name: %A" d.Name
        m
    )
    |> Seq.toList
```

### Creating nodes/links

Create a node
```fs
let create (client: ConnectedClient) =
    let movie = Node ( Name = "Once upon a Hollywood" )

    client.Cypher
        .Create("(n:MOVIE {newMovie})")
        .WithParam("newMovie", movie)
        .ExecuteWithoutResults()
```

Create a link
```fs
let createLink (client: ConnectedClient) =
    client.Cypher
        .Match("(m:MOVIE)", "(d:DIRECTOR)")
        .Where<Node>(fun m -> m.Name = "Once upon a Hollywood")
        .AndWhere<Node>(fun d -> d.Name = "Tarantino")
        .Merge("(m)-[:IS_DIRECTED_BY]->(d)")

        .ExecuteWithoutResults()
```

## CypherFluentQuery
> This module is an abstraction above a `Neo4jClient.Cypher.ICypherFluentQuery`, which adds some functions and operators to allow functional approach

### Operators

Let's use following code _globally_ for sake of simple examples.
```fs
let client: Client.Connected = ...
let cypher = client.Cypher

let nodeId = NodeId.ofString "m"
let nodeType = NodeType.ofString "movie"
let movieName = Name.ofString "Reservoir Dogs"

let nameValue = movieName |> Name.value

let namePlaceholder = Placeholder.withId nodeId "name"
```

| Operator | Usage                                                        | Cypher Result                           | Note |
| ---      | ---                                                          | ---                                     | ---  |
| `@`      | `nodeId @ nodeType`                                          | `(m:MOVIE)`                             | - |
| `@<*>`   | `(nodeId @<*> nodeType) (Name.Property => nameValue)`        | `(m:MOVIE { Name: "Reservoir Dogs" })`  | - |
| `@<?>`   | `(nodeId @<?> nodeType) (Name.Property => namePlaceholder)`  | `(m:MOVIE { Name: { mname } })`         | You must set a placeholder value (see `<?=>`) |
| `<?=>`   | `cypher <?=> (namePlaceholder => nameValue)`                 | -                                       | It is used to set a parameter value. |

## Release
1. Increment version in `Neo4j.fsproj`
2. Update `CHANGELOG.md`
3. Commit new version and tag it

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)

### Build
```bash
./build.sh build
```

### Tests
```bash
./build.sh -t tests
```
