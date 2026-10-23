# Build and release HKMP gameplay fork

**Development instructions — not yet a verified release.** The upstream project depends on copyrighted Hollow Knight/Unity/Modding API binaries that must be obtained from a game installation and compatible modding setup. Do not commit or upload those DLLs to this public repository.

## Local build (Windows)

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

Only publish a GitHub **prerelease** after both client and server builds and the co-op scenarios pass. Provide the client and standalone-server binaries, compatible game/API version, mod dependencies, exact instructions, changelog, known limitations and SHA-256 artifact checksums. Keep original HKMP attribution and licensing.

Do not call a release stable until progression, combat, reconnects and save restoration work in actual gameplay.
