# Preferred Patterns

## Core Principles

- Treat the connected client as `Client.Connected` (an `IGraphClient`) and pipe its `.Cypher` through the `CypherFluentQuery` functions instead of writing raw query strings by hand.
- Build node patterns from typed building blocks (`NodeId`, `NodeType`, `Name`, `Placeholder`) and the node operators rather than interpolating strings; this keeps escaping and casing consistent.
- Keep query construction (`RawCypherQuery` / `Cypher`) separate from execution (`Cypher.execute`) so the same query can be dumped for inspection before it runs.
- Prefer `Async`/`AsyncResult` flows from `Feather.ErrorHandling`; most builder functions return `AsyncResult` and errors are typed.

## Recommended API Usage

- **Connecting**: parse settings with `Client.Connection.parse` from a JSON config, then `Client.connect debug connection` for an `AsyncResult<Connected, _>`, or `Client.connectOrFail` when a hard failure is acceptable at startup. Port defaults to 7474 (Http) or 7687 (Bolt) when omitted. See `examples.md` → Basic.
- **Reading**: pipe `client.Cypher` into `CypherFluentQuery.matchText` (or `matchNode nodeType nodeId`) and finish with `fetchResults<'a> nodeId`, which returns an `AsyncResult` of a typed list. See `examples.md` → Realistic.
- **Node operators**: use `nodeId @ nodeType` for `(id:TYPE)`, `(nodeId @<*> nodeType) (field => value)` for a single inline property, `(nodeId @<***> nodeType) [ field, value; ... ]` for several, and `(nodeId @<?> nodeType) (field => placeholder)` to emit a `$placeholder` that you later bind with `cypher <?=> (placeholder => value)`. Open `Alma.Neo4j.CypherFluentQuery.NodeOperators` to bring them into scope.
- **Higher-level writes**: build a `GraphQueryNode` and turn it into a `Cypher` with `Cypher.fromNode`, or build a `Link` (operators `-|`, `&>`, `|->`) and use `Cypher.fromLink`. `RawCypherQuery.addGraphNode` issues a `MERGE ... ON CREATE SET` using name/dto placeholders. See `examples.md` → Integration and Full Workflow.

## Error Handling

- Surface failures through the typed error unions and their `format` helpers: `ConfigurationError.format`, `AssertNodeError.format` (via `RawCypherQueryError.format`), and `ExecuteCypherError.format`.
- `Cypher.execute` returns a `Validation`/`Result` that aggregates per-query errors for a `Cypher.Multi`; inspect it rather than assuming success.
- `RawCypherQuery.addGraphLink` asserts both endpoint nodes exist before merging the relationship and yields `AssertNodeError.NodeNotExists` (carrying the offending `CypherRunableQuery`) when one is missing.

## Composition

- A `Cypher` is one of `RawQuery`, `Composition` (a `RawCypherQuery -> RawCypherQuery` function), or `Multi` (a list). Combine independent writes into a single `Cypher.Multi` and run them together with `Cypher.execute`; `Cypher.countQueries` reports how many statements it represents.
- `RawCypherQuery` threads an `AsyncResult` Cypher through `mapCypher`/`bindCypher`, so steps compose without losing error context.

## Dry Run vs Execute

- A `RawCypherQuery` carries a `Neo4jMode`. Start it with `RawCypherQuery.start (OutputOnly log) client` to build without touching the database (node-existence assertions are logged and skipped), or `RawCypherQuery.start Execute client` to run for real.
- Use `Cypher.dump` with a `Log` to render the generated query, and `CypherRunableQuery.format` to produce a `:params { ... }`-prefixed string you can paste into the Neo4j browser; `CypherRunableQuery.dump` is the human-readable variant for logs. See `examples.md` → Test.

## Integration with Other Libraries

- The library returns `Feather.ErrorHandling` types (`AsyncResult`, `Result`, `Validation`); open its operator modules (e.g. `AsyncResult.Operators`) for the `<@>`, `<!>`, `>>*` combinators used to map errors and tee values.
- DTOs are plain mutable classes consumed directly by `Neo4jClient` for (de)serialization; the prepared ones live in the `Dto` module.

## Naming Conventions

- `NodeType.value` upper-cases the label; `PropertyName.value` upper-cases the first letter; `NodeId.value` strips separators and renders `*` as `Any`. Construct these through their `create`/`ofString` functions so normalization is applied consistently.
- `Name.Property` is the conventional `PropertyName` for a node's name field; derive node ids from a node with `GraphQueryNode.id` / `NodeId.create nodeType name` rather than ad-hoc strings.

## Testing Recommendations

- Build queries in `OutputOnly` mode and assert on the rendered Cypher via `CypherRunableQuery.dump` / `format` — this validates query generation without a live database. See `examples.md` → Test.
