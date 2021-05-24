# SharpSpades
A work-in-progress [Ace of Spades Classic](https://buildandshoot.com/) v0.75 server implementation

Inspired by [Piqueserver](https://github.com/piqueserver/piqueserver) and [Obsidian](https://github.com/ObsidianMC/Obsidian)

## Features
**Coming**
 - Plugins
 - v0.75 compatability
 - Protocol extension support
 - ...

## Contributing
Feel free to send a PR or create an issue if you find any problems!

If you want to work on something:
- Check that someone isn't already doing the same thing
- Create an issue to inform everyone that you're working on it

If you use an IDE that supports EditorConfig files, use the EditorConfig
in the repo root. If your editor doesn't support EditorConfigs, look 
at other files in the project for reference for code style etc.

### Things to work on
 - [ ] Physics
   - [ ] Players
     - [ ] Tests
   - [ ] Grenades
     - [ ] Tests
 - [ ] Map serialization

### Project structure
`SharpSpades` - The server itself, plugins will use this when implemented

`SharpSpades.Cli` - A CLI for starting (and managing?) servers

`SharpSpades.Tests` - Tests

`SharpSpades.Vxl` - A map library for AoS
