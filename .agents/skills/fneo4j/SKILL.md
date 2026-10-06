---
name: fneo4j
description: >-
  Use whenever generating or reviewing F# code that talks to a Neo4j graph database via the Alma.Neo4j library — connecting a client (Client.connect, Client.connectOrFail, Connection.parse), building Cypher with the fluent module (matchText, matchNode, merge, where/andWhere/orWhere, fetchResults<'a>) or the node operators (@, @<*>, @<***>, @<?>, <?=>), composing higher-level queries (RawCypherQuery.addGraphNode/addGraphLink, Cypher.Composition/Multi, Cypher.execute, Cypher.dump), or defining graph DTO classes. Trigger also on mentions of NodeId, NodeType, Name, Label, LinkType, Placeholder, Attributes, GraphQueryNode, CypherRunableQuery, ExecuteCypherError, Bolt/Http GraphClient, or "Neo4jClient wrapper in F#".
---

# F-Neo4j

Library: [alma-oss/fneo4j](https://github.com/alma-oss/fneo4j)
NuGet: `Alma.Neo4j`

## Purpose

`Alma.Neo4j` is a thin functional F# layer on top of the `Neo4jClient` .NET library. It adds a Cypher query DSL (fluent functions plus custom operators for node/link composition), typed and named graph nodes, and a composable query type that can either execute against a connected client or be dumped as raw Cypher for inspection.

## When to Use

- Connecting to a Neo4j instance from F# and running Cypher.
- Building Cypher matches/merges functionally instead of raw strings.
- Composing several node/link writes into one batched query.
- Producing the raw Cypher + parameters for logging or dry runs.

## When NOT to Use

- Non-Neo4j databases, or when the consuming code already holds a raw `Neo4jClient.ICypherFluentQuery` and only needs vanilla `Neo4jClient` calls.
- Defining graph nodes as immutable F# records — DTOs must be mutable classes (see `references/anti-patterns.md`).

## Main Concepts

- `Client.Connected` — alias for `Neo4jClient.IGraphClient`; the connected client exposing `.Cypher`.
- `Client.Connection` / `Connection.parse` — connection settings parsed from JSON config (Http or Bolt, version, host, port).
- `Neo4jServer.ConnectionType` / `Version` — Http (default port 7474) vs Bolt (default port 7687); `Version4x`.
- `CypherFluentQuery` — alias for `Neo4jClient.Cypher.ICypherFluentQuery`; the module wraps it with F# functions.
- `CypherFluentQuery.NodeOperators` — operators `@`, `@<*>`, `@<***>`, `@<?>`, `<?=>` that build Cypher node patterns and bind parameters.
- `NodeId`, `NodeType`, `Name<'Value>`, `Label<'Value>`, `LinkType`, `PropertyName`, `Placeholder` — typed building blocks for node patterns.
- `Attributes` — a map of `PropertyName → string` with a merge `(+)` operator, used on links.
- `GraphQueryNode<'Value>` / `GraphQueryNodeForLink` — a typed node (type, name, DTO) and its link-ready projection.
- `Link` plus operators `-|`, `&>`, `|->` — build a relationship between two nodes.
- `RawCypherQuery` — a wrapper carrying a `Neo4jMode` (`OutputOnly log` or `Execute`), a client, and an `AsyncResult` Cypher; supports `addGraphNode`, `addGraphLink`, `addGraphLinkWithoutAssertion`.
- `Cypher` — `RawQuery` | `Composition` | `Multi`; the executable/dumpable unit built by `Cypher.fromNode` / `Cypher.fromLink` and run by `Cypher.execute` or inspected by `Cypher.dump`.
- `CypherQuery<'Value, 'Error>` — a function from a value to a `Result<Cypher, 'Error>`.
- `CypherRunableQuery` — `CypherQueryText * CypherQueryParameters`; `dump` and `format` render it for logs / the Neo4j browser.
- Error types — `ConfigurationError`, `AssertNodeError`, `RawCypherQueryError`, `ExecuteCypherError`, each with a `format` function.

## Related Libraries

- `Neo4jClient` — the underlying .NET client; this library wraps `ICypherFluentQuery` and `IGraphClient`.
- `Feather.ErrorHandling` — supplies `asyncResult`/`AsyncResult`/`Result`/`Validation` used throughout the API.
- `FSharp.Data` — `JsonProvider` parses the connection configuration JSON.

## Keywords for Search

Neo4j, Cypher, F#, Alma.Neo4j, Neo4jClient, graph database, IGraphClient, BoltGraphClient, Client.connect, connectOrFail, Connection.parse, matchText, matchNode, fetchResults, merge, where, NodeId, NodeType, Name, Label, LinkType, Placeholder, Attributes, node operators, GraphQueryNode, RawCypherQuery, addGraphNode, addGraphLink, Cypher.execute, Cypher.dump, Composition, Multi, CypherRunableQuery, ExecuteCypherError, asyncResult, DTO mutable class

## Reference Files

For composition principles and recommended API usage, read `references/preferred-patterns.md`. For known pitfalls and incorrect assumptions, read `references/anti-patterns.md`. For worked code examples, read `references/examples.md`.
