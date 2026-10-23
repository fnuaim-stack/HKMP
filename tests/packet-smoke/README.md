# Core packet regression smoke tests

This .NET 8 console runner compiles the **production** HKMP `SliceData` and `SliceAckData` source files without Hollow Knight, Unity or NuGet test frameworks. It uses a minimal in-memory `IPacket` implementation to check the wire format.

Run: `dotnet run --project tests/packet-smoke/PacketSmoke.csproj --configuration Release`

Tests cover 1–256 slices, partial and complete slices, acknowledgments at multiples of eight, 256-slice round trips, and rejection of impossible incoming slice indices and lengths.

This is *not* a full HKMP build or real-game test. It cannot validate Unity entity synchronization, boss logic, save files, or DTLS gameplay.

The linked production sources are under `HKMP/Networking/Packet/Data/`, so smoke tests exercise the actual serialization implementations, not a copy of the algorithm.
