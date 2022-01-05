namespace Lmc.Neo4j

open Lmc.ErrorHandling

[<RequireQualifiedAccess>]
type Cypher =
    | RawQuery of RawCypherQuery
    | Composition of (RawCypherQuery -> RawCypherQuery)
    | Multi of Cypher list

[<RequireQualifiedAccess>]
module Cypher =
    open AsyncResult.Operators

    type ExecuteEvents = {
        OnBeforeExecute: CypherRunableQuery -> unit
        OnAfterExecute: Result<unit, ExecuteCypherError> -> unit
    }

    [<RequireQualifiedAccess>]
    module ExecuteEvents =
        let ignore = {
            OnBeforeExecute = ignore
            OnAfterExecute = ignore
        }

    let private executeQuery { OnBeforeExecute = onBefore; OnAfterExecute = onAfter } { Cypher = cypher } =
        asyncResult {
            let! cypher = cypher <@> RawCypherQueryError

            cypher |> CypherRunableQuery.ofCypher |> onBefore

            return!
                cypher.ExecuteWithoutResultsAsync()
                |> AsyncResult.ofEmptyTaskCatch (fun e -> RuntimeError (CypherRunableQuery.ofCypher cypher, e))
        }
        |> Async.RunSynchronously
        |> tee onAfter

    let private dumpQuery { LogQuery = log } { Cypher = cypher } =
        cypher
        >>* (CypherRunableQuery.ofCypher >> log)
        <!> ignore

    let rec dump log client = function
        | Cypher.RawQuery query -> query |> dumpQuery log <@> List.singleton
        | Cypher.Composition composition -> client |> RawCypherQuery.start (OutputOnly log) |> composition |> dumpQuery log <@> List.singleton
        | Cypher.Multi multi -> multi |> List.map (dump log client) |> AsyncResult.sequenceA <!> ignore

    let rec execute client events = function
        | Cypher.RawQuery query -> query |> executeQuery events |> Validation.ofResult
        | Cypher.Composition composition -> client |> RawCypherQuery.start Execute |> composition |> executeQuery events |> Validation.ofResult
        | Cypher.Multi multi ->
            multi
            |> List.map (execute client events)
            |> Validation.sequence
            |> Result.map ignore

    let fromNode node =
        node
        |> RawCypherQuery.addGraphNode
        |> Cypher.Composition

    let fromLink link =
        link
        |> RawCypherQuery.addGraphLink
        |> Cypher.Composition

    let rec countQueries = function
        | Cypher.RawQuery _
        | Cypher.Composition _ -> 1
        | Cypher.Multi multi -> multi |> List.sumBy countQueries

type CypherQuery<'Value, 'Error> = CypherQuery of ('Value -> Result<Cypher, 'Error>)

[<RequireQualifiedAccess>]
module CypherQuery =
    open Lmc.ErrorHandling.Result.Operators

    let ok toNode = CypherQuery (toNode >> Ok)
    let result toNode = CypherQuery toNode

    let fromNode toNode =
        ok (toNode >> Cypher.fromNode)

    let fromNodeResult toNode =
        result (toNode >!> Cypher.fromNode)

    let toCypher (CypherQuery query) = query
    let toCypherValue value (CypherQuery query) = query value
