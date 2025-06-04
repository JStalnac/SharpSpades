// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System
open System.IO
open System.Runtime.ExceptionServices

module IO =
    type IOError =
        | FileNotFound
        | DirectoryNotFound
        | IOException of IOException

    let private rethrowKeepStacktrace ex =
        ExceptionDispatchInfo.Capture(ex).Throw()
        Unchecked.defaultof<_>

    let getDirectoryName (path : string) = Path.GetDirectoryName(path)

    let fileExists path = File.Exists(path)

    let readFile path =
        async {
            try
                let! contents =
                    File.ReadAllTextAsync(path)
                    |> Async.AwaitTask
                return Ok contents
            with
                | :? AggregateException as ae ->
                    match ae.InnerException with
                    | :? FileNotFoundException ->
                        return Error FileNotFound
                    | :? DirectoryNotFoundException ->
                        return Error DirectoryNotFound
                    | :? IOException as e ->
                        return Error (IOException e)
                    | ex ->
                        return rethrowKeepStacktrace ex
        }

    let readFileBytes path =
        async {
            try
                let! contents =
                    File.ReadAllBytesAsync(path)
                    |> Async.AwaitTask
                return Ok contents
            with
                // | :? AggregateException as ae ->
                //     match ae.InnerException with
                    | :? FileNotFoundException ->
                        return Error FileNotFound
                    | :? DirectoryNotFoundException ->
                        return Error DirectoryNotFound
                    | :? IOException as e ->
                        return Error (IOException e)
                    | ex ->
                        return rethrowKeepStacktrace ex
        }
