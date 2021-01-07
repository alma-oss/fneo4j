# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased
- [**BC**] Add `Server` field to the Connection configuration
- [**BC**] Change `Connection.parse` to return a `Result`
    - it also parse a `Server` fields
    - allow `port` field not to be set for default values (based on `connectionType`)
- [**BC**] Remove `Connection.connectClient`
- Add `Connection.connect` and `Connection.connectOrFail` functions
- Change inner type of `Client.Connected` to the `IGraphClient`, so the Client.Connected could be both `BoltGraphClient` or `GraphClient`
- [**BC**] Support Neo4j Server `4.x` only
- [**BC**] Rename `CypherQuery.toCypherResult` to `CypherQuery.toCypher` and remove a previous `CypherQuery.toCypher` function

## 2.0.0 - 2020-11-23
- [**BC**] Use .netcore 5.0

## 1.4.0 - 2020-11-23
- Update dependencies

## 1.3.0 - 2020-03-05
- Add `CypherFluentQuery` operator `@<***>` to match a node by multiple fields

## 1.2.0 - 2020-02-12
- Add `CypherFluentQuery` functions:
    - `where`
    - `andWhere`
    - `orWhere`
- Add `NodeId.empty` function

## 1.1.0 - 2020-01-28
- Add more examples to Readme
- Open `CypherFluentQuery.NodeOperators` as public
- Add functions to `CypherFluentQuery` module:
    - `matchText`
    - `matchNode`
    - `fetchResults`
- Add function to basic types:
    `Name.ofString`
    `NodeId.ofString`
    `NodeType.ofString` (_as alias of NodeType.create_)

## 1.0.0 - 2020-01-27
- Initial implementation
