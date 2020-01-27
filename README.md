F-Neo4j
=======

This library is just a tinny layer above `Neo4jClient`.

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

More nodes
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
