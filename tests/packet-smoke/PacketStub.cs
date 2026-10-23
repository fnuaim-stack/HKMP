using System;
using System.Collections.Generic;

namespace Hkmp.Networking {
    // The protocol constants in ConnectionManager, independent of game/Unity.
    internal static class ConnectionManager {
        public const int MaxSliceSize = 1024;
        public const int MaxSlicesPerChunk = 256;
    }
}

namespace Hkmp.Networking.Packet {
    // Minimal interface needed by the linked production packet data source files.
    public interface IPacket {
        void Write(byte value);
        void Write(ushort value);
        byte ReadByte();
        ushort ReadUShort();
    }

    internal sealed class MemoryPacket : IPacket {
        private readonly List<byte> _bytes;
        private int _readPosition;

        public MemoryPacket() {
            _bytes = new List<byte>();
        }

        public MemoryPacket(byte[] bytes) {
            _bytes = new List<byte>(bytes);
        }

        public byte[] ToArray() => _bytes.ToArray();

        public void Write(byte value) => _bytes.Add(value);

        public void Write(ushort value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
        }

        public byte ReadByte() {
            if (_readPosition >= _bytes.Count) {
                throw new InvalidOperationException("Packet data truncated");
            }
            return _bytes[_readPosition++];
        }

        public ushort ReadUShort() =>
            (ushort) (ReadByte() | (ReadByte() << 8));
    }
}
