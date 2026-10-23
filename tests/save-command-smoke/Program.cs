using System;
using System.Collections.Generic;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Server.Save;

static class Program {
    private static int checks;

    static void Check(bool condition, string message) {
        checks++;
        if (!condition) throw new Exception("Regression failed: " + message);
    }

    static (ServerManager manager, ServerSaveData data, FakeSender sender, CopySaveCommand command) NewTest() {
        var manager = new ServerManager();
        manager.Players.Add(new Player { Username = "Alice", AuthKey = "a", Id = 1 });
        manager.Players.Add(new Player { Username = "Bob", AuthKey = "b", Id = 2 });
        var data = new ServerSaveData();
        var sender = new FakeSender();
        return (manager, data, sender, new CopySaveCommand(manager, data));
    }

    static void TestSuccessAndDeepCopy() {
        var x = NewTest();
        var value = new byte[] { 1, 2, 3 };
        x.data.PlayerSaveData["a"] = new Dictionary<ushort, byte[]> { [5] = value };
        x.command.Execute(x.sender, new[] { "/copysave", "Alice", "Bob" });
        Check(x.manager.PersistCount == 1, "copy must persist before target disconnect");
        Check(x.manager.DisconnectCount == 1, "successful copy should disconnect recipient");
        Check(x.data.PlayerSaveData["b"][5][0] == 1, "data copied");
        value[0] = 42;
        Check(x.data.PlayerSaveData["b"][5][0] == 1, "copied bytes must be independent of original");
    }

    static void TestMissingSource() {
        var x = NewTest();
        x.command.Execute(x.sender, new[] { "/copysave", "Alice", "Bob" });
        Check(x.manager.PersistCount == 0, "missing source should not persist");
        Check(x.manager.DisconnectCount == 0, "missing source must not kick recipient");
        Check(!x.data.PlayerSaveData.ContainsKey("b"), "missing source must not create progress");
    }

    static void TestRollback() {
        var x = NewTest();
        var previous = new Dictionary<ushort, byte[]> { [6] = new byte[] { 8 } };
        x.data.PlayerSaveData["a"] = new Dictionary<ushort, byte[]> { [5] = new byte[] { 1 } };
        x.data.PlayerSaveData["b"] = previous;
        x.manager.ShouldPersist = false;
        x.command.Execute(x.sender, new[] { "/copysave", "Alice", "Bob" });
        Check(object.ReferenceEquals(previous, x.data.PlayerSaveData["b"]), "restore previous save when disk write fails");
        Check(x.manager.DisconnectCount == 0, "failed write must not disconnect target");
        Check(x.sender.Messages.Count > 0, "report write failure");
    }

    static void TestRollbackWithoutPreviousEntry() {
        var x = NewTest();
        x.data.PlayerSaveData["a"] = new Dictionary<ushort, byte[]> { [5] = new byte[] { 1 } };
        x.manager.ShouldPersist = false;
        x.command.Execute(x.sender, new[] { "/copysave", "Alice", "Bob" });
        Check(!x.data.PlayerSaveData.ContainsKey("b"), "failed copy removes newly added entry");
    }

    static void TestSamePlayer() {
        var x = NewTest();
        x.data.PlayerSaveData["a"] = new Dictionary<ushort, byte[]> { [5] = new byte[] { 1 } };
        x.command.Execute(x.sender, new[] { "/copysave", "Alice", "Alice" });
        Check(x.manager.PersistCount == 0, "self-copy must not persist");
        Check(x.manager.DisconnectCount == 0, "self-copy must not disconnect");
    }

    static int Main() {
        try {
            TestSuccessAndDeepCopy();
            TestMissingSource();
            TestRollback();
            TestRollbackWithoutPreviousEntry();
            TestSamePlayer();
            Console.WriteLine($"PASS: {checks} production /copysave regression assertions");
            return 0;
        } catch (Exception e) {
            Console.Error.WriteLine(e);
            return 1;
        }
    }
}
