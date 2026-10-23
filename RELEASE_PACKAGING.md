# Packaging HKMP and its dedicated server

The Windows screenshots supplied by the owner show `HKMP.dll`, `HKMP.pdb`, `HKMP.xml` in one directory and `HKMPServer.exe`, `HKMPServer.pdb` in another. Those are **not sufficient to confirm a current, complete build**.

## Runtime dependencies

The client needs `HKMP.dll` and `BouncyCastle.Cryptography.dll`. The standalone server needs `HKMPServer.exe`, the **matching** `HKMP.dll`, `BouncyCastle.Cryptography.dll`, and `Newtonsoft.Json.dll`. The latter two are third-party runtime libraries, not included in the owner's screenshots. The build requires other game/Unity/PlayMaker/Modding API reference DLLs available privately from a compatible installation; **do not upload those to the public repo**.

## Package previously compiled Windows builds

From the repo checkout (using binaries actually compiled from the commit to be released):

```powershell
./scripts/package-release.ps1 -ClientDir "$env:USERPROFILE\Downloads\HKMP" -ServerDir "$env:USERPROFILE\Downloads\HKMPServer" -DependencyDir "<directory with BouncyCastle and Newtonsoft DLLs>" -OutputDir "./dist-rc1" -SourceCommit "<40-character commit SHA>" -Version "v3.0.0-coop-rc1"
```

The script fails when a required dependency is missing or when the server's and client's copies of `HKMP.dll` disagree. It creates two ZIPs and `SHA256SUMS.txt`. This tests **packaging**, not the source code, binary provenance, or gameplay.

## GitHub release automation

`.github/workflows/release-candidate.yml` is a manually dispatched action intended for **master**. Given a successful `.NET CI` **push** build run ID for the same commit, it downloads the `HKMP` and `HKMPServer` build artifacts, packages them, checks SHA-256, and publishes a **prerelease**.

The fork's current inherited full build cannot succeed because `DEPENDENCIES_ZIP_PASSPHRASE` is absent. Do not bypass the provenance check or republish upstream binaries as if built from this fork.

The independent packaging smoke action only uses synthetic test files. Never describe a prerelease as gameplay-tested unless the steps in `GAMEPLAY_QA.md` were actually completed.
