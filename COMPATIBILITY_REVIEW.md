# HKMP source and compatibility review

**Compared:** upstream [Extremelyd1/HKMP](https://github.com/Extremelyd1/HKMP) at `f7c35d96f84c3d7b671fc4283795a07b4df97227`, original published [v2.4.3](https://github.com/Extremelyd1/HKMP/releases/tag/v2.4.3), and this fork. Review date: 2026-10-23.

## Lineage and compatibility boundaries

- Original HKMP **v2.4.3 is not the source baseline for this fork**. Upstream master contains **225 commits** after the v2.4.3 tag, including much of the experimental full-synchronisation game/scene/save feature set.
- The fork is based on that upstream master, with targeted patches to 16 production C# files at the time of the review. No original project file (`HKMP.csproj`, `HKMPServer.csproj`), embedded game resource, sprite, Unity hook assembly reference, or packet ID enum was changed in the fork comparison.
- Both build projects target `net472`. `HKMP.csproj` references Hollow Knight `Assembly-CSharp`, Modding API/MonoMod hooks, PlayMaker, UnityEngine assemblies, Newtonsoft.Json and BouncyCastle. The fork did not remove those dependencies.
- Use **Hollow Knight 1.5.78.11833** with the matching installed Hollow Knight Modding API. As of the review, [Lumafly issue #239](https://github.com/TheMulhima/Lumafly/issues/239) says game patch `1.5.12459` cannot be used with the normal modding API. This is an upstream toolchain limitation, not a source fix in this fork.
- **Do not mix fork client and server versions, and do not assume a v2.4.3 client works with a 3.0.0-development server.** The full-synchronisation and addon negotiation feature set has changed in upstream master.

## Audited patch categories

| Areas | Fork change relative to upstream master | Verification |
|---|---|---|
| Entity tracking and scene host | Bounded unresolved-update queue, replay on spawned entity, authority filtering for simulated enemy movement | Source inspected; no real boss/scene tests |
| Reliable update routing | Scene-host-only Active and HostFsm; generic interaction Data remains forwarded | Source inspected; needs combat tests |
| Packet framing | Preserve a split 2-byte length prefix; isolate leftover parsing per server client | Source inspected; segmented/multi-client runtime tests outstanding |
| Add-on negotiation | Server connection handler uses the connection registry rather than the update registry | Source inspected; addon integration test outstanding |
| Chunk transport | 256 slices, per-chunk ACK/index reset, reject unrelated chunks, clear sender queue on disconnect | Production chunk and packet regression tests passed; rapid stop/start race fixed |
| Wire format | Preserve existing byte count encoding and extra ACK padding when slice count is divisible by 8 | Production serializer golden byte fixtures in `tests/packet-smoke` |
| Dedicated server save | Preserve shared save-data references, back up on writes, restore valid backup, refuse corrupt saves | Production file I/O tests on Windows and Linux with simplified game codec |
| Player catch-up | Guard missing source progression and rollback on persistence failure | Production `/copysave` headless tests passed |
| Runtime/build | No fork changes to original CSProj game reference list; added source syntax and test CI | Roslyn C# syntax tests passed; **full compilation not verified** |

## Code changes audited against upstream master

`HKMP/Game/Client/Entity/EntityManager.cs`, `HKMP/Game/Command/Server/CopySaveCommand.cs`, `HKMP/Game/Server/ServerManager.cs`, `HKMP/Networking/Chunk/{ChunkReceiver,ChunkSender,ClientChunkSender,ServerChunkSender}.cs`, `HKMP/Networking/Client/{ClientUpdateManager,NetClient}.cs`, `HKMP/Networking/Packet/{PacketManager,Data/SliceAckData,Data/SliceData}.cs`, `HKMP/Networking/Server/{NetServer,NetServerClient,ServerUpdateManager}.cs`, and `HKMPServer/ConsoleServerManager.cs`.

This is a **review of differences**. It is not a claim that all inherited upstream gameplay code was formally verified.

## Release-blocking unknowns (not solved by a source review)

1. Full solution compilation for the targeted game Modding API DLLs (the existing full .NET CI currently fails at decryption of its privately provisioned references before compilation).
2. Actual two-client and dedicated-server boot, first connection, rejection and reconnect, scene-host handoff, boss combat, merchant interactions, pickup progression, save/reload, and corrupt *real game save* recovery.
3. Add-on interoperability and compatibility against different historical client/server protocol generations. Same exact fork revision for client and server is required.
4. Runtime behavior under packet loss and latency and cross-platform Mono. Code syntax tests and stub-based harnesses do not establish these.

## Recommended final-test setup

- Install game build **1.5.78.11833** and the compatible modding API in a disposable copy.
- Build both client and server from the *same* fork commit, matching managed game references and dependencies.
- Enable **Full Synchronisation**; it is off by default in both in-game hosting and standalone console settings.
- Run `GAMEPLAY_QA.md` with separate saves and at least two clients; record results by scenario and exact git SHA.

**Decision:** Source-only comparison did not reveal a changed Unity reference surface or packet ID table. The fork has focused tested improvements, but it is not proven functionally equivalent to published v2.4.3, and it is not code-complete-proof for all experimental upstream full-sync features.
