namespace SharpSpades.Server

open SharpSpades

/// Messages sent to supervisor by worlds
type SupervisorMessage =
    | WorldStarting of WorldId
    | WorldStopping of WorldId
    | WorldStopped of WorldId

/// Messages sent to worlds by supervisor
type WorldMessage =
    | Stop
