namespace Alma.Neo4j

[<AutoOpen>]
module internal Neo4jUtils =

    let tee f a =
        f a
        a

    [<RequireQualifiedAccess>]
    module String =
        let toLower (value: string) =
            value.ToLower()

        let ucFirst (value: string) =
            match value |> Seq.toList with
            | [] -> ""
            | first :: rest -> (string first).ToUpper() :: (rest |> List.map string) |> String.concat ""

        let split (separator: string) (value: string) =
            value.Split(separator) |> Seq.toList

        let replaceAll (replace: string list) replacement (value: string) =
            replace
            |> List.fold (fun (value: string) toRemove ->
                value.Replace(toRemove, replacement)
            ) value

    [<RequireQualifiedAccess>]
    module internal Map =
        /// Merge new values with the current values (replacing already defined values).
        let merge currentValues newValues =
            currentValues
            |> Map.fold (fun merged name connection ->
                if merged |> Map.containsKey name then merged
                else merged.Add(name, connection)
            ) newValues

    [<RequireQualifiedAccess>]
    module Option =
        module Operators =
            let (=>) key value = (key, value)

    [<RequireQualifiedAccess>]
    module Json =
        open Newtonsoft.Json

        let serialize obj =
            JsonConvert.SerializeObject obj

        let serializePretty obj =
            JsonConvert.SerializeObject(obj, Formatting.Indented)
