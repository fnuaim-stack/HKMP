# HKMP gameplay stabilization — release candidate notes

**Status: development branch, not a release.**

## Current work

- Prevent infinite processing of unresolved entity updates
- Enforce scene-host authority for simulated enemy state while preserving other players' interaction updates
- Fix server add-on connection handler dispatch
- Isolate incomplete packet state for each connected client
- Preserve a partial packet-length prefix
- Validate incoming chunk slice identifiers and lengths
- Protect persistent save data with recoverable backups and safer writes

## Still required

Build HKMP.dll and HKMPServer.exe; validate compatibility against the installed Hollow Knight and Modding API versions; complete `GAMEPLAY_QA.md` with real clients; add automated regression coverage; investigate the remaining boss/NPC/progression desynchronization cases.

## Distribution target

Once validated, publish a **prerelease** with:
- Client `HKMP.dll` and required runtime dependencies
- Standalone `HKMPServer.exe` and its dependencies
- SHA-256 checksums
- Supported game/API versions and platforms
- Installer and manual-install instructions
- Known limitations and original-author attribution

This fork derives from [Extremelyd1/HKMP](https://github.com/Extremelyd1/HKMP), licensed LGPL-2.1-or-later. Co-op testing methods inspired by [cranebeaker/coop-anything](https://github.com/cranebeaker/coop-anything).
