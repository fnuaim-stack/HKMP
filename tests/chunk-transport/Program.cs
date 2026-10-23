using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hkmp.Networking;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

internal sealed class TestSender : ChunkSender {
    public TestReceiver Receiver;
    public bool InjectFutureSlice = true;
    public bool FutureSliceInjected;
    public int SendCalls;

    protected override void SetSliceData(byte chunkId, byte sliceId, ushort numSlices, byte[] data) {
        SendCalls++;

        // The test uses the actual production serializers at every network boundary.
        var original = new SliceData {
            ChunkId = chunkId,
            SliceId = sliceId,
            NumSlices = numSlices,
            Data = data
        };

        var raw = new MemoryPacket();
        original.WriteData(raw);
        var decoded = new SliceData();
        decoded.ReadData(new MemoryPacket(raw.ToArray()));
        Receiver.ProcessReceivedData(decoded);

        // A future chunk with the same number of slices must not corrupt
        // an in-progress chunk, even if it arrives out of order.
        if (InjectFutureSlice && !FutureSliceInjected && chunkId == 0 &&
            sliceId == 0 && numSlices > 1) {
            FutureSliceInjected = true;
            Receiver.ProcessReceivedData(new SliceData {
                ChunkId = (byte) (chunkId + 1),
                SliceId = 1,
                NumSlices = numSlices,
                Data = Enumerable.Repeat((byte) 0xee, ConnectionManager.MaxSliceSize).ToArray()
            });
        }
    }
}

internal sealed class TestReceiver : ChunkReceiver {
    public TestSender Sender;
    public int Acks;

    protected override void SetSliceAckData(byte chunkId, ushort numSlices, bool[] acked) {
        Acks++;
        var original = new SliceAckData {
            ChunkId = chunkId,
            NumSlices = numSlices,
            Acked = acked
        };
        var raw = new MemoryPacket();
        original.WriteData(raw);
        var decoded = new SliceAckData();
        decoded.ReadData(new MemoryPacket(raw.ToArray()));
        Sender.ProcessReceivedData(decoded);
    }
}

internal static class Program {
    private static int _checks;

    private static void Check(bool pass, string label) {
        _checks++;
        if (!pass) {
            throw new Exception("FAIL: " + label);
        }
    }

    private static byte[] Payload(int size, int seed) {
        var bytes = new byte[size];
        for (var i = 0; i < bytes.Length; i++) {
            bytes[i] = (byte) ((i + seed) % 251);
        }

        return bytes;
    }

    private static void TestMultipleChunksAndForeignSlice() {
        var sender = new TestSender();
        var receiver = new TestReceiver();
        sender.Receiver = receiver;
        receiver.Sender = sender;
        var received = new ConcurrentQueue<byte[]>();
        using var finished = new ManualResetEventSlim();

        receiver.ChunkReceivedEvent += packet => received.Enqueue(packet.ToArray());
        var messages = new List<byte[]> {
            Payload(4 * ConnectionManager.MaxSliceSize + 17, 11),
            Payload(3, 20),        // Fewer slices than the preceding chunk
            Payload(2 * ConnectionManager.MaxSliceSize, 21),
            Payload(1, 22),        // One-slice chunk after a larger transfer
            Payload(ConnectionManager.MaxChunkSize, 23), // Exact 256-slice boundary
            Payload(13, 24)
        };

        // Invalid queued packets should be skipped, not sent as corrupt slices.
        sender.EnqueuePacket(new Packet(Array.Empty<byte>()));
        sender.EnqueuePacket(new Packet(new byte[ConnectionManager.MaxChunkSize + 1]));
        foreach (var bytes in messages) {
            sender.EnqueuePacket(new Packet(bytes));
        }

        sender.FinishSendingData(() => finished.Set());
        sender.Start();
        try {
            Check(finished.Wait(TimeSpan.FromSeconds(20)), "sender finished every queued chunk");
            Check(received.Count == messages.Count, "receiver obtained every nonempty, valid chunk");
            var actual = received.ToArray();
            for (var i = 0; i < messages.Count; i++) {
                Check(actual[i].AsSpan().SequenceEqual(messages[i]), "content and order of chunk " + i);
            }

            Check(sender.FutureSliceInjected, "out-of-order future chunk was exercised");
            Check(sender.SendCalls >= 266, "sender exercised 256-slice maximum and multiple transfers");
            Check(receiver.Acks == sender.SendCalls, "every valid sent slice was acknowledged");
        } finally {
            sender.Stop();
        }
    }

    private static void TestStopClearsPreviousConnectionQueue() {
        var sender = new TestSender();
        sender.EnqueuePacket(new Packet(new byte[] { 10, 11 }));
        sender.EnqueuePacket(new Packet(new byte[] { 12, 13 }));
        sender.Stop();

        bool completed = false;
        sender.FinishSendingData(() => completed = true);
        Check(completed, "stop clears queued old-connection handshake data");
    }

    private static void TestImmediateStartStop() {
        // Connection rejection and timeouts can occur before the sender thread
        // receives CPU time. The sender must never dereference a shared
        // CancellationTokenSource that Stop() has already cleared.
        for (var i = 0; i < 100; i++) {
            var sender = new TestSender();
            sender.Start();
            sender.Stop();
        }
        Check(true, "100 immediate start/stop cycles completed without sender thread exception");
    }

    private static void Main() {
        TestImmediateStartStop();
        TestStopClearsPreviousConnectionQueue();
        TestMultipleChunksAndForeignSlice();
        Console.WriteLine($"PASS: {_checks} real sender/receiver chunk-transfer checks");
    }
}
