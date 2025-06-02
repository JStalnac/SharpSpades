// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open SharpSpades.Net
open SharpSpades.World

/// Messages sent to supervisor by worlds
type SupervisorMessage =
    | WorldStarting of WorldId
    | WorldStopping of WorldId
    | WorldStopped of WorldId
    | SendPacket of WorldId * ClientId * PacketFlags * Packet

/// Messages sent to worlds by supervisor
type WorldMessage =
    | Stop
    | TransferClient of ClientId
    | PacketReceived of ClientId * Packet
