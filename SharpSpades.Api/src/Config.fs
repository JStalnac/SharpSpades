// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Configuration

type Section = Map<string, Value>
and Value =
    | Section of Section
    | String of string
    | Integer of int64
    | Bool of bool
    | Double of double
    | TimeSpan of System.TimeSpan
    | DateTime of System.DateTime
    | DateTimeOffset of System.DateTimeOffset
    | List of Value list

module Config =
    let hasKey key (config : Section) =
        Map.containsKey key config

    let getKey key (config : Section) =
        Map.tryFind key config

    let mapKey f key config =
        match getKey key config with
        | Some value ->
            match f value with
            | Ok x -> Ok (Some x)
            | Error err -> Error err
        | None -> Ok None

    let getSection key config =
        (key, config)
        ||> mapKey (function
            | Section section -> Ok section
            | other -> Error (sprintf "key '%s' must be a section" key))

    let getString key config =
        (key, config)
        ||> mapKey (function
            | String str -> Ok str
            | other -> Error (sprintf "key '%s' must be a string" key))

    let getInteger key config =
        (key, config)
        ||> mapKey (function
            | Integer i -> Ok i
            | other -> Error (sprintf "key '%s' must be an integer" key))

    let getList key config =
        (key, config)
        ||> mapKey (function
            | List xs -> Ok xs
            | other -> Error (sprintf "key '%s' must be a list" key))

    let castList f (list : Value list) =
        (Ok [], list)
        ||> List.fold (fun state item ->
            state |> Result.bind (fun items ->
                f item |> Result.map (fun item -> item :: items)))
