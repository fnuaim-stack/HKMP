using System;
using System.Collections.Generic;

namespace Hkmp.Networking {
    internal abstract class ConnectionManager {
        public const int MaxSliceSize = 1024;
        public const int MaxSlicesPerChunk = 256;
        public const int MaxChunkSize = MaxSlicesPerChunk * MaxSliceSize;

        public static bool IsWrappingIdSmaller(byte id1, byte id2) =>
            id1 < id2 && id2 - id1 <= 128 || id1 > id2 && id1 - id2 > 128;
    }
}

namespace Hkmp.Logging {
    internal static class Logger {
        public static void Error(string message) {
            Console.Error.WriteLine(message);
        }
    }
}

namespace Hkmp.Networking.Packet {
    public interface IPacket {
        void Write(byte value);
        void Write(ushort value);
        byte ReadByte();
        ushort ReadUShort();
    }

    internal sealed class MemoryPacket : IPacket {
        private readonly List<byte> _bytes;
        private int _position;

        public MemoryPacket() {
            _bytes = new List<byte>();
        }

        public MemoryPacket(byte[] data) {
            _bytes = new List<byte>(data);
        }

        public void Write(byte value) => _bytes.Add(value);

        public void Write(ushort value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
        }

        public byte ReadByte() {
            if (_position >= _bytes.Count) {
                throw new InvalidOperationException("Unexpected end of frame");
            }

            return _bytes[_position++];
        }

        public ushort ReadUShort() => (ushort) (ReadByte() | (ReadByte() << 8));

        public byte[] ToArray() => _bytes.ToArray();
    }

    internal class Packet {
        private readonly byte[] _bytes;

        public Packet(byte[] bytes) {
            _bytes = bytes;
        }

        public byte[] ToArray() => _bytes;
    }
}
