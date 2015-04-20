using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Lidgren.Network.Xna;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
namespace NetManager
{
    partial class GameClient
    {
        private Map worldMap;
        private NetClient netClient;
        private Thread loopThread;
        private ConcurrentQueue<Message> chatQueue = new ConcurrentQueue<Message>();
        private ConcurrentDictionary<ushort, Client> clients = new ConcurrentDictionary<ushort, Client>();
        private Player localPlayer;
        private string name;
        public bool Initialized { get; set; }
        public GameClient(int port, string ip,string playerName)
        {
            name = playerName;
            NetPeerConfiguration config = new NetPeerConfiguration("Beard");
            //config.Port = port; --Auto assign the port instead, to avoid conflicts
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            netClient = new NetClient(config);
            netClient.Start();
            netClient.DiscoverKnownPeer(ip, port);
            chatQueue.Enqueue(new Message("Sending discovery request", 0));
            loopThread = new Thread(new ThreadStart(Loop));
            loopThread.Start();
            
        }

        private void Loop()
        {
            NetIncomingMessage netIn;
            NetOutgoingMessage netOut;
            while (true)
            {
                while ((netIn = netClient.ReadMessage()) != null)
                {
                    switch (netIn.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            HandleData(netIn);
                            break;
                        case NetIncomingMessageType.DiscoveryResponse:
                            chatQueue.Enqueue(new Message("Connecting to " + netIn.ReadString(), 0));
                            netClient.Connect(netIn.SenderEndPoint);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            if (netClient.ConnectionStatus == NetConnectionStatus.Connected)
                            {
                                netOut = netClient.CreateMessage();
                                netOut.Write((byte)MessageType.ClientConnection);
                                netOut.Write(name);
                                netClient.SendMessage(netOut, NetDeliveryMethod.ReliableOrdered);
                                netOut = netClient.CreateMessage();
                                netOut.Write((byte)MessageType.PlayersRequest);
                                netClient.SendMessage(netOut, NetDeliveryMethod.ReliableOrdered);
                                netOut = netClient.CreateMessage();
                                netOut.Write((byte)MessageType.MapRequest);
                                netClient.SendMessage(netOut, NetDeliveryMethod.ReliableOrdered);
                            }
                            break;
                        default:
                            break;
                    }
                }
                foreach (var s in chatQueue.ToList())
                {
                    Console.WriteLine(s.ToString());
                }
                chatQueue = new ConcurrentQueue<Message>(); ///Bad Solution
                lock (Map.RegionLock)
                {
                    var enu = worldMap.GetRequestedChunks();
                    foreach (var cnk in enu)
                    {
                        if (cnk.Value == true)
                            continue;
                        netOut = netClient.CreateMessage();
                        netOut.Write((byte)MessageType.ChunkRequest);
                        netOut.Write(cnk.Key);
                        enu[cnk.Key] = true;
                        netClient.SendMessage(netOut, NetDeliveryMethod.ReliableOrdered);
                    } 
                }
            }
        }

        private void HandleData(NetIncomingMessage netIn)
        {
            ushort id;
            Client c;
            switch ((MessageType)netIn.ReadByte())
            {
                case MessageType.ClientConnection:
                    break;
                case MessageType.ClientConnectionResponse:
                    id = netIn.ReadUInt16();
                    if (id != 0)
                    {
                        chatQueue.Enqueue(new Message(netIn.ReadString(), 0));
                        localPlayer = new Player();
                        localPlayer.SetAllFields(netIn.ReadVector2(), name, id, 100);
                    }
                    else
                    {
                        chatQueue.Enqueue(new Message(netIn.ReadString(), 0));
                    }
                    break;
                case MessageType.ClientPosition:
                    id = netIn.ReadUInt16();
                    if (clients.ContainsKey(id))
                    {
                        clients[id].Position = netIn.ReadVector2();
                        clients[id].AnimationState = netIn.ReadByte();
                        clients[id].Health = netIn.ReadByte();
                        if (netIn.ReadBoolean())
                            clients.TryRemove(id, out c);
                    }
                    else
                    {
                        chatQueue.Enqueue(new Message("Got invalid id from server", 0));    
                    }
                    break;
                case MessageType.ChatMessage:
                    break;
                case MessageType.Kicked:
                    break;
                case MessageType.ChunkRequest:
                    break;
                case MessageType.ChunkHash:
                    break;
                case MessageType.MapRequest:
                    break;
                case MessageType.BlockUpdate:
                    break;
                case MessageType.MapResponse:
                    worldMap = new Map(netIn.ReadString());
                    worldMap.SetPlayer(localPlayer);
                    Initialized = true;
                    break;
                case MessageType.ChunkResponse:
                    //ID,string
                    string data = netIn.ReadString();
                    short chunkId = netIn.ReadInt16();
                    worldMap.AddChunk(Chunk.GetChunkFromString(data, chunkId));
                    break;
                case MessageType.PlayersResponse:
                    int length = netIn.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        c = new Client();
                        c.ID = netIn.ReadUInt16();
                        c.Name = netIn.ReadString();
                        c.Position = netIn.ReadVector2();
                        c.AnimationState = netIn.ReadByte();
                        c.Type = netIn.ReadByte();
                        if(c.ID != localPlayer.ID)
                            clients.TryAdd(c.ID, c);
                    }
                    break;
                default:
                    break;
            }
        } 
        private void DrawClient(Client client, SpriteBatch spriteBatch)
        { }
        

    }
    class Player:Client
    {
        public bool Initialized { get; set; }
        public void SetAllFields(Vector2 position, string name,ushort id,byte health)
        {
            Position = position;
            Name = name;
            ID = id;
            Health = health;
            Initialized = true;
        }


        internal void Velocity(Vector2 velocity)
        {
            Position += velocity;
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(TextureManager.SpriteSheet, TextureManager.GetSourceRectangle(5), Color.White);
        }
    }
    

}
