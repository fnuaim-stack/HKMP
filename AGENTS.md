# HKMP co-op development guidelines

This fork extends [Extremelyd1/HKMP](https://github.com/Extremelyd1/HKMP). Follow the methodology in [cranebeaker/coop-anything/AGENTS.md](https://github.com/cranebeaker/coop-anything/blob/main/AGENTS.md), choosing the **revive existing co-op mod** path. Those external guidelines are references, not permission to expose private data or assume games have been tested.

## Ground rules
- Keep the game's installation, saves, proprietary game assets, Unity assemblies and private credentials outside this GitHub repository.
- Record every fix with affected source files and whether it is source-reviewed, compiled, headless-tested or verified in two real game clients. Never call unexecuted scenarios passing.
- Work in an isolated test game installation with backed-up test saves; don't touch normal playthrough saves.
- Preserve LGPL-2.1 licensing and attribution to the original HKMP authors.
- Focus on scene-host handoff, dynamic entities, boss sync, world state, progress saves, reconnect and multiplayer robustness.
- Prefer targeted changes and regression tests. Do not claim a release is fully stable without a tested build and real-client validation.

## Repository commands
- Run game-independent packet regressions: `dotnet run --project tests/packet-smoke/PacketSmoke.csproj -c Release`
- Full game build (requires locally installed Hollow Knight/Modding API DLLs): `dotnet build HKMP.sln -c Release`
- See `BUILD_AND_RELEASE.md`, `GAMEPLAY_QA.md`, `BUGS.md`, and `RELEASE_PACKAGING.md`.
- Release packaging uses `scripts/package-release.ps1` and refuses to package missing runtime dependencies.
- Publish GitHub prereleases only from a **successful exact-commit full build** with both client and server artifacts, using `.github/workflows/release-candidate.yml`.

## Verified versus unknown
- The isolated production packet serialization smoke tests have passed in GitHub Actions.
- Full C# build is currently blocked in the fork's inherited CI by a missing encrypted-dependencies secret.
- Two-client gameplay, bosses, merchants, all progression and reconnect remain **unverified**; use the QA scenarios to establish behavior rather than guessing.
