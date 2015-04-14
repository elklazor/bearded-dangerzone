using Lidgren.Network;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace NetManager
{
    enum MessageType : byte
    {
        ClientConnection = 1,
        ClientConnectionResponse,
        ClientPosition,
        ChatMessage,
        Kicked,
        ChunkRequest,
        ChunkHash,
        MapRequest,
        BlockUpdate,
        MapResponse,
        ChunkResponse,
        PlayersRequest,
        PlayersResponse
    }

    class GameServer
    {
        private NetServer netServer;
        private NetPeerConfiguration serverConfig;
        private List<string> commandQueue = new List<string>();
        private Thread serverLoopThread;
        private double nextUpdate = 1d / 30d;
        private string serverName;
        //private Dictionary<ushort, Client> clients = new Dictionary<ushort, Client>();
        private List<Message> messageQueue = new List<Message>();
        private object commandLock = new object();
        private string mapPath;
        private Map worldMap;

        public GameServer(int port)
        {
            LoadConfig();
            netServer = new NetServer(serverConfig);
            worldMap = new Map(mapPath,1000);
        }
        
        private ushort NextClientID()
        {
            for (ushort i = 1; i < netServer.Configuration.MaximumConnections; i++)
            {
                bool match = false;
                foreach (var k in worldMap.Trackables.Keys)
                {
                    if (k == i)
                        match = true;
                }
                if (!match)
                    return i;
            }
            return 0;
        }
        public void Start()
        {
            netServer.Start();
            serverLoopThread = new Thread(Loop);
            serverLoopThread.Start();
        }

        public void Input(string message)
        {
            lock (commandLock)
            {
                commandQueue.Add(message);
            }
        }

        private void Loop()
        {
            NetIncomingMessage netIn;
            NetOutgoingMessage netOut;
            short id;
            while (true)
            {
                ProcessCommands();

                while ((netIn = netServer.ReadMessage()) != null)
                {

                    switch (netIn.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            switch ((MessageType)netIn.ReadByte())
                            {
                                case MessageType.ClientConnection:
                                    ushort clientId;
                                    string clientName = netIn.ReadString();
                                    NetOutgoingMessage connectionResponse = netServer.CreateMessage();
                                    connectionResponse.Write((byte)MessageType.ClientConnectionResponse);
                                    bool validName = true;
                                    if (clientName == "" || clientName.Contains(' '))
                                        validName = false;
                                    Client locC;
                                    if((locC = worldMap.CheckClient(clientName)) != null)
                                    {
                                        worldMap.AddTrackable(locC);
                                        messageQueue.Add(new Message(clientName + "connected!", 0));
                                        connectionResponse.Write(locC.ID);
                                        connectionResponse.Write("Welcome");
                                        connectionResponse.Write(locC.Position);
                                    }
                                    else if ((clientId = NextClientID()) != 0 && validName)
                                    {
                                        worldMap.AddTrackable(new Client(clientName, clientId,netIn.SenderConnection));
                                        messageQueue.Add(new Message(clientName + " connected!", 0));
                                        connectionResponse.Write(clientId);
                                        connectionResponse.Write("Welcome");
                                        connectionResponse.Write(new Vector2(400, 40));
                                    }
                                    else if (clientId == 0)
                                    {
                                        connectionResponse.Write(0);
                                        connectionResponse.Write("Server full");
                                        messageQueue.Add(new Message("Client kicked: Server full", 0));
                                    }
                                    else
                                    {
                                        connectionResponse.Write(0);
                                        connectionResponse.Write("Invalid player name");
                                        messageQueue.Add(new Message("Client kicked: Invalid name", 0));
                                    }
                                    netServer.SendMessage(connectionResponse, netIn.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                    break;
                                case MessageType.ClientPosition:
                                    try
                                    {
                                        worldMap.Trackables[netIn.ReadUInt16()].Position = netIn.ReadVector2();
                                    }
                                    catch (Exception)
                                    { }
                                    break;
                                case MessageType.MapRequest:
                                    break;
                                case MessageType.ChunkRequest:
                                    id = netIn.ReadInt16();
                                    netOut = netServer.CreateMessage();
                                    netOut.Write((byte)MessageType.ChunkResponse);
                                    netOut.Write(id);
                                    netOut.Write(worldMap.GetChunk(id).GetChunk());
                                    netServer.SendMessage(netOut, netIn.SenderConnection, NetDeliveryMethod.ReliableUnordered);
                                    worldMap.GetChunk(id).Reserved = false;
                                    break;
                                case MessageType.ChunkHash:
                                    id = netIn.ReadInt16();
                                    netOut = netServer.CreateMessage();
                                    netOut.Write((byte)MessageType.ChunkHash);
                                    netOut.Write(id);
                                    netOut.Write(worldMap.GetChunk(id).GetHash());
                                    netServer.SendMessage(netOut, netIn.SenderConnection, NetDeliveryMethod.ReliableUnordered);
                                    worldMap.GetChunk(id).Reserved = false;
                                    break;
                                case MessageType.PlayersRequest:
                                    netOut = netServer.CreateMessage();
                                    netOut.Write((byte)MessageType.PlayersResponse);
                                    var b = worldMap.GetTrackables();
                                    netOut.Write(b.Count);
                                    foreach (var c in b)
                                    {
                                        netOut.Write(c.ID);
                                        netOut.Write(c.Name);
                                        netOut.Write(c.Health);
                                        netOut.Write(c.Position);
                                        netOut.Write(c.AnimationState);
                                        netOut.Write(c.Type);
                                    }
                                    netServer.SendMessage(netOut, netIn.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.DiscoveryRequest:
                            NetOutgoingMessage discoveryResponse = netServer.CreateMessage();
                            discoveryResponse.Write(serverName);
                            netServer.SendDiscoveryResponse(discoveryResponse, netIn.SenderEndPoint);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            if (netIn.SenderConnection.Status == NetConnectionStatus.Disconnected || netIn.SenderConnection.Status == NetConnectionStatus.Disconnecting)
                            {
                                worldMap.Trackables[netIn.ReadUInt16()].Disconnected = true;
                            }
                            break;
                        default:
                            break;
                    }
                }

                if (NetTime.Now >= nextUpdate)
                {
                    SendUpdates();
                    nextUpdate = NetTime.Now + 1d / 30d;
                }
                Thread.Sleep(1);
            }
        }

        private void SendMap(NetConnection netConnection)
        {
            NetOutgoingMessage netOut = netServer.CreateMessage();
            netOut.Write((byte)MessageType.MapResponse); 
        }
        private string GetChunkHash(short id)
        {
            return worldMap.GetChunk(id).GetHash();
        }
        private void SendUpdates()
        {
            NetOutgoingMessage netOut;
            var toEnumerate = worldMap.Trackables.Values.ToList();
            foreach (var client in toEnumerate)
            {
                foreach (var client2 in toEnumerate) 
                {
                    if (client.ID != client2.ID) 
                    {
                        netOut = netServer.CreateMessage();
                        netOut.Write((byte)MessageType.ClientPosition);
                        netOut.Write(client.ID);
                        netOut.Write(client.Position);
                        netOut.Write(client.AnimationState);
                        netOut.Write(client.Health);
                        netOut.Write(client.Disconnected);
                        netServer.SendMessage(netOut, client2.Connection, NetDeliveryMethod.UnreliableSequenced);
                    } 
                }
                foreach (var msg in messageQueue)
                {
                    netOut = netServer.CreateMessage();
                    msg.Write(netOut);
                    netServer.SendMessage(netOut, client.Connection, NetDeliveryMethod.ReliableUnordered);
                    Console.WriteLine(msg.ToString());
                }
            }
            foreach (var disc in toEnumerate)
                if (disc.Disconnected)
                    worldMap.RemoveTrackable(disc.ID);

            messageQueue.Clear();
        }
        private void ProcessCommands()
        {
            lock (commandLock)
            {
                foreach (var command in commandQueue)
                {

                    switch (command)
                    {
                        case "stop":
                            worldMap.SaveMap();
                            netServer.Shutdown("Server shutting down");
                            serverLoopThread.Abort();
                            return;
                        default:
                            //Unknown command
                            break;
                    }
                }
                commandQueue.Clear();
            }
        }
        private void LoadConfig()
        {
            XmlDocument xDoc = new XmlDocument();
            if (!File.Exists("./ServerConfig.xml"))
            {
                XmlElement xBase = xDoc.CreateElement("CONFIG");
                XmlElement xPort = xDoc.CreateElement("PORT");
                xPort.InnerText ="25452";
                XmlElement xClientCount = xDoc.CreateElement("MAXCLIENTS");
                XmlElement xServerName = xDoc.CreateElement("SERVERNAME");
                XmlElement xMapName = xDoc.CreateElement("MAPNAME");
                xMapName.InnerText = "./Map/";
                xServerName.InnerText = "Bearded server";
                xClientCount.InnerText = "2";
                xBase.AppendChild(xPort);
                xBase.AppendChild(xClientCount);
                xDoc.AppendChild(xBase);
                xDoc.AppendChild(xServerName);
                xDoc.AppendChild(xMapName);
                xDoc.Save("./ServerConfig.xml");
            }
            xDoc.Load("./ServerConfig.xml");
            serverConfig = new NetPeerConfiguration("Beard");
            serverConfig.Port = int.Parse(xDoc.SelectSingleNode("/PORT").InnerText);
            serverConfig.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            serverConfig.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            int clientCount = int.Parse(xDoc.SelectSingleNode("/MAXCLIENTS").InnerText);
            serverConfig.MaximumConnections = (clientCount > 255) ? 255 : clientCount;
            serverName = xDoc.SelectSingleNode("SERVERNAME").InnerText;
            mapPath = xDoc.SelectSingleNode("MAPNAME").InnerText;
        }
        
    }

    class Message
    {
        private string _message;
        private byte senderID;
        public Message(string message, byte id)
        {
            _message = message;
            senderID = id;
        }
        public void Write(NetOutgoingMessage netOutgoing)
        {
            netOutgoing.Write((byte)MessageType.ChatMessage);
            netOutgoing.Write(senderID);
            netOutgoing.Write(_message);
        }
        /// <summary>
        /// Returns the message
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _message;
        }
    }
}
//Move game1 logic (drawing and updating) to GameClient