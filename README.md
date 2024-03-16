## NOTE: This project is currently not being developed
I don't have time to work on the project and there is a [new version of the protocol](https://github.com/SpadesX/aosprotocol) (come help if you want!) being
developed that will most likely cause huge breaking changes in the server API. I don't want to have to develop a piece of software that will have to be
rewritten a few maybe months later. Development/prototyping will continue with the new protocol when it is closed to being finished.

# SharpSpades
A work-in-progress [Ace of Spades Classic](https://buildandshoot.com/) v0.75 server implementation
inspired by [Piqueserver](https://github.com/piqueserver/piqueserver) and [Obsidian](https://github.com/ObsidianMC/Obsidian)

[Discord server](https://discord.gg/N5Vv3BX3MV)

[SharpSpades.Native](https://github.com/JStalnac/SharpSpades.Native) is the
native library for SharpSpades

## Features
**Coming**
 - Plugins
 - v0.75 compatability
 - Protocol extension support
 - ...

## Requirements
 - .NET 6.0

The server supports the following platforms:
 - Linux x86_64
 - Linux x86_64 (musl) * 
 - Linux ARM and ARM64 *
 - *Later there will also be builds for Windows...*

\* = *Not tested*

Windows users will have to run the server inside [WSL](https://docs.microsoft.com/en-us/windows/wsl/install)

## Installation
1. Download the sources and navigate to `SharpSpades.Cli` project
```sh
git clone https://github.com/JStalnac/SharpSpades
cd SharpSpades/SharpSpades.Cli
```

2. Download an AoS map file from a service like [aos.party](https://aos.party) and
place it in `SharpSpades/SharpSpades.Cli/classicgen.vxl`. This is the map used
by the server (will change in the future).

3. Start the server
```sh
dotnet run -c Release
```

## Contributing
Feel free to send a PR or create an issue if you find any problems!

If you want to work on something:
 - Check that someone isn't already doing the same thing
 - Create an issue or comment to inform everyone that you're working on it

If you use an IDE that supports EditorConfig files, use the EditorConfig
in the repo root. If your editor doesn't support EditorConfigs, look 
at other files in the project for reference for code style etc.

**Notes:**
 - The client can choose the player id it sends in a packet, prefer the
  `Client.Id` property in packet handlers

### Project structure
`SharpSpades` - The server itself

`SharpSpades.Cli` - A CLI for starting (and managing?) servers

`SharpSpades.Api` - The API part of the server, plugins will link to this in the future

`SharpSpades.Generators` - Source generators for the server

`SharpSpades.Tests` - Tests
