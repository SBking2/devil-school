using System.Threading.Tasks;
using System.Text;
using Godot;

namespace EGame
{
    public class NetTransportTestCmd : AbstractConsoleCmd
    {
        private class TestHandler : INetHostHandler, INetClientHandler
        {
            private readonly Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);

            public int HostConnectedCount { get; private set; }
            public int HostDisconnectedCount { get; private set; }
            public int ClientConnectedCount { get; private set; }
            public int ClientDisconnectedCount { get; private set; }
            public int PacketReceivedCount { get; private set; }
            public int PacketsFromHostCount { get; private set; }
            public int PacketsFromClientCount { get; private set; }

            public void OnClientConnected()
            {
                HostConnectedCount++;
                _Logger.Debug("[nettest] host received client connected");
            }

            public void OnClientDisconnected()
            {
                HostDisconnectedCount++;
                _Logger.Debug("[nettest] host received client disconnected");
            }

            public void OnConnected()
            {
                ClientConnectedCount++;
                _Logger.Debug("[nettest] client connected to host");
            }

            public void OnDisconnected()
            {
                ClientDisconnectedCount++;
                _Logger.Debug("[nettest] client disconnected from host");
            }

            public void OnPacketReceived(ulong sender_id, byte[] data)
            {
                PacketReceivedCount++;
                string message = Encoding.UTF8.GetString(data);
                if(sender_id == 1uL)
                    PacketsFromHostCount++;
                else
                    PacketsFromClientCount++;

                _Logger.Debug($"[nettest] packet received from {sender_id}: {message}");
            }
        }

        public override string CmdName => "nettest";

        public override string Args => "";

        public override bool DebugOnly => true;

        public override CmdResult Execute(string[] args)
        {
            return new CmdResult(true, "Started ENet transport test. Check Godot output for details.", RunTest());
        }

        private async Task RunTest()
        {
            Log.Logger logger = new Log.Logger(Log.LogType.NetWork);
            TestHandler handler = new TestHandler();
            ENetHost host = new ENetHost(handler);
            ENetClient client = new ENetClient(handler);

            Error hostResult = host.StartHost();
            if(hostResult != Error.Ok)
            {
                logger.Error($"[nettest] failed to start host: {hostResult}");
                return;
            }

            logger.Debug("[nettest] host started on 127.0.0.1:8080");
            Task<ENetConnectResult> connectTask = client.ConnectToHost(2uL);

            int totalDelay = 0;
            while(!connectTask.IsCompleted && totalDelay <= 12000)
            {
                host.Update();
                await Task.Delay(16);
                totalDelay += 16;
            }

            if(!connectTask.IsCompleted)
            {
                logger.Error("[nettest] client connect task did not finish");
                client.DisConnectFromHost(true);
                host.StopHost();
                return;
            }

            ENetConnectResult connectResult = await connectTask;
            host.Update();
            if(connectResult != ENetConnectResult.Success)
            {
                logger.Error($"[nettest] client connect failed: {connectResult}");
                host.StopHost();
                return;
            }

            logger.Debug("[nettest] connect and handshake succeeded");

            client.SendMessage(Encoding.UTF8.GetBytes("client-to-host"));
            host.SendMessage(2uL, Encoding.UTF8.GetBytes("host-to-client"));
            host.SendMessageAll(Encoding.UTF8.GetBytes("host-broadcast"));

            for(int i = 0; i < 60 && handler.PacketReceivedCount < 3; i++)
            {
                host.Update();
                client.Update();
                await Task.Delay(16);
            }

            bool receivedExpectedPackets =
                handler.PacketReceivedCount == 3 &&
                handler.PacketsFromHostCount == 2 &&
                handler.PacketsFromClientCount == 1;

            if(receivedExpectedPackets)
                logger.Debug("[nettest] app message send/receive succeeded");
            else
                logger.Error(
                    "[nettest] app message test failed. " +
                    $"packets: {handler.PacketReceivedCount}, " +
                    $"from host: {handler.PacketsFromHostCount}, " +
                    $"from client: {handler.PacketsFromClientCount}"
                );

            client.DisConnectFromHost(false);
            for(int i = 0; i < 30; i++)
            {
                host.Update();
                await Task.Delay(16);
            }

            host.StopHost();
            logger.Debug(
                "[nettest] finished. " +
                $"host connected: {handler.HostConnectedCount}, " +
                $"host disconnected: {handler.HostDisconnectedCount}, " +
                $"client connected: {handler.ClientConnectedCount}, " +
                $"client disconnected: {handler.ClientDisconnectedCount}, " +
                $"packets: {handler.PacketReceivedCount}, " +
                $"from host: {handler.PacketsFromHostCount}, " +
                $"from client: {handler.PacketsFromClientCount}"
            );
        }
    }
}
