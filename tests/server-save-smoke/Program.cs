using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hkmp.Game.Server.Save;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer;
using HkmpServer.Logging;

internal sealed class TestServer : ConsoleServerManager {
    public TestServer() : base(new NetServer(), new PacketManager(), new ServerSettings(), new ConsoleLogger()) {}
    public ServerSaveData Data => ServerSaveData;
}

internal static class Program {
    private static int _checks;
    private static void Check(bool ok, string message) {
        _checks++;
        if (!ok) throw new Exception("FAIL: " + message);
    }

    private static TestServer StartServer() {
        var server = new TestServer();
        server.Start(26950, true);
        return server;
    }

    private static void ExpectInvalidData(Action action, string description) {
        var caught = false;
        try { action(); } catch (InvalidDataException) { caught = true; }
        Check(caught, description);
    }

    private static int Main() {
        // Never touch real game saves. This test runs inside its own compiled test
        // output folder and refuses to overwrite preexisting data.
        var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var primary = Path.Combine(folder, "save.json");
        var backup = primary + ".bak";
        var temporary = primary + ".tmp";
        if (File.Exists(primary) || File.Exists(backup) || File.Exists(temporary)) {
            Console.Error.WriteLine("Refusing to overwrite existing save files in the smoke-test output folder");
            return 2;
        }

        try {
            var first = new TestServer();
            var originalReference = first.Data;
            first.Start(26950, true);
            Check(ReferenceEquals(originalReference, first.Data), "constructor-owned save object retained at startup");
            Check(File.Exists(primary), "initial primary save created");
            Check(!File.Exists(backup), "backup not created for a brand-new server");

            first.Data.GlobalSaveData[7] = new byte[] { 7 };
            first.Data.PlayerSaveData["alice"] = new Dictionary<ushort, byte[]> {
                [5] = new byte[] { 17 }
            };
            Check(first.PersistSaveData(), "first update written");
            Check(File.Exists(backup), "previous primary backed up");
            Check(first.PersistSaveData(), "second update written and backup refreshed");

            var second = new TestServer();
            var secondReference = second.Data;
            second.Start(26950, true);
            Check(ReferenceEquals(secondReference, second.Data), "reload preserves object used by commands");
            Check(second.Data.GlobalSaveData[7][0] == 7, "global progression restored");
            Check(second.Data.PlayerSaveData["alice"][5][0] == 17, "player progression restored");

            File.WriteAllText(primary, "{}");
            var restored = StartServer();
            Check(restored.Data.PlayerSaveData["alice"][5][0] == 17, "missing fields trigger backup recovery");
            Check(!File.Exists(primary), "corrupt primary is quarantined");
            Check(Directory.GetFiles(folder, "save.json.corrupt-*").Length == 1,
                "corrupt source retained separately");
            Check(restored.PersistSaveData(), "restored backup can be written to new primary");
            Check(File.Exists(primary), "new primary created from backup");

            File.WriteAllText(primary, "invalid primary");
            File.WriteAllText(backup, "invalid backup");
            ExpectInvalidData(() => StartServer(), "double corruption refuses startup");
            Check(File.ReadAllText(primary) == "invalid primary", "corrupt primary not overwritten");
            Check(File.ReadAllText(backup) == "invalid backup", "corrupt backup not overwritten");

            Console.WriteLine($"PASS: {_checks} production dedicated-save tests");
            return 0;
        } catch (Exception error) {
            Console.Error.WriteLine(error);
            return 1;
        } finally {
            foreach (var file in Directory.GetFiles(folder, "save.json*")) {
                File.Delete(file);
            }
        }
    }
}
