namespace Lmc.Neo4j

type CypherQueryText = CypherQueryText of string

[<RequireQualifiedAccess>]
module CypherQueryText =
    let value (CypherQueryText queryText) = queryText

type CypherQueryParameters = CypherQueryParameters of (string * string) list

[<RequireQualifiedAccess>]
module CypherQueryParameters =
    let value (CypherQueryParameters parameters) = parameters

type CypherRunableQuery = CypherQueryText * CypherQueryParameters

[<RequireQualifiedAccess>]
module CypherRunableQuery =
    let internal ofCypher (cypher: Neo4jClient.Cypher.ICypherFluentQuery): CypherRunableQuery =
        let parameters =
            cypher.Query.QueryParameters
            |> Seq.map (fun kvPair -> kvPair.Key, (kvPair.Value |> Json.serialize))
            |> Seq.toList

        CypherQueryText cypher.Query.QueryText, CypherQueryParameters parameters

    let dump (CypherQueryText query, CypherQueryParameters parameters) =
        sprintf "%s\nParameters:\n%s"
            query
            (
                match parameters with
                | [] -> "No Parameters"
                | parameters -> parameters |> List.map (fun (k, v) -> sprintf "  - $%s => %s" k v) |> String.concat "\n"
            )

    let format (CypherQueryText query, CypherQueryParameters parameters) =
        sprintf "%s\n%s"
            (
                // in order to run this in the Neo4j browser, it has to be a single line "command", which runs BEFORE a query
                parameters |> List.map (fun (k, v) -> sprintf "%s: %s" k v) |> String.concat ", " |> sprintf ":params { %s }"
            )
            query

//
// Errors
//

[<RequireQualifiedAccess>]
type AssertNodeError =
    | RuntimeError of exn
    | NodeNotExists of CypherRunableQuery

type RawCypherQueryError =
    | AssertNodeError of AssertNodeError

[<RequireQualifiedAccess>]
module RawCypherQueryError =
    let format = function
        | AssertNodeError (AssertNodeError.RuntimeError e) -> sprintf "Asserting that node exists failed with\n%A" e
        | AssertNodeError (AssertNodeError.NodeNotExists cypherRunableQuery) -> sprintf "Node not found by cypher:\n%s" (cypherRunableQuery |> CypherRunableQuery.format)

type ExecuteCypherError =
    | RawCypherQueryError of RawCypherQueryError
    | RuntimeError of CypherRunableQuery * exn

[<RequireQualifiedAccess>]
module ExecuteCypherError =
    let format = function
        | RawCypherQueryError error ->
            error
            |> RawCypherQueryError.format
            |> sprintf "Query could not start, because it has an internal error.\n%s"

        | RuntimeError (cypherRunableQuery, error) ->
            sprintf "Query:\n%s\n\nEnds with error:\n%A\n" (cypherRunableQuery |> CypherRunableQuery.format) error
