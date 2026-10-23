# Source syntax gate

Run `dotnet run --project tests/source-syntax/SourceSyntax.csproj -c Release -- .`.

This test parses **every C# file** under `HKMP/` and `HKMPServer/` with Roslyn C# Preview syntax and exits nonzero for parser diagnostics. It does not require a copy of Hollow Knight or its proprietary build DLLs.

**Limitations:** Roslyn parsing is not the same as compiling/linking. It cannot detect missing game references, unknown types, invalid member references, Unity/PlayMaker API mismatches, or actual gameplay bugs. Those require the licensed game DLLs and running the mod with isolated client saves.
