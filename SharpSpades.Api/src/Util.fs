module SharpSpades.Util

/// <summary>
/// Convert a list of results into a result of <see cref="Ok" />s or
/// <see cref="Error" />s.
/// </summary>
let resultBindAll (results : Result<'a, 'b> list) : Result<'a list, 'b list> =
    (Ok [], results)
    ||> List.fold (fun state res ->
        match state, res with
        | Ok xs, Ok x -> Ok (x :: xs)
        | Ok _, Error err -> Error [ err ]
        | Error _, Ok _ -> state
        | Error errs, Error err -> Error (err :: errs))
