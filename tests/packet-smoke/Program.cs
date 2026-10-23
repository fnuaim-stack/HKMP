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
        Assert(bytes.Length == 2 + (count + 7) / 8, $"no extra ack flag for {count} slices");

        var roundTrip = new SliceAckData();
        roundTrip.ReadData(new MemoryPacket(bytes));
        Assert(roundTrip.NumSlices == count, $"ack count {count}");
        Assert(roundTrip.ChunkId == 5, "ack chunk ID");
        for (int i = 0; i < count; i++) {
            Assert(roundTrip.Acked[i] == original.Acked[i], $"ack bit {i} of {count}");
        }
    }

    private static int Main() {
        try {
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
