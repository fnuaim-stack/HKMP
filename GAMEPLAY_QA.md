# HKMP co-op gameplay QA / release gates

This manual checklist is **not executed**. Based on Co-op Anything's coop-revive, coop-sync-testing, and coop-qa practices.

## Setup

- Record exact Hollow Knight version, HKMP commit, operating system, installed add-ons and active options.
- Use isolated client installs/test save slots. Back up the original save data first.
- Use one host and two independent game clients when possible; test standalone-server hosting separately.
- Keep client and server logs for every failure and name the first moment players' worlds disagree.

## Required scenarios

| Scenario | Steps | What must be true |
|---|---|---|
| Initial session | Host, join, disconnect, rejoin | Both characters appear; no stuck connections or duplicate players |
| Scene loading | Both enter same zone; one enters late | No freezes, missing entity updates eventually apply |
| Scene-host handoff | The host leaves a scene or disconnects | Exactly one machine simulates each enemy, enemies continue moving |
| Combat | Both damage one enemy with nail and spells | Health, attack animation, death and rewards converge |
| Boss encounter | Enter boss arena together, one dies/leaves | No duplicate bosses; consistent health, rewards and scene changes |
| Rejoin after kill | One player rejoins after an enemy dies | Dead enemy remains dead on both clients |
| Ability progression | Obtain Mantis Claw and another ability | Both players retain intended progression across sessions |
| Merchant dialogue | Purchase an item and repair a broken charm | Merchant inventory and charm state remain usable |
| Save restart | Save, stop server, start server, rejoin | Prior progress restored and consistent |
| Save corruption | Corrupt a copied test save; keep valid .bak | Backup loads; corrupted primary is preserved separately |
| Double corruption | Corrupt primary and backup in isolated test folder | Server refuses to start; no progress files overwritten |
| Add-on connection | Register distinct connection and update handlers | Correct handler fires for each packet type |
| Network loss | 100–250 ms artificial RTT and 1–5% packet loss | No permanent duplication, desync or freezes |
| Multiple connection chunks | Send a 5+ slice connection packet followed by a smaller packet | Sender starts at slice zero, carries no previous ACKs, recipient receives both |
| Login rejected then retry | Connect with incorrect add-on settings, then correct settings | Previous session shuts down; retry succeeds without ghost frames |
| Sequential join/save chunks | Repeated joins and save transfers, including large data | No stuck 'connecting' screen or lost progress; all chunks complete |

## Before tagging a release

- [ ] Both C# projects compile in Release configuration with required game and Modding API assemblies
- [ ] Regression tests exist for queue processing, packet splitting, add-on dispatch and save recovery
- [x] Headless production sender/receiver test covers consecutive chunks, 256 slices, ACK reset and foreign-slice rejection (not in-game validation)
- [ ] Two-client co-op playthrough completed in normal conditions
- [ ] Boss, progression and NPC scenarios tested after reconnect and restart
- [ ] Test with latency and loss; inspect client/server logs
- [ ] Standalone server validated on Windows; Mono compatibility verified if advertised
- [ ] Build artifacts, dependency list, checksums and installation instructions prepared
- [ ] Release candidate first published as **prerelease**, not stable
- [ ] Original LGPL license and credit preserved

Never interpret a source-code change as proof a gameplay issue is fixed.
