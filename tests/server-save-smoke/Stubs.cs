using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hkmp.Api.Command.Server {
    internal interface IServerCommand {}
    internal class FakeCommandManager {
        public void RegisterCommand(IServerCommand cmd) {}
        public void DeregisterCommand(IServerCommand cmd) {}
    }
}
namespace Hkmp.Game.Settings { internal class ServerSettings {} }
namespace Hkmp.Logging {
    internal static class Logger {
        public static void Warn(string s) => Console.WriteLine("WARN: " + s);
        public static void Error(string s) => Console.WriteLine("ERROR: " + s);
    }
}
namespace Hkmp.Networking.Packet { internal class PacketManager {} }
namespace Hkmp.Networking.Packet.Data { internal class SaveUpdate {} }
namespace Hkmp.Networking.Server { internal class NetServer {} }
namespace HkmpServer.Logging { internal class ConsoleLogger {} }
namespace HkmpServer.Command {
    internal class ExitCommand : Hkmp.Api.Command.Server.IServerCommand {
        public ExitCommand(Hkmp.Game.Server.ServerManager manager) {}
    }
    internal class ConsoleSettingsCommand : Hkmp.Api.Command.Server.IServerCommand {
        public ConsoleSettingsCommand(Hkmp.Game.Server.ServerManager manager, Hkmp.Game.Settings.ServerSettings settings) {}
    }
    internal class LogCommand : Hkmp.Api.Command.Server.IServerCommand {
        public LogCommand(HkmpServer.Logging.ConsoleLogger logger) {}
    }
}
namespace Hkmp.Game.Server.Save {
    internal class ServerSaveData {
        public Dictionary<ushort, byte[]> GlobalSaveData { get; set; } = new();
        public Dictionary<string, Dictionary<ushort, byte[]>> PlayerSaveData { get; set; } = new();
    }
    internal class ModSaveFile {
        public class SaveData : Dictionary<ushort, byte[]> {}
        [JsonProperty("playerSaveData")]
        public Dictionary<string, SaveData> PlayerSaveData { get; set; } = new();
        public virtual ServerSaveData ToServerSaveData() => new();
        public static ModSaveFile FromServerSaveData(ServerSaveData data) => new();
    }
}
namespace Hkmp.Util {
    internal static class EncodeUtil {
        public static Dictionary<ushort, byte[]> ConvertToServerSaveData(Hkmp.Game.Server.Save.ModSaveFile.SaveData data) =>
            new(data);

        public static Hkmp.Game.Server.Save.ModSaveFile.SaveData ConvertFromServerSaveData(
            Dictionary<ushort, byte[]> data) {
            var save = new Hkmp.Game.Server.Save.ModSaveFile.SaveData();
            foreach (var item in data) save[item.Key] = item.Value;
            return save;
        }
    }
}
namespace Hkmp.Game.Server {
    internal class FakeAddonManager { public void LoadAddons() {} }
    internal class ServerManager {
        protected Hkmp.Game.Server.Save.ServerSaveData ServerSaveData = new();
        protected Hkmp.Game.Settings.ServerSettings InternalServerSettings = new();
        protected Hkmp.Api.Command.Server.FakeCommandManager CommandManager = new();
        protected FakeAddonManager AddonManager = new();
        protected ServerManager(
            Hkmp.Networking.Server.NetServer netServer,
            Hkmp.Networking.Packet.PacketManager packetManager,
            Hkmp.Game.Settings.ServerSettings settings) {}
        public virtual void Initialize() {}
        public virtual void Start(int port, bool fullSynchronisation) {}
        protected virtual void RegisterCommands() {}
        protected virtual void DeregisterCommands() {}
        protected virtual void OnSaveUpdate(ushort id, Hkmp.Networking.Packet.Data.SaveUpdate data) {}
        public virtual bool PersistSaveData() => true;
        public void Stop() {}
    }
}
