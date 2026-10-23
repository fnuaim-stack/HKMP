# HKMP co-op gameplay stabilization

Fork of [Extremelyd1/HKMP](https://github.com/Extremelyd1/HKMP). Gameplay review follows the [Co-op Anything](https://github.com/cranebeaker/coop-anything) `coop-revive`, `coop-sync-testing`, and `coop-qa` workflows.

**IMPORTANT:** These fixes are source-level changes and isolated regression tests. No complete Hollow Knight/Modding API build or two-client gameplay session has been verified on this fork.

## Code changes made

| Subsystem | Problem | Patch | Verification |
|---|---|---|---|
| Entity queue | Missing entity at the head can freeze the game | Bounded processing, defer missing entities | Needs game test |
| Entity authority | Non-scene-host clients can replace simulated entity state | Host-only position/scale/animation, active and host-FSM data; generic interaction data still allowed | Needs combat test |
| Add-on connections | Server dispatches connection data using update registry | Connection handler registry | Needs addon regression |
| Network framing | Fragments from multiple connections can be mixed | Per-client leftover packet state | Needs 2+ clients |
| Network framing | Split 2-byte length prefix is dropped | Preserve a one-byte remainder | Needs segmented-packet regression |
| Chunk transfer | Invalid slice IDs or lengths | Reject malformed slices before applying | Production packet serializer smoke tests passed |
| Dedicated saves | Corrupted save can be overwritten with empty progress | Backup recovery, quarantine of unreadable primary, and atomic replacement | Needs save recovery test |
| Consecutive connection chunks | ACK flags and slice index reused from previous packet | Reset per-chunk state; skip invalid-sized packets without keeping sender busy | Production sender/receiver headless CI passed; game untested |
| In-flight chunk corruption | A later chunk's slice with the same count could be accepted into the current one | Enforce current chunk ID during assembly | Future-slice injection headless CI passed; game untested |
| Reconnect after rejection | Failed server handshake kept DTLS and packet framing state alive; queued old chunks survived Stop | Fully disconnect failed login, clear partial frames and stale outgoing queue | Source-reviewed; needs actual login/reconnect test |

## Gameplay investigations still open

- Player interactions with bosses, enemy health, attacks, animations, hit detection and death
- New scene loading, scene-host transfer, host disconnect and late join
- Ability and key-item progression (reported upstream [#97](https://github.com/Extremelyd1/HKMP/issues/97))
- Vendors, fragile-charm repair and NPC state ([#99](https://github.com/Extremelyd1/HKMP/issues/99))
- Co-op save selection, restoration and progress across sessions ([#111](https://github.com/Extremelyd1/HKMP/issues/111), [#128](https://github.com/Extremelyd1/HKMP/issues/128))
- Multiple-player latency and packet-loss resilience

The referenced upstream issues were closed and may be obsolete. Reproduce on the fork before claiming a current bug or its fix.

## Excluded from this gameplay branch

Certificate changes and general security hardening are intentionally deprioritized at the owner's request.

## Completion requirement

Work is not finished until `GAMEPLAY_QA.md` passes with independent Hollow Knight clients, on an exact recorded game version and mod commit.
