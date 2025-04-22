# SharpSpades
A work-in-progress [Ace of Spades Classic](https://buildandshoot.com/) v0.75
server implementation inspired by
[Piqueserver](https://github.com/piqueserver/piqueserver) and
[Obsidian](https://github.com/ObsidianMC/Obsidian).

## Features
**Coming**

 - Plugins
 - v0.75 compatability
 - Protocol extension support
 - ...

## Requirements
SharpSpades requires the following packages for compiling:

- .NET SDK 8.0
- A C11 compiler
- [xmake](https://xmake.io/)
- [enet6](https://github.com/SirLynix/enet6) (installed automatically)

At runtime only a .NET 8.0 runtime is needed.

## Contributing
Feel free to send a PR or create an issue if you find any problems!

If you want to work on something:

 - Check that someone isn't already doing the same thing
 - Create an issue or comment to inform everyone that you're working on it

If you use an IDE that supports EditorConfig files, use the EditorConfig in the
repo root. If your editor doesn't support EditorConfigs, look at other files in
the project for reference for code style etc.

If you are making a fork of the project, please list the changes that you make
in the file `CHANGES.txt`, including any relevant dates. It makes it easier for
users of your fork to understand what it does and how it differs from the
original version. Stating changes when distributing a modified version of the
project is also a requirement of both the GNU GPLv3 (section 5) and EUPL v1.2
(section 5).

### Project structure
SharpSpades is divided into several projects. Each project has a `<project>/src`
directory and separate tests in `<project>/tests`.

`SharpSpades` - The API of the server

`SharpSpades.Server` - Server implementation

`SharpSpades.Native` - Native library wrapper code (C#)

`SharpSpades.Testing` - Test framework

The native code lives under `native/`. It is built using xmake. To compile, run
`xmake` in the native code directory.

## License
The native code is licensed only under GNU General Public License version 3.0
(or any later version) and as it is linked with and runs in the same process as
the rest of SharpSpades, SharpSpades is licensed under GPLv3 or any later
version. If, however, you wish to use SharpSpades without the native code
library, you may do so under the terms of either the GNU General Public License,
version 3.0 or any later version, or the European Union Public License, version
1.2.

Because plugins are linked with the SharpSpades program, they must also be
distributed under the GNU General Public License version 3.0 or any later
version.
