# Anti-Patterns

Each entry is **mistake → why → fix**.

## DTO / Node Definitions

- **Defining graph nodes as F# records** → `Neo4jClient` serializes and hydrates nodes by mutating properties, which records do not allow → define nodes as mutable classes with a parameterless constructor and `get`/`set` members; reuse the prepared classes in the `Dto` module (`Dto.Name`, `Dto.NameAndLabel`) where they fit.

- **Annotating DTO properties with the wrong serialized name** → property casing/name drives how data maps to Neo4j fields → put a `JsonProperty(PropertyName = "...")` attribute on the member when the stored field name differs from the F# member name.

## Placeholders & Parameters

- **Using `@<?>` and forgetting to bind the placeholder** → `@<?>` only emits a `$placeholder` token; the query has no value for it until you bind one → set it with `cypher <?=> (placeholder => value)`, building the placeholder via `Placeholder.withId nodeId "suffix"` so it matches.

- **Reusing the same placeholder suffix for unrelated values on the same node** → placeholders are derived from the node id plus a suffix, so colliding suffixes overwrite each other → give each value a distinct suffix (e.g. `"name"`, `"dto"`).

## Query Construction

- **Hand-writing node pattern strings like `"(m:TYPE { Name: \"x\" })"`** → bypasses normalization (label casing, id sanitisation, escaping) and drifts from the rest of the codebase → compose with the node operators (`@`, `@<*>`, `@<***>`, `@<?>`) and the typed `NodeId`/`NodeType`/`PropertyName` constructors instead.

- **Assuming `addGraphLink` will create missing endpoint nodes** → it first asserts both nodes exist and fails with `AssertNodeError.NodeNotExists` if not → ensure the nodes are merged first (e.g. via `addGraphNode` in the same `Cypher.Multi`), or use `addGraphLinkWithoutAssertion` when you deliberately want to skip the existence check.

## Execution & Modes

- **Calling `.Cypher` directly and `await`ing without typed error handling** → you lose the library's typed errors and aggregation → run through `Cypher.execute` (which returns a `Result`/`Validation` aggregating `ExecuteCypherError`) and format failures with `ExecuteCypherError.format`.

- **Expecting `OutputOnly` mode to write to the database** → in `OutputOnly` mode node-existence assertions are logged and skipped and nothing is executed; it exists purely to render Cypher → use `RawCypherQuery.start Execute client` (or `Cypher.execute`) when you actually want the statements to run.

## Project Setup

- **Reordering the `<Compile>` items in `Neo4j.fsproj`** → F# requires strict dependency order and the source files build on one another (utils → client/types → cypher types → fluent/raw/query → dto) → keep the existing compile order when adding files.

## Configuration

- **Hardcoding the connection URL scheme/port** → the scheme and default port depend on `ConnectionType` (Http → `http://…:7474`, Bolt → `neo4j://…:7687`) and are derived by the library → supply a parsed `Connection` and let `Client.connect` choose `GraphClient` vs `BoltGraphClient`.
