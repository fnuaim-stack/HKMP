# Automated checks

These five isolated .NET 8 runners use real production source files with lightweight substitutes where game libraries are needed. Run from the repository root.

| Test | Run | Covers |
|---|---|---|
| Packet encoding | dotnet run --project tests/packet-smoke/PacketSmoke.csproj -c Release | Upstream wire-format golden vectors, 1–256 slices, invalid data and ACK padding |
| Chunk transport | dotnet run --project tests/chunk-transport/ChunkTransport.csproj -c Release | Real sender/receiver flow, ACK state, consecutive chunks, foreign slices and rapid start/stop |
| Copy-save | dotnet run --project tests/save-command-smoke/SaveCommandSmoke.csproj -c Release | Real /copysave command, independent copies, persistence rejection and rollback |
| Server save recovery | dotnet run --project tests/server-save-smoke/ServerSaveSmoke.csproj -c Release | Real ConsoleServerManager/ConsoleSaveFile I/O, backups, quarantine, double corruption with simplified game-data fixtures (Windows/Linux) |
| C# syntax | dotnet run --project tests/source-syntax/SourceSyntax.csproj -c Release -- . | Roslyn parsing of every C# source file in HKMP and HKMPServer |

**Limits:** These are not full Hollow Knight gameplay tests; isolated harnesses use .NET 8 and stubbed game-facing types, whereas the mod targets .NET Framework 4.7.2. Syntax parsing does not validate type references or Unity/PlayMaker hooks.

See [build and packaging](../BUILD_AND_RELEASE.md) and [gameplay QA](../GAMEPLAY_QA.md). The prerelease compiled on Windows against the owner's Hollow Knight 1.5.78.11833 files, but live two-client/boss/progression checks remain unverified. Do not commit copyrighted game assemblies or saves.
