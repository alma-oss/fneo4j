namespace Lmc.Neo4j

open Lmc.ErrorHandling

type CypherFluentQuery = Neo4jClient.Cypher.ICypherFluentQuery

type CypherQueryText = CypherQueryText of string

[<RequireQualifiedAccess>]
module CypherQueryText =
    let value (CypherQueryText queryText) = queryText

type CypherQueryParameters = CypherQueryParameters of string list

[<RequireQualifiedAccess>]
module CypherQueryParameters =
    let value (CypherQueryParameters parameters) = parameters

type LogQuery = CypherQueryText -> CypherQueryParameters -> unit

type Log = {
    LogQuery: LogQuery
    LogMessage: string -> unit
}

type Neo4jMode =
    | OutputOnly of Log
    | Execute

type RawCypherQuery = private {
    Mode: Neo4jMode
    Client: Client.Connected
    Cypher: CypherFluentQuery
}

[<RequireQualifiedAccess>]
module RawCypherQuery =
    let cypher { Cypher = cypher } = cypher

    let start mode (client: Client.Connected) =
        { Mode = mode; Client = client; Cypher = client.Cypher }

    let mapQuery f rawCypherQuery =
        { rawCypherQuery with Cypher = rawCypherQuery |> f }

    module Operators =
        /// Map f
        let (<!>) query f =
            query |> mapQuery f

        /// Compose f1 and f2 and lift them to operate on RawCypherQuery
        let (>>>) f1 f2 =
            mapQuery f1 >> mapQuery f2

        /// Compose f1 and f2 and lift f2 to operate on RawCypherQuery
        let (>!>) (f1: RawCypherQuery -> RawCypherQuery) f2: RawCypherQuery -> RawCypherQuery =
            f1 >> mapQuery f2

[<RequireQualifiedAccess>]
module CypherFluentQuery =
    open Option.Operators

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

        /// Node with: (nodeId:TYPE { Field: {placeholder} })
        /// You must set placeholder value to the query -> see (<?=>) below
        let (@<?>) nodeId nodeType (field, placeholder) =
            sprintf "(%s:%s { %s: {%s} })"
                (nodeId |> NodeId.value)
                (nodeType |> NodeType.value)
                (field |> PropertyName.value)
                (placeholder |> Placeholder.value)

        /// Add parameter value for placeholder to cypher query.
        let (<?=>) (cypher: CypherFluentQuery) (placeholder, value: obj) =
            cypher.WithParam(placeholder |> Placeholder.value, value)

    open NodeOperators

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

    let fetchResults<'a> nodeId (cypher: CypherFluentQuery) =
        cypher
            .Return<'a>(nodeId |> NodeId.value)
            .ResultsAsync
        |> Async.AwaitTask
        |> Async.map Seq.toList

    let addGraphNode node { Cypher = cypher }: CypherFluentQuery =
        let nodeId = node |> GraphQueryNode.id

        let placeholder = Placeholder.withId nodeId
        let namePlaceholder = placeholder "name"
        let dtoPlaceholder = placeholder "dto"

        cypher
            .Merge(
                (nodeId @<?> node.Type) (Name.Property => namePlaceholder)
            )
            .OnCreate()
            .Set(sprintf "%s = {%s}" (nodeId |> NodeId.value) (dtoPlaceholder |> Placeholder.value))
            <?=> (namePlaceholder => (node.Name |> Name.value))
            <?=> (dtoPlaceholder => node.Dto)

    let private addUniqueLink link (query: CypherFluentQuery): CypherFluentQuery =
        query.CreateUnique(link)

    let private assertNodeExists mode (client: Client.Connected) { Id = nodeId; Type = nodeType; Name = name } =
        match mode with
        | OutputOnly log ->
            (nodeId @<*> nodeType) (Name.Property => (name |> FormattedName.value))
            |> sprintf "skipped -> Asserting node exist %s"
            |> log.LogMessage
        | Execute ->
            let query =
                client.Cypher
                |> matchText (
                    (nodeId @<*> nodeType) (Name.Property => (name |> FormattedName.value))
                )

            let results =
                query
                |> fetchResults nodeId
                |> Async.RunSynchronously

            if results |> Seq.isEmpty then
                [
                    "=====================\nAsserting node exists"
                    sprintf " -> %s" query.Query.QueryText
                ]
                |> String.concat "\n"
                |> failwith

    let addGraphLinkWithoutMatching link { Cypher = cypher }: CypherFluentQuery =
        cypher
        |> addUniqueLink (link |> Link.toCypher)

    let private matchNodeByName (nodeForLink: GraphQueryNodeForLink) rawCypherQuery =
        let { Id = nodeId; Type = nodeType; Name = name } =
            nodeForLink
            |> tee (assertNodeExists rawCypherQuery.Mode rawCypherQuery.Client)

        { rawCypherQuery with
            Cypher =
                rawCypherQuery.Cypher
                |> matchText (
                    (nodeId @<*> nodeType) (Name.Property => (name |> FormattedName.value))
                )
        }

    let addGraphLink link rawCypherQuery: CypherFluentQuery =
        rawCypherQuery
        |> matchNodeByName link.From
        |> matchNodeByName link.To
        |> RawCypherQuery.cypher
        |> addUniqueLink (link |> Link.toCypher)

[<RequireQualifiedAccess>]
type Cypher =
    | RawQuery of RawCypherQuery
    | Composition of (RawCypherQuery -> RawCypherQuery)
    | Multi of Cypher list

type CypherError =
    | RuntimeError of CypherQueryText * exn

[<RequireQualifiedAccess>]
module CypherError =
    let format = function
        | RuntimeError (CypherQueryText queryText, error) ->
            sprintf "Query:\n%s\n\nEnds with error:\n%A\n" queryText error

[<RequireQualifiedAccess>]
module Cypher =
    open Result.Operators

    let private executeQuery { Cypher = cypher } =
        try
            cypher.ExecuteWithoutResultsAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Ok
        with
        | error ->
            RuntimeError (CypherQueryText cypher.Query.QueryText, error)
            |> Error

    let private dumpQuery { LogQuery = log } { Cypher = cypher } =
        let parameters =
            cypher.Query.QueryParameters
            |> Seq.map (fun kvPair -> sprintf "  - {%s} => %s" kvPair.Key (kvPair.Value |> Json.serialize))
            |> Seq.toList

        log (CypherQueryText cypher.Query.QueryText) (CypherQueryParameters parameters)

    let rec dump log client = function
        | Cypher.RawQuery query -> query |> dumpQuery log
        | Cypher.Composition composition -> client |> RawCypherQuery.start (OutputOnly log) |> composition |> dumpQuery log
        | Cypher.Multi multi -> multi |> List.iter (dump log client)

    let rec execute onEach client = (tee onEach) >> function
        | Cypher.RawQuery query -> query |> executeQuery
        | Cypher.Composition composition -> client |> RawCypherQuery.start Execute |> composition |> executeQuery
        | Cypher.Multi multi -> multi |> List.map (execute onEach client) |> Result.sequence <!> ignore

    let fromNode node =
        node
        |> CypherFluentQuery.addGraphNode
        |> RawCypherQuery.mapQuery
        |> Cypher.Composition

    let fromLink link =
        link
        |> CypherFluentQuery.addGraphLink
        |> RawCypherQuery.mapQuery
        |> Cypher.Composition

    let rec countQueries = function
        | Cypher.RawQuery _
        | Cypher.Composition _ -> 1
        | Cypher.Multi multi -> multi |> List.sumBy countQueries

type CypherQueryValue<'Value, 'Error> =
    | OkCypherQuery of ('Value -> Cypher)
    | ResultCypherQuery of ('Value -> Result<Cypher, 'Error>)

type CypherQuery<'Value, 'Error> = CypherQuery of CypherQueryValue<'Value, 'Error>

[<RequireQualifiedAccess>]
module CypherQuery =
    let ok toNode = CypherQuery (OkCypherQuery toNode)
    let result toNode = CypherQuery (ResultCypherQuery toNode)

    let inline private (>!>) fR f = // todo - use from Result.Operators
        fR >> Result.map f

    let fromNode toNode =
        CypherQuery (OkCypherQuery (toNode >> Cypher.fromNode))

    let fromNodeResult toNode =
        CypherQuery (ResultCypherQuery (toNode >!> Cypher.fromNode))

    let value (CypherQuery query) = query

    // todo - probably use this in every Component -> so most of them (maybe all of them) will return CypherQuery.result
    let toCypherResult (CypherQuery query) =
        match query with
        | OkCypherQuery toCypher -> toCypher >> Ok
        | ResultCypherQuery toResultCypher -> toResultCypher

    let private orFail = function   // todo - remove and use result everywhere?
        | Ok option -> option
        | Error e -> e |> failwith

    let toCypher query = toCypherResult query >> orFail
