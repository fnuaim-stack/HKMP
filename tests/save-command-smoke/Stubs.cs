using System;
using System.Collections.Generic;
using System.Linq;

namespace Hkmp.Api.Command.Server {
    internal interface ICommandSender {
        void SendMessage(string message);
    }

    internal interface IServerCommand {
        string Trigger { get; }
        string[] Aliases { get; }
        bool AuthorizedOnly { get; }
        void Execute(ICommandSender sender, string[] args);
    }

    internal sealed class FakeSender : ICommandSender {
        public List<string> Messages { get; } = new List<string>();
        public void SendMessage(string message) => Messages.Add(message);
    }
}

namespace Hkmp.Networking.Packet.Data {
    internal enum DisconnectReason { Kicked }
}

namespace Hkmp.Networking.Server {
    // Namespace retained for linked production source compatibility.
}

namespace Hkmp.Game.Server.Save {
    internal sealed class ServerSaveData {
        public Dictionary<string, Dictionary<ushort, byte[]>> PlayerSaveData { get; } =
            new Dictionary<string, Dictionary<ushort, byte[]>>();
    }
}

namespace Hkmp.Game.Server {
    internal sealed class Player {
        public string Username { get; set; }
        public string AuthKey { get; set; }
        public ushort Id { get; set; }
    }

    internal class ServerManager {
        public List<Player> Players { get; } = new List<Player>();
        public bool ShouldPersist { get; set; } = true;
        public int PersistCount { get; private set; }
        public int DisconnectCount { get; private set; }

        public bool PersistSaveData() {
            PersistCount++;
            return ShouldPersist;
        }

        public void DisconnectPlayer(ushort id, Hkmp.Networking.Packet.Data.DisconnectReason reason) {
            DisconnectCount++;
        }
    }
}

namespace Hkmp.Util {
    internal static class CommandUtil {
        public static bool TryGetPlayerByName(
            IEnumerable<Hkmp.Game.Server.Player> players, string name,
            out Hkmp.Game.Server.Player player) {
            player = players.FirstOrDefault(p => p.Username == name);
            return player != null;
        }
    }
}
