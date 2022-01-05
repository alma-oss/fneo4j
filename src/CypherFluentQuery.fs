namespace Lmc.Neo4j

open Lmc.ErrorHandling

type CypherFluentQuery = Neo4jClient.Cypher.ICypherFluentQuery

[<RequireQualifiedAccess>]
module CypherFluentQuery =
    module NodeOperators =
        /// Node with: (nodeId:TYPE)
        let (@) nodeId nodeType =
            sprintf "(%s:%s)"
                (nodeId |> NodeId.value)
                (nodeType |> NodeType.value)

        /// Node with: (nodeId:TYPE { Field1: "value1", Field2: "value2", ... })
        let (@<***>) nodeId nodeType fields =
            sprintf "(%s:%s { %s })"
                (nodeId |> NodeId.value)
                (nodeType |> NodeType.value)
                (
                    fields
                    |> List.map (fun (field, value) ->
                        sprintf "%s: \"%s\""
                            (field |> PropertyName.value)
                            value
                    )
                    |> String.concat ", "
                )

        /// Node with: (nodeId:TYPE { Field: "value" })
        let (@<*>) nodeId nodeType (field, value) =
            (nodeId @<***> nodeType) [ (field, value) ]

        /// Node with: (nodeId:TYPE { Field: $placeholder })
        /// You must set placeholder value to the query -> see (<?=>) below
        let (@<?>) nodeId nodeType (field, placeholder) =
            sprintf "(%s:%s { %s: $%s })"
                (nodeId |> NodeId.value)
                (nodeType |> NodeType.value)
                (field |> PropertyName.value)
                (placeholder |> Placeholder.value)

        /// Add parameter value for placeholder to cypher query.
        let (<?=>) (cypher: CypherFluentQuery) (placeholder, value: obj) =
            cypher.WithParam(placeholder |> Placeholder.value, value)

    open NodeOperators

    //
    // Low level functions
    //

    let merge (mergeText: string) (cypher: CypherFluentQuery): CypherFluentQuery =
        cypher.Merge(mergeText)

    let matchText (matchText: string) (cypher: CypherFluentQuery): CypherFluentQuery =
        cypher.Match(matchText)

    let where condition (cypher: CypherFluentQuery): CypherFluentQuery =
        cypher.Where(condition)

    let andWhere condition (cypher: CypherFluentQuery): CypherFluentQuery =
        cypher.AndWhere(condition)

    let orWhere condition (cypher: CypherFluentQuery): CypherFluentQuery =
        cypher.OrWhere(condition)

    let matchNode nodeType nodeId (cypher: CypherFluentQuery): CypherFluentQuery =
        matchText (nodeId @ nodeType) cypher

    let fetchResults<'a> nodeId (cypher: CypherFluentQuery) = asyncResult {
        let! results =
            cypher
                .Return<'a>(nodeId |> NodeId.value)
                .ResultsAsync

        return results |> Seq.toList
    }
