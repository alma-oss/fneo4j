# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased
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
