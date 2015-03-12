
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Xml;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
namespace NetManagerTest
{
    enum MessageType:byte
    {
        ClientConnection = 1,
        ClientConnectionResponse,
        ClientPosition,
        ChatMessage,
        Kicked
    }
    class Client
    {
        public Vector2 Position {get;set;}
        public byte AnimationState { get; set; }
        public string Name { get; private set; }
        public ushort ID { get; private set; }
        public bool Disconnected { get; set; }
        public byte Health { get; set; }
        public NetConnection Connection { get; private set; }
        public Client(string clientName,ushort clientID,NetConnection connection)
        {
            Disconnected = false;
            ID = clientID;
            Name = clientName;
            AnimationState = 0;
            Connection = connection;
        }
    }
    class Message
    {
        private string _message;
        private byte senderID;
        public Message(string message,byte id)
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
                            netServer.SendDiscoveryResponse(discoveryResponse, netIn.SenderEndPoint);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            if (netIn.SenderConnection.Status == NetConnectionStatus.Disconnected || netIn.SenderConnection.Status == NetConnectionStatus.Disconnecting)
                            {
                                byte id;
                                if (netIn.ReadByte(out id))
                                {
                                    clients[id].Disconnected = true;
                                }
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

    class GameClient
    { 
        
    }

    class Map
    {
        private Point chunkSize
        {
            get { return chunkSize; }
            set 
            { 
                chunkSize = value;
                Chunk.ChunkSize = value;
            }
        }
        private short maxChunk;
        private short minChunk;

        private Dictionary<short,string> regions = new Dictionary<short,string>();
        private List<Chunk> activeChunks;

        public Map(string mapPath)
        {
            //Load Config
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(mapPath + "/World.xml");
            string[] sizeArr = xDoc.SelectSingleNode("CHUNKSIZE").InnerText.Split('x');
            chunkSize = new Point(int.Parse(sizeArr[0]), int.Parse(sizeArr[1]));
            maxChunk = short.Parse(xDoc.SelectSingleNode("MAXCHUNK").InnerText);
            minChunk = short.Parse(xDoc.SelectSingleNode("MINCHUNK").InnerText);
            //Identify Regions
            foreach (var region in Directory.GetFiles(mapPath + "/Regions/"))
            {
                xDoc.Load(region);
                regions.Add(short.Parse(xDoc.DocumentElement.Attributes["id"].Value),region);
            }
            activeChunks = new List<Chunk>();
            //Load Active Regions
            
        }

        private void LoadChunk(short chunkID)
        { 
            if(!regions.ContainsKey(chunkID))
            {
                if (chunkID > 0)
                {
                    
                }

                else if (chunkID < 0)
                {

                }
                else
                {
                    Chunk c = Chunk.GenerateChunk(chunkSize.Y / 2, false, true);
                    c.ID = chunkID;
                    activeChunks.Add(c);
                }
            }
        }
        private void SaveChunk(Chunk chunk)
        {

        }
        class Chunk
        {
            private Block[,] blocks;
            private List<Block> drawableBlocks;
            private List<Entity> entities;
            public static Point BlockSize = new Point(40, 40);
            public static Point ChunkSize { get; set; }
            private static Random rnd = new Random();
            private Vector2 chunkCorner;

            public short ID 
            {
                get { return ID; }
                set
                {
                    ID = value;
                    chunkCorner = new Vector2(ID * ChunkSize.X, 0);
                }
            }
            /* Air 0
             * Dirt 1
             * Grass 2
             * Stone 3
             * Spawn 4
             */
            public Chunk(Block[,] _blocks)
            {
                blocks = _blocks;
                foreach (var block in _blocks)
                {
                    if (block.ID != 0)
                    {
                        block.Position = new Vector2(block.Position.X * BlockSize.X, block.Position.Y * BlockSize.Y);
                        drawableBlocks.Add(block);
                    }
                    
                }
            }

            public void Draw(SpriteBatch spriteBatch)
            {
                foreach (var block in drawableBlocks)
                {
                    block.Draw(spriteBatch,chunkCorner);
                }
            }

            public static Chunk GenerateChunk(int lastY,bool negative, bool first = false)
            {
                Block[,] chunk = new Block[ChunkSize.X, ChunkSize.Y];
                if (!first)
                {
                    int nextY = 0;
                    int prevY = lastY;
                    bool up = rnd.Next(0, 2) == 1;
                    int changeInRow = 0;
                    int nextChange = 0;
                    bool wasFlat = false;
                    if (!negative)
                    {
                        for (int x = 0; x < ChunkSize.X; x++)
                        {
                            //Elevation change logic
                            if(changeInRow == 0)
                            {
                                up = !up;
                                if (up)
                                {
                                    while (prevY + (changeInRow = rnd.Next(0, 6)) <= 0) ;
                                    nextChange = 1;
                                }
                                else
                                {
                                    while (prevY - (changeInRow = rnd.Next(0, 6)) >= ChunkSize.Y) ;
                                    nextChange = -1;
                                }

                                if (!wasFlat)
                                {
                                    changeInRow = rnd.Next(2, 7);
                                    nextChange = 0;
                                    wasFlat = true;
                                }
                                else
                                    wasFlat = false;

                                nextY = prevY + (nextChange * changeInRow);
                            }
                            changeInRow--;
                            for (int y = 0; y < ChunkSize.Y; y++)
                            {
                                if (y == prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 2);
                                else if (y < prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 0);
                                else
                                    chunk[x, y] = new Block(new Vector2(x, y), (byte)((y > prevY + nextChange + 3) ? 3 : 1));
                                
                            }
                            prevY = nextY;
                        }
                        return new Chunk(chunk);
                    }
                    else
                    {
                        for (int x = ChunkSize.X -1; x >= 0; x--)
                        {
                            //Elevation change logic
                            if (changeInRow == 0)
                            {
                                up = !up;
                                if (up)
                                {
                                    while (prevY + (changeInRow = rnd.Next(0, 6)) <= 0) ;
                                    nextChange = 1;
                                }
                                else
                                {
                                    while (prevY - (changeInRow = rnd.Next(0, 6)) >= ChunkSize.Y) ;
                                    nextChange = -1;
                                }

                                if (!wasFlat)
                                {
                                    changeInRow = rnd.Next(2, 7);
                                    nextChange = 0;
                                    wasFlat = true;
                                }
                                else
                                    wasFlat = false;

                                nextY = prevY + (nextChange * changeInRow);
                            }
                            changeInRow--;
                            for (int y = 0; y < ChunkSize.Y; y++)
                            {
                                if (y == prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 2);
                                else if (y < prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 0);
                                else
                                    chunk[x, y] = new Block(new Vector2(x, y), (byte)((y > prevY + nextChange + 3) ? 3 : 1));

                            }
                            prevY = nextY;
                        }
                        return new Chunk(chunk);
                    }
                }
                else
                {
                    for (int y = 0; y < ChunkSize.Y; y++)
                    {
                        for (int x = 0; x < ChunkSize.X; x++)
                        {
                            if (y < lastY)
                                chunk[x, y] = new Block(new Vector2(x,y), 0);
                            else if (y == lastY)
                            {
                                if(x < ChunkSize.X/2-2 && x > ChunkSize.X/2+2)
                                    chunk[x, y] = new Block(new Vector2(x, y), 2);
                                else
                                    chunk[x, y] = new Block(new Vector2(x, y), 4);
                            }   
                            else
                            {
                                chunk[x, y] = new Block(new Vector2(x, y), (byte)((y > lastY + 3) ? 3 : 1));
                            }
                        }
                    }
                    return new Chunk(chunk);
                }
            }
            
            class Entity
            {

            }

        }
        class Block
        {
            public Vector2 Position { get; set; }
            public byte ID { get; set; }
            private Rectangle source;
            public Block(Vector2 position, byte id)
            {
                Position = position;
                ID = id;
                source = TextureManager.GetSourceRectangle(id);
            }

            public void Draw(SpriteBatch spriteBatch, Vector2 chunkOffset)
            {
                spriteBatch.Draw(TextureManager.SpriteSheet, chunkOffset + Position, source, Color.White);
            }
        }
    }
    static class TextureManager
    {
        private static Dictionary<byte, Rectangle> textureData = new Dictionary<byte, Rectangle>();
        public static Texture2D SpriteSheet;
        public static void Load(ContentManager content)
        {
            SpriteSheet = content.Load<Texture2D>("Sheet");
            textureData.Add(1, new Rectangle(0, 0, 40, 40));
            textureData.Add(2, new Rectangle(40, 0, 40, 40));
            textureData.Add(3, new Rectangle(40, 0, 80, 40));
            textureData.Add(4, new Rectangle(40, 0, 120, 40));
        }
        public static Rectangle GetSourceRectangle(byte id)
        {
            return textureData[id];
        }
    }
}
