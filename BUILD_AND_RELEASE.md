# Build and release HKMP gameplay fork

**Experimental prerelease:** [v3.0.0-coop-rc1](https://github.com/fnuaim-stack/HKMP/releases/tag/v3.0.0-coop-rc1) contains client and server binaries from a successful local build. Two-client gameplay remains unverified. The upstream project depends on copyrighted Hollow Knight/Unity/Modding API binaries that must be obtained from a game installation and compatible modding setup. Do not commit or upload those DLLs to this public repository.

## Local build (Windows)

For the supplied Hollow Knight 1.5.78.11833 Managed reference set (ZIP SHA-256 `1DC53E91BFCCF69C29E6751396DF29C6B153D3E8749E3089183B64C33F047D95`), `HKMP/HKMP.csproj` now references the game's `netstandard.dll` 2.1 facade. `HKMP/Util/ImageConversionUtil.cs` selects Unity's byte-array `LoadImage` overload without asking the .NET Framework 4.7.2 compiler to resolve Unity's `ReadOnlySpan` overload. The two image call sites are `HKMP/Game/Client/Skin/SkinLoader.cs` and `HKMP/Ui/Resources/TextureManager.cs`.

On a Windows runner with .NET SDK 9.0.319, the full client and server solution compiled with 0 errors and 33 Unity API deprecation warnings. The packet, chunk transport, save command, server save, and source syntax smoke programs passed. The image loader and multiplayer gameplay have not been run inside Hollow Knight; this remains a prerelease candidate.

1. Install the .NET SDK that supports building .NET Framework 4.7.2 projects. For native Windows, install the .NET Framework 4.7.2 developer/targeting pack if the SDK requests it.
2. Install a compatible Hollow Knight build and Hollow Knight Modding API. Ensure the required game assemblies and Modding API DLLs exist under the game's `hollow_knight_Data/Managed` directory or its modding folders.
3. Copy or reference the DLL dependencies from the game installation in private local directories. `HKMP/LocalBuildProperties_example.props` and `HKMPServer/LocalBuildProperties_example.props` document the default `lib` folder and `References` property. Include `Newtonsoft.Json.dll`, MonoMod/Hook libraries, UnityEngine modules, PlayMaker and game assemblies as referenced by `HKMP.csproj`.
4. Create `HKMP/LocalBuildProperties.props` and `HKMPServer/LocalBuildProperties.props` if you want to override `References` and output directories. These private configuration files should point to your installed libraries.
5. Run:

   ```powershell
   dotnet restore HKMP.sln
   dotnet build HKMP.sln -c Release --no-restore
   dotnet run --project tests/packet-smoke/PacketSmoke.csproj -c Release
   ```

The smoke tests compile only the production chunk serialization source and are **not** a substitute for the full solution build or a playtest.

## CI limitation

The inherited `.github/workflows/dotnet.yml` downloads an encrypted proprietary dependency archive requiring the `DEPENDENCIES_ZIP_PASSPHRASE` repository secret. The fork does not currently have that secret, so the full-build workflow fails before compilation. Do **not** treat that as evidence that C# compilation succeeded or failed. The new independent `gameplay-smoke.yml` needs no such secret.

An alternate, self-managed full-build runner can use locally installed Hollow Knight dependencies, provided they are licensed and aren't exposed publicly.

## Gameplay verification

- Install the compiled mod into **isolated test** Hollow Knight installations; don't overwrite your normal saves.
- Enable **Full Synchronisation** explicitly in server settings: it is disabled by default in the upstream configuration.
- Host, connect a second client, confirm matching areas and distinct save identities, then run all scenarios in `GAMEPLAY_QA.md`.
- Test Mantis Claw and another major ability, NPC merchants and charm repairs, two boss arenas, scene-host exit/rejoin, late joins, save/restart and rollback after corrupt test saves.
- Inspect client and server logs for the first divergence. Record the game build, mod commit and platform for each test.
- Run a dedicated server with the same full-synchronisation settings; verify `save.json` and `save.json.bak` preservation after save updates.

## Shipping gate

Publish only a GitHub **prerelease** after both client and server builds pass. State which co-op scenarios remain untested. Provide the client and standalone-server binaries, compatible game/API version, mod dependencies, exact instructions, changelog, known limitations and SHA-256 artifact checksums. Keep original HKMP attribution and licensing.

Do not call a release stable until progression, combat, reconnects and save restoration work in actual gameplay.

## Version compatibility and upstream lineage

This fork starts from [Extremelyd1/HKMP](https://github.com/Extremelyd1/HKMP) development master, **not** from its published v2.4.3 binary release. As of the 2026-10-23 comparison, that upstream master had 225 further commits, including experimental full-synchronisation logic. The fork preserved the original packet-ID definitions and .NET Framework 4.7.2 targets.

**Compatible target:** Hollow Knight **1.5.78.11833** with the matching Modding API, PlayMaker and Unity managed assemblies. The client and standalone server must come from the **same fork build**. Do not mix the published v2.4.3 client with this fork's server. Newer game installations require a separate compatibility check.

Source-reviewed changes include scene-host authority and deferred entity updates, per-client packet framing, matching add-on connection handlers, reliable chunk/ACK state, safe reconnects, dedicated-save recovery, and copy-save persistence. Isolated tests cover portions of these; game scenes, bosses, merchants and real saves remain unverified (see [GAMEPLAY_QA.md](GAMEPLAY_QA.md)).

## Packaging and runtime dependencies

Use the repository's package-release.ps1 script to create matching client and server ZIPs and SHA-256 checksums from binaries compiled at the **same commit**:

    ./scripts/package-release.ps1 -ClientDir "<compiled HKMP directory>" -ServerDir "<compiled HKMPServer directory>" -DependencyDir "<runtime dependency directory>" -OutputDir ./dist -SourceCommit "<40-character commit SHA>" -Version "v3.0.0-coop-rc1"

The client needs HKMP.dll and BouncyCastle.Cryptography.dll. The dedicated server needs HKMPServer.exe, HKMPServer.exe.config, the matching HKMP.dll, BouncyCastle.Cryptography.dll and Newtonsoft.Json.dll. Preserve the main LGPL LICENSE and all third-party license notices. **Do not publish proprietary game/Unity/Modding API reference DLLs.**

The optional release-candidate.yml workflow publishes prereleases only with a successful **push** full-build run for the exact commit; the privately built v3.0.0-coop-rc1 prerelease did not use that workflow because inherited CI lacks the dependency-decryption secret. Synthetic packaging smoke checks are not a real build or gameplay test.
