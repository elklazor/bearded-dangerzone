using Lidgren.Network;
using Lidgren.Network.Xna;
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
        Kicked
    }

    class GameServer
    {
        private NetServer netServer;
        private NetPeerConfiguration serverConfig;
        private List<string> commandQueue = new List<string>();
        private Thread serverLoopThread;
        private double nextUpdate = 1d / 30d;
        private string serverName;
        private Dictionary<ushort, Client> clients = new Dictionary<ushort, Client>();
        private List<Message> messageQueue = new List<Message>();
     
        public GameServer(int port)
        {
            LoadConfig();
            netServer = new NetServer(serverConfig);
        }
        
        private ushort NextClientID()
        {
            for (ushort i = 1; i < netServer.Configuration.MaximumConnections; i++)
            {
                bool match = false;
                foreach (var k in clients.Keys)
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
            lock (commandQueue)
            {
                commandQueue.Add(message);
            }
        }

        private void Loop()
        {
            NetIncomingMessage netIn;
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

                                    if ((clientId = NextClientID()) != 0 && validName)
                                    {
                                        clients.Add(clientId, new Client(clientName, clientId,netIn.SenderConnection));
                                        messageQueue.Add(new Message(clientName + " connected!", 0));
                                        connectionResponse.Write(clientId);
                                        connectionResponse.Write("Welcome");
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
                                        clients[netIn.ReadUInt16()].Position = netIn.ReadVector2();
                                    }
                                    catch (Exception)
                                    { }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.DiscoveryRequest:
                            NetOutgoingMessage discoveryResponse = netServer.CreateMessage();
                            discoveryResponse.Write(serverName);
                            netServer.SendDiscoveryResponse(discoveryResponse, netIn.SenderEndpoint);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            if (netIn.SenderConnection.Status == NetConnectionStatus.Disconnected || netIn.SenderConnection.Status == NetConnectionStatus.Disconnecting)
                            {
                                clients[netIn.ReadUInt16()].Disconnected = true;
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
        private void SendUpdates()
        {
            NetOutgoingMessage netOut;
            foreach (var client in clients.Values)
            {
                foreach (var client2 in clients.Values)
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
            }
        }
        private void ProcessCommands()
        {
            lock (commandQueue)
            {
                foreach (var command in commandQueue)
                {

                    switch (command)
                    {
                        case "stop":
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
                xServerName.InnerText = "Bearded server";
                xClientCount.InnerText = "2";
                xBase.AppendChild(xPort);
                xBase.AppendChild(xClientCount);
                xDoc.AppendChild(xBase);
                xDoc.AppendChild(xServerName);
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
    }
}

  
