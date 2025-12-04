# Artificial Rimtelligence Core

Core module for the Artificial Rimtelligence mod suite for RimWorld.

## Building

1. Ensure you have the .NET SDK installed (targeting .NET Framework 4.8)
2. Run `dotnet build Source/ArtificialRimtelligenceCore.csproj`
3. The compiled assembly will be output to the `Assemblies/` folder

Or use VS Code tasks:
- `Ctrl+Shift+B` to build (Release)
- Run "Build and Run RimWorld" task to build and launch the game

## Project Structure

```
Artificial Rimtelligence Core/
├── About/              # Mod metadata (About.xml)
├── Assemblies/         # Compiled DLLs (output)
├── Defs/               # XML definitions
├── Languages/          # Localization files
├── Patches/            # XML patches
├── Source/             # C# source code
├── Textures/           # Image assets
└── README.md
```

## Dependencies

- RimWorld 1.6
- Uses Krafs.Rimworld.Ref NuGet package for game references

## License

[Add your license here]
