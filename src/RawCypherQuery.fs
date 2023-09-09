namespace Alma.Neo4j

open Alma.ErrorHandling

// --------

type LogQuery = CypherRunableQuery -> unit

type Log = {
    LogQuery: LogQuery
    LogMessage: string -> unit
}

type Neo4jMode =
    | OutputOnly of Log
    | Execute

/// "Monadic" Wrapper for CypherFluentQuery, which allows to dry run the Cypher, or inject a Client later, ...
type RawCypherQuery = private {
    Mode: Neo4jMode
    Client: Client.Connected
    Cypher: AsyncResult<CypherFluentQuery, RawCypherQueryError>
}

[<RequireQualifiedAccess>]
module RawCypherQuery =
    let cypher { Cypher = cypher } = cypher

    let start mode (client: Client.Connected) =
        { Mode = mode; Client = client; Cypher = AsyncResult.ofSuccess client.Cypher }

    let private map f rawCypherQuery =
        { rawCypherQuery with Cypher = rawCypherQuery |> f |> AsyncResult.ofSuccess }

    let private bind f rawCypherQuery =
        { rawCypherQuery with Cypher = rawCypherQuery |> f }

    let mapCypher f rawCypherQuery =
        { rawCypherQuery with Cypher = rawCypherQuery.Cypher |> AsyncResult.map f }

    let bindCypher f rawCypherQuery =
        { rawCypherQuery with Cypher = rawCypherQuery.Cypher |> AsyncResult.bind f }

    //
    // High level functions
    //

    open Option.Operators
    open CypherFluentQuery.NodeOperators

    let addGraphNode node (rawCypherQuery: RawCypherQuery): RawCypherQuery =
        let nodeId = node |> GraphQueryNode.id

        let placeholder = Placeholder.withId nodeId
        let namePlaceholder = placeholder "name"
        let dtoPlaceholder = placeholder "dto"

        rawCypherQuery
        |> mapCypher (fun cypher ->
            cypher
                .Merge(
                    (nodeId @<?> node.Type) (Name.Property => namePlaceholder)
                )
                .OnCreate()
                .Set(sprintf "%s = $%s" (nodeId |> NodeId.value) (dtoPlaceholder |> Placeholder.value))
                <?=> (namePlaceholder => (node.Name |> Name.value))
                <?=> (dtoPlaceholder => node.Dto)
        )

    open AsyncResult.Operators

    let addGraphLink link rawCypherQuery =
        let assertNodeExists { Mode = mode; Client = client } { Id = nodeId; Type = nodeType; Name = name } =
            match mode with
            | OutputOnly log ->
                (nodeId @<*> nodeType) (Name.Property => (name |> FormattedName.value))
                |> sprintf "skipped -> Asserting node exist %s"
                |> log.LogMessage
                |> AsyncResult.ofSuccess

            | Execute ->
                asyncResult {
                    let cypher =
                        client.Cypher
                        |> CypherFluentQuery.matchText (
                            (nodeId @<*> nodeType) (Name.Property => (name |> FormattedName.value))
                        )

                    let! results =
                        cypher
                        |> CypherFluentQuery.fetchResults nodeId <@> AssertNodeError.RuntimeError

                    if results |> List.isEmpty then
                        return!
                            AssertNodeError.NodeNotExists (cypher |> CypherRunableQuery.ofCypher)
                            |> AsyncResult.ofError
                }

        let matchNodeByName (nodeForLink: GraphQueryNodeForLink) rawCypherQuery =
            asyncResult {
                let! cypher = rawCypherQuery.Cypher

                do! nodeForLink |> assertNodeExists rawCypherQuery <@> AssertNodeError
                let { Id = nodeId; Type = nodeType; Name = name } = nodeForLink

                return
                    cypher
                    |> CypherFluentQuery.matchText (
                        (nodeId @<*> nodeType) (Name.Property => (name |> FormattedName.value))
                    )
            }

        rawCypherQuery
        |> bind (matchNodeByName link.From)
        |> bind (matchNodeByName link.To)
        |> mapCypher (CypherFluentQuery.merge (link |> Link.toCypher))

    let addGraphLinkWithoutAssertion link rawCypherQuery =
        rawCypherQuery
        |> mapCypher (CypherFluentQuery.merge (link |> Link.toCypher))
