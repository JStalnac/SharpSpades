## Position Data

This packet is used to set the players position.

|-----------:|----------|
| Packet ID:  |  0       |
| Total Size: | 13 bytes |

#### Fields

| Field Name|Field Type|Example|Notes|
|----------:|----------|-------|-----|
|  position | Vector3f |  `0`  |     |

## Orientation Data
This packet is used to set the players orientation.

|------------:|----------|
| Packet ID   |  1       |
| Total Size: | 13 bytes |

#### Fields

| Field Name|Field Type|Example|Notes|
|-------------:|----------|-------|-----|
|  orientation | Vector3f |  `0`  |     |


## Input Data
Contains the key-states of a player, packed into a byte.

| ----------- | -------- |
| Packet ID   | 3        |
| Total Size: | 3 bytes  |

#### Fields

| Field Name  | Field Type | Example | Notes                                                                 |
|-------------|------------|---------|-----------------------------------------------------------------------|
| player ID   | UByte      | `0`     |                                                                       |
| input state | UByte      | `0`     | Each bit in the byte represents a key, as defined in the table below. |SharpSpades.Api.InputState

## Hit Packet

Sent by the client when a hit is registered. The server should verify that this
is possible to prevent abuse (such as hitting without shooting, facing the
wrong way, etc).


| -----------:| ------- |
| Packet ID   | 5       |
| Total Size: | 3 bytes |

#### Fields

| Field Name    | Field Type | Example | Notes                     |
|---------------|------------|---------|---------------------------|
| target        | UByte      | `0`     |                           |
| hit type      | UByte      | `0`     | See values in table below |SharpSpades.Api.HitType

## Set HP

Sent to the client when hurt.


| -----------:| -------- |
| Packet ID   | 5        |
| Total Size: | 15 bytes |

#### Fields

| Field Name        | Field Type | Example | Notes                |
|-------------------|------------|---------|----------------------|
| health            | UByte      | `0`     |                      |
| type              | UByte      | `0`     | 0 = fall, 1 = weapon |SharpSpades.Api.DamageType
| source            | Vector3f   | `0`     |                      |

## Grenade Packet
Spawns a grenade with the given information.

| ------------:| --------- |
| Packet ID    | 6         |
| Total Size:  | 30 bytes  |

#### Fields

| Field Name  | Field Type | Example | Notes |
|-------------|------------|---------|-------|
| player ID   | UByte      | `0`     |       |
| fuse length | LE Float   | `0`     |       |
| position    | Vector3f   | `0`     |       |
| velocity    | Vector3f   | `0`     |       |

## Set Tool
Sets a player's currently equipped tool/weapon.


|------------:|---------|
| Packet ID   | 7       |
| Total Size: | 3 bytes |

#### Fields

| Field Name | Field Type | Example | Notes                        |
|------------|------------|---------|------------------------------|
| player ID  | UByte      | `0`     |                              |
| tool       | UByte      | `0`     | Tool values are listed below |SharpSpades.Api.Tool

## Set Color
Set the color of a player's held block.

|------------:|---------|
| Packet ID   | 8       |
| Total Size: | 5 bytes |

#### Fields

| Field Name | Field Type | Example | Notes |
|------------|------------|---------|-------|
| player ID  | UByte      | `0`     |       |
| color      | Color      | `0`     |       |

## Move Object
This packet is used to move various game objects like tents, intels and even grenades. When moving grenades in TC mode the voxlap client has a bug that changes grenades' models to small tents.

| ----------: | -------- |
| Packet ID   | 11       |
| Total Size: | 15 bytes |

#### Fields

| Field Name | Field Type | Example | Notes       |
|------------|------------|---------|-------------|
| object id  | UByte      | `0`     |             |
| team       | UByte      | `0`     | 2 = neutral |
| position   | Vector3f   | `0`     |             |

## Block Action
Sent when a block is placed/destroyed.

| ----------: | -------- |
| Packet ID   | 13       |
| Total Size: | 15 bytes |

#### Fields

| Field Name  | Field Type | Example  | Notes           |
|-------------|------------|----------|-----------------|
| player id   | UByte      | `0`      |                 |
| action type | UByte      | `0`      | See table below |
| x           | LE UInt    | `0`      |                 |
| y           | LE UInt    | `0`      |                 |
| z           | LE UInt    | `0`      |                 |

## Block Line
Create a line of blocks between 2 points. The block color is defined by the `Set Color` packet. 

| ----------: | -------- |
| Packet ID   | 14       |
| Total Size: | 26 bytes |

#### Fields

| Field Name  | Field Type       | Example | Notes |
| ----------- | ---------------- | ------- | ----- |
| player id   | UByte            | `0`     |       |
| start x     | LE UInt          | `0`     |       |
| start y     | LE UInt          | `0`     |       |
| start z     | LE UInt          | `0`     |       |
| end x       | LE UInt          | `0`     |       |
| end y       | LE UInt          | `0`     |       |
| end z       | LE UInt          | `0`     |       |

## Kill Action

Notify the client of a player's death.

| ----------: | -------- |
| Packet ID   | 16       |
| Total Size: | 5 bytes  |

#### Fields

| Field Name       | Field Type | Example | Notes                 |
|------------------|------------|---------|-----------------------|
| player ID        | UByte      | 12      | Player that died      |
| killer ID        | UByte      | 8       |                       |
| kill type        | UByte      | 0       | See table below       |SharpSpades.Api.KillType
| respawn time     | UByte      | 1       | Seconds until respawn |

## Map Start

Sent when a client connects, or a map is advanced for already existing connections.

Should be the first packet received when a client connects.

| ----------: | -------- |
| Packet ID   | 18       |
| Total Size: | 5 bytes  |

| Field Name | Field Type | Example | Notes |
|------------|------------|---------|-------|
| Map size   | LE UInt    | `4567`  |       |

## Player Left

Sent when a player disconnects.


| ----------: | -------- |
| Packet ID   | 20       |
| Total Size: | 2 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes |
|------------|------------|---------|-------|
| player ID  | UByte      | `0`     |       |

## Territory Capture

Sent when a player captures a Command Post in Territory Control mode.

Captures have affects on the client.

| ----------: | -------- |
| Packet ID   | 21       |
| Total Size: | 5 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes                           |
|------------|------------|---------|---------------------------------|
| player ID  | UByte      | `0`     |                                 |
| entity ID  | UByte      | `0`     | The ID of the CP being captured |
| winning    | UByte      | `0`     | (or losing)                     |
| state      | UByte      | `0`     | team id                         |

## Progress Bar

Display the TC progress bar.


| ----------: | -------- |
| Packet ID   | 22       |
| Total Size: | 8 bytes  |

#### Fields

| Field Name        | Field Type | Example | Notes                                                                                            |
|-------------------|------------|---------|--------------------------------------------------------------------------------------------------|
| entity ID         | UByte      | `0`     | The ID of the tent entity                                                                     |
| capturing team    | UByte      | `1`     |                                                                                                  |SharpSpades.Api.TeamType
| rate              | Byte       | `2`     | Used by the client for interpolation, one per team member capturing (minus enemy team members). One rate unit is 5% of progress per second. |
| progress          | LE Float   | `0.5`   | In range [0,1]                                                                                   |

## Intel Capture

Sent when a player captures the intel, which is determined by the server.

Winning captures have affects on the client.

| ----------: | -------- |
| Packet ID   | 23       |
| Total Size: | 3 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes                   |
|------------|------------|---------|-------------------------|
| player ID  | UByte      | `0`     |                         |
| winning    | UByte      | `0`     | Was the winning capture |

## Intel Pickup

Sent when a player collects the intel, which is determined by the server.

| ----------: | -------- |
| Packet ID   | 24       |
| Total Size: | 2 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes |
|------------|------------|---------|-------|
| player ID  | UByte      | `0`     |       |

## Intel Drop

Sent when a player dropped the intel. This will update the intel position on the client.


| ----------: | -------- |
| Packet ID   | 25       |
| Total Size: | 14 bytes |

#### Fields

| Field Name | Field Type | Example | Notes                              |
| player ID  | UByte      | `0`     | ID of the player who dropped intel |
| position   | Vector3f   | `32.0`  |                                    |

## Restock

Id of the player who has been restocked.

| ----------: | -------- |
| Packet ID   | 26       |
| Total Size: | 2 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes                          |
|------------|------------|---------|--------------------------------|
| player ID  | UByte      | `0`     | ID of the player who restocked |

## Fog color

Set the colour of a player's fog.

| ----------: | -------- |
| Packet ID   | 27       |
| Total Size: | 5 bytes  |

#### Fields

| Field Name | Field Type | Example      | Notes        |
| ---------- | ---------- | ------------ | ------------ |
| color  | LE UInt    | `0h00fefefe` | BGRA encoded |

## Weapon Reload

Sent by the client when the player reloads their weapon, and relayed to other
clients after protocol logic applied.

This has no affect on animation, but is used to trigger sound effects on the
other clients.

| ----------: | -------- |
| Packet ID   | 28       |
| Total Size: | 4 bytes |

#### Fields

| Field Name   | Field Type | Example | Notes               |
|--------------|------------|---------|---------------------|
| player ID    | UByte      | `0`     | Player who reloaded |
| clip ammo    | UByte      | `0`     |                     |
| reserve ammo | UByte      | `0`     |                     |

## Change Team

Sent by the client when the player changes team. Is not relayed to all clients
directly, but instead uses **Kill Action**
then **Create Player** to inform other
clients of the team change.

| ----------: | -------- |
| Packet ID   | 29       |
| Total Size: | 3 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes                     |
|------------|------------|---------|---------------------------|
| player ID  | UByte      | `0`     | Player who changed team   |
| team       | Byte       | `0`     | See values in table below |SharpSpades.Api.TeamType

## Change Weapon

Sent by the client when player changes weapon, and relayed to clients by server
after `filter_visibility` logic is applied.

Receiving clients will also be sent a preceding
**Kill Action** to inform them the player
has died both of which are sent as reliable packets.


| ----------: | -------- |
| Packet ID   | 30       |
| Total Size: | 3 bytes  |

#### Fields

| Field Name | Field Type | Example | Notes                       |
|------------|------------|---------|-----------------------------|
| player ID  | UByte      | `0`     | Player who's changed weapon |
| weapon     | UByte      | `0`     | See values in table below   |SharpSpades.Api.WeaponType

## Existing Player
Set player's team, weapon, etc.

|------------:|---------|
| Packet ID   | 9       |
| Total Size: | 28      |

#### Fields

| Field Name | Field Type  | Example | Notes |
|------------|-------------|---------|-------|
| player ID  | UByte       | `0`     |       |
| team       | Byte        | `0`     |       |SharpSpades.Api.TeamType
| weapon     | UByte       | `0`     |       |SharpSpades.Api.WeaponType
| held item  | UByte       | `0`     |       |
| kills      | LE UInt     | `0`     |       |
| color      | Color       | `0`     |       |
| name       | String      | `Deuce` |       |

## Create Player
Send on respawn of a player.

| ----------: | ------- |
| Packet ID   | 12      |
| Total Size: | 32      |

#### Fields

| Field Name | Field Type  | Example | Notes |
|------------|-------------|---------|-------|
| player id  | UByte       | `0`     |       |
| weapon     | UByte       | `0`     |       |SharpSpades.Api.WeaponType
| team       | Byte        | `0`     |       |SharpSpades.Api.TeamType
| position   | Vector3f    | `0`     |       |
| name       | String      | `Deuce` |       |

