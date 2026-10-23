using System;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

internal static class Program {
    private static int _tests;

    private static void Assert(bool condition, string message) {
        _tests++;
        if (!condition) {
            throw new Exception("FAIL: " + message);
        }
    }

    private static void ExpectFailure<TException>(Action action, string name) where TException : Exception {
        bool failed = false;
        try {
            action();
        } catch (TException) {
            failed = true;
        }

        Assert(failed, name);
    }

    private static void SliceRoundTrip(ushort count, byte index, int length) {
        var original = new SliceData {
            ChunkId = 7,
            SliceId = index,
            NumSlices = count,
            Data = new byte[length]
        };
        for (int i = 0; i < original.Data.Length; i++) {
            original.Data[i] = (byte) (i % 251);
        }

        var packet = new MemoryPacket();
        original.WriteData(packet);
        var roundTrip = new SliceData();
        roundTrip.ReadData(new MemoryPacket(packet.ToArray()));
        Assert(roundTrip.NumSlices == count, $"slice count {count}");
        Assert(roundTrip.SliceId == index, $"slice index {index}");
        Assert(roundTrip.ChunkId == original.ChunkId, "chunk ID");
        Assert(roundTrip.Data.AsSpan().SequenceEqual(original.Data), $"slice data length {length}");
    }

    private static void AckRoundTrip(ushort count) {
        var original = new SliceAckData {
            ChunkId = 5,
            NumSlices = count,
            Acked = new bool[count]
        };
        original.Acked[0] = true;
        original.Acked[count - 1] = true;
        if (count > 3) {
            original.Acked[3] = true;
        }

        var packet = new MemoryPacket();
        original.WriteData(packet);
        byte[] bytes = packet.ToArray();
        // Keep the original one-byte padding when count is divisible by 8.
        // Deployed HKMP receivers expect this exact packet size.
        Assert(bytes.Length == 3 + count / 8, $"legacy ACK wire size for {count} slices");

        var roundTrip = new SliceAckData();
        roundTrip.ReadData(new MemoryPacket(bytes));
        Assert(roundTrip.NumSlices == count, $"ack count {count}");
        Assert(roundTrip.ChunkId == 5, "ack chunk ID");
        for (int i = 0; i < count; i++) {
            Assert(roundTrip.Acked[i] == original.Acked[i], $"ack bit {i} of {count}");
        }
    }

    private static void LegacyWireFormatGoldenVectors() {
        // These are the exact byte layouts of the unmodified upstream
        // Extremelyd1/HKMP master WriteData implementations (f7c35d96).
        // They detect silent changes to chunk/ACK field order or the
        // legacy extra padding flag for ACKs of 8, 16, ... 256 slices.
        var oneSlice = new SliceData {
            ChunkId = 7, SliceId = 0, NumSlices = 1,
            Data = new byte[] { 0xAA, 0xBB }
        };
        var oneBytes = new MemoryPacket();
        oneSlice.WriteData(oneBytes);
        Assert(oneBytes.ToArray().AsSpan().SequenceEqual(
            new byte[] { 7, 0, 0, 2, 0, 0xAA, 0xBB }),
            "legacy single-slice wire bytes");

        var finalOf256 = new SliceData {
            ChunkId = 7, SliceId = 255, NumSlices = 256,
            Data = new byte[] { 0xEF }
        };
        var lastBytes = new MemoryPacket();
        finalOf256.WriteData(lastBytes);
        Assert(lastBytes.ToArray().AsSpan().SequenceEqual(
            new byte[] { 7, 255, 255, 1, 0, 0xEF }),
            "legacy 256-slice final slice uses encoded count 255");

        var flags = new bool[8];
        flags[0] = true;
        flags[7] = true;
        var eightAck = new MemoryPacket();
        new SliceAckData { ChunkId = 5, NumSlices = 8, Acked = flags }
            .WriteData(eightAck);
        Assert(eightAck.ToArray().AsSpan().SequenceEqual(
            new byte[] { 5, 7, 0x81, 0 }),
            "legacy eight-slice ACK padding flag preserved");

        var maxFlags = new bool[256];
        maxFlags[0] = true;
        maxFlags[255] = true;
        var maxAck = new MemoryPacket();
        new SliceAckData { ChunkId = 5, NumSlices = 256, Acked = maxFlags }
            .WriteData(maxAck);
        var serialized = maxAck.ToArray();
        Assert(serialized.Length == 35, "legacy 256-slice ACK has 32 flags plus one padding");
        Assert(serialized[0] == 5 && serialized[1] == 255 &&
               serialized[2] == 1 && serialized[33] == 0x80 &&
               serialized[34] == 0,
               "legacy 256-slice ACK flag positions");
    }

    private static int Main() {
        try {
            LegacyWireFormatGoldenVectors();
            SliceRoundTrip(1, 0, 16);
            SliceRoundTrip(2, 0, 1024);
            SliceRoundTrip(2, 1, 1);
            SliceRoundTrip(255, 254, 27);
            SliceRoundTrip(256, 254, 1024);
            SliceRoundTrip(256, 255, 1024);

            // An impossible slice index cannot count towards completing a save chunk.
            ExpectFailure<ArgumentOutOfRangeException>(() => {
                var invalid = new SliceData();
                invalid.ReadData(new MemoryPacket(new byte[] { 0, 2, 1, 0, 0 }));
            }, "invalid slice ID");

            // Last-slice length zero and lengths over 1024 are not valid.
            ExpectFailure<ArgumentOutOfRangeException>(() => {
                var invalid = new SliceData();
                invalid.ReadData(new MemoryPacket(new byte[] { 0, 0, 0, 0, 0 }));
            }, "zero slice data");

            ExpectFailure<ArgumentOutOfRangeException>(() => {
                var invalid = new SliceData();
                invalid.ReadData(new MemoryPacket(new byte[] { 0, 0, 0, 1, 4 }));
            }, "oversize slice data");

            foreach (ushort n in new ushort[] { 1, 7, 8, 9, 31, 32, 255, 256 }) {
                AckRoundTrip(n);
            }

            Console.WriteLine($"PASS: {_tests} production packet serialization assertions");
            return 0;
        } catch (Exception ex) {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
