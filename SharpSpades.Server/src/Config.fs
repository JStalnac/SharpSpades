// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open Tomlet
open Tomlet.Models
open Tomlet.Exceptions
open SharpSpades.Configuration

module Config =
    let rec configOfTomlTable (table : TomlTable) : Section =
        let rec valueOfToml (v : TomlValue) =
            match v with
            | :? TomlString as s -> String s.Value
            | :? TomlLong as i -> Integer i.Value
            | :? TomlBoolean as b -> Bool b.Value
            | :? TomlDouble as d -> Double d.Value
            | :? TomlLocalTime as t -> TimeSpan t.Value
            | :? TomlLocalDate as d -> DateTime d.Value
            | :? TomlLocalDateTime as d -> DateTime d.Value
            | :? TomlOffsetDateTime as d -> DateTimeOffset d.Value
            | :? TomlArray as arr ->
                List <| [
                    for v in arr do
                        v |> valueOfToml
                ]
            | :? TomlTable as t -> Section <| configOfTomlTable t
            | x -> failwithf "Unknown TOML value %O" (x.GetType())

        Map [
            for kvp in table do
                kvp.Key, kvp.Value |> valueOfToml
        ]

    let load toml =
        try 
            TomlParser().Parse(toml)
            |> configOfTomlTable
            |> Ok
        with
            :? TomlException as e ->
                Error (sprintf "%s: %s" (e.GetType().Name) e.Message)
