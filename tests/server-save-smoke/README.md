# Dedicated-server save recovery regression

This console test compiles the **real** `ConsoleServerManager.cs` and `ConsoleSaveFile.cs` with lightweight fake game types and a simplified save-data codec. It exercises disk operations in an isolated test build directory; it does not read or write any Hollow Knight saves.

Scenarios: initial save creation, atomic backup, restoring player/global progress after restart, preserving save-object references, rejecting a structurally incomplete primary file, backup recovery with corruption quarantine, and refusing startup if both files are unreadable.

Run with `dotnet run --project tests/server-save-smoke/ServerSaveSmoke.csproj -c Release` from a clean test build output.

**Scope:** .NET 8 with Newtonsoft.Json 13 on Windows/Linux. HKMP itself targets .NET Framework 4.7.2 and uses game-specific save codecs. These tests do not prove Mono behavior, game save decoding, a full HKMP compile, or two-player gameplay.
