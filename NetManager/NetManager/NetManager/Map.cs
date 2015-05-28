using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using Lidgren.Network;
using System.Xml.Linq;
using NetManager.Environment;

namespace NetManager
{

    class Map
    {
        private Point chunkSize;
        public static object RegionLock = new object();
        private short maxChunk;
        private short minChunk;
        private ConcurrentDictionary<short, string> regions = new ConcurrentDictionary<short, string>();
        internal ConcurrentDictionary<short,Chunk> activeChunks = new ConcurrentDictionary<short, Chunk>();
        private ConcurrentDictionary<ushort, Client> chunkLoaders = new ConcurrentDictionary<ushort, Client>();
        private ConcurrentDictionary<short, Chunk> allChunks = new ConcurrentDictionary<short, Chunk>();
        public static object CollisionBlocksLock = new object();
        internal List<Block> CollisionBlocks = new List<Block>();
        private string baseRegionPath;
        private TimerCallback chunkCallback;
        private Timer chunkManagerTimer;
        private List<short> chunkManagerChunks = new List<short>();
        private readonly bool isClient;
        private string pathToMap;
        private Dictionary<short,bool> requestedChunks = new Dictionary<short,bool>();
        private Player localPlayer;
        public string MapConfig { get; set; }
        private List<Cloud> cloudsList = new List<Cloud>();
        Random rnd = new Random();

        public ConcurrentDictionary<ushort, Client> Trackables
        {
            get { return chunkLoaders; }
        }
        
        public void AddChunk(Chunk c)
        {
            //lock (RegionLock)
            //{
                activeChunks.TryAdd(c.ID, c);
                if (requestedChunks.Keys.Contains(c.ID))
                    requestedChunks.Remove(c.ID); 
                if(allChunks.ContainsKey(c.ID))
                    allChunks[c.ID] = c;
                else
                    allChunks.TryAdd(c.ID,c);
            //}
        }
        public Dictionary<short,bool> GetRequestedChunks()
        {
            return requestedChunks;
        }
        /// <summary>
        /// Use only for server
        /// </summary>
        /// <param name="mapPath"></param>
        /// <param name="mapManagerTimer"></param>
        /// <param name="client"></param>
        public Map(string mapPath,int mapManagerTimer)
        {
            isClient = false;
            //Load Config
            
            if (!Directory.Exists(mapPath) || !Directory.Exists(mapPath + "Regions/"))
                Directory.CreateDirectory(mapPath + "Regions/");

            pathToMap = mapPath;
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(mapPath + "World.xml");
            string[] sizeArr = xDoc.SelectSingleNode("WORLD/CHUNKSIZE").InnerText.Split('x');
            chunkSize = new Point(int.Parse(sizeArr[0]), int.Parse(sizeArr[1]));
            maxChunk = short.Parse(xDoc.SelectSingleNode("WORLD/MAXCHUNK").InnerText);
            minChunk = short.Parse(xDoc.SelectSingleNode("WORLD/MINCHUNK").InnerText);
            Chunk.ChunkSize = chunkSize;
            MapConfig = xDoc.OuterXml;
            //Identify Regions
            baseRegionPath = mapPath + "Regions/";
            foreach (var region in Directory.GetFiles(mapPath + "Regions/"))
            {
                xDoc.Load(region);
                regions.TryAdd(short.Parse(xDoc.DocumentElement.Attributes["id"].Value), region);
            }
            Client track;
            xDoc.Load(mapPath + "Players.xml");
            foreach (XmlNode iTrack in xDoc.SelectNodes("TRACKABLE"))
            {
                track = new Client();
                string[] sPos = iTrack.SelectSingleNode("POSITION").InnerText.Split('x');
                ushort id = ushort.Parse(iTrack.SelectSingleNode("ID").InnerText);
                track.SetAll(iTrack.SelectSingleNode("NAME").InnerText, id , new Vector2(float.Parse(sPos[0]), float.Parse(sPos[1])), byte.Parse(iTrack.SelectSingleNode("TYPE").InnerText));
                chunkLoaders.TryAdd(id,track);
            }
            chunkCallback = new TimerCallback(ManageChunks);
            chunkManagerTimer = new Timer(chunkCallback, null, 0, mapManagerTimer);
            //Load Active Regions
            LoadChunk(0);
        }
        public Map(string worldConfig)
        {
            isClient = true;
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(worldConfig);
            string[] sizeArr = xDoc.SelectSingleNode("WORLD/CHUNKSIZE").InnerText.Split('x');
            chunkSize = new Point(int.Parse(sizeArr[0]), int.Parse(sizeArr[1]));
            maxChunk = short.Parse(xDoc.SelectSingleNode("WORLD/MAXCHUNK").InnerText);
            minChunk = short.Parse(xDoc.SelectSingleNode("WORLD/MINCHUNK").InnerText);
            Chunk.ChunkSize = chunkSize;
            chunkCallback = new TimerCallback(ManageChunks);
            chunkManagerTimer = new Timer(chunkCallback, null, 0, 2000);
        }
        public void SetPlayer(Player c)
        {
            localPlayer = c;
            localPlayer.MapRef = this;
        }
        public Player LocalPlayer
        {
            get { return localPlayer; }
        }
        public List<Client> GetTrackables()
        {
            return chunkLoaders.Values.ToList();
        }
        public void SaveMap()
        {
            foreach (var chunk in activeChunks.Values)
            {
                SaveChunk(chunk);
            }
            //XmlDocument xDoc = new XmlDocument();
            //xDoc.Load(pathToMap + "Players.xml");
            ////GetList of ID's
            //Dictionary<ushort, XmlNode> nodes = new Dictionary<ushort, XmlNode>();
            //foreach (XmlNode id in xDoc.SelectNodes("TRACKABLE"))
            //{

            //}

            //foreach (var p in chunkLoaders)
            //{

            //}
        }
        public Chunk GetChunk(short id)
        {
            LoadChunk(id);
            activeChunks[id].Reserved = true;
            return activeChunks[id];
        }
        public void AddTrackable(Client toTrack)
        {
            chunkLoaders.TryAdd(toTrack.ID,toTrack);
        }
        public void RemoveTrackable(Client toRemove)
        {
            Client itr;
            chunkLoaders.TryRemove(toRemove.ID, out itr);
        }
        public void RemoveTrackable(ushort id)
        {
            Client itr;
            chunkLoaders.TryRemove(id, out itr);
        }
        public Client CheckClient(string name) 
        {
            var e = chunkLoaders.Values.Where(x => x.Name == name);
            if (e.Count() != 0)
            {
                return e.ToList()[0];
            }
            else return null;
        }
        
        /// <summary>
        /// Used on the server
        /// </summary>
        /// <param name="chunkID"></param>
        /// <param name="force"></param>
        private void LoadChunk(short chunkID,bool force = false)
        {
            
            if (isClient)
            {
                ClientLoadChunk(chunkID);
                return;
            }

            if (!regions.ContainsKey(chunkID) || force)
            {
                if (chunkID > 0)
                {
                    LoadChunk((short)(chunkID - 1));
                    //LastY

                    Chunk c = Chunk.GenerateChunk(activeChunks[(short)(chunkID-1)].GetY(false), false,chunkID,false);
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID,c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".xml");
                }

                else if (chunkID < 0)
                {
                    LoadChunk((short)(chunkID + 1));
                    //LastY
                    Chunk c = Chunk.GenerateChunk(activeChunks[(short)(chunkID+1)].GetY(true), true, chunkID,false);
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID, c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".xml");
                }
                else
                {
                    Chunk c = Chunk.GenerateChunk(chunkSize.Y / 2, false,chunkID, true);
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID, c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".xml");
                }
            }
            else
            {
                if (!activeChunks.ContainsKey(chunkID))
                { 
                    //Needs to be loaded
                    
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(regions[chunkID]);
                    string data;
                    Block[,] chunk;
                    string[] rows = new string[1], columns;
                    data = xDoc.SelectSingleNode("CHUNK/MAP").InnerText;
                    if (!string.IsNullOrEmpty(data))
                    {
                        columns = data.Split('|');
                    }
                    else
                    {
                        LoadChunk(chunkID, true);
                        return;
                    }
                    chunk = new Block[32, 32];

                    for (int y = 0; y < columns.Length; y++)
                    {
                        rows = columns[y].Split(',');
                        for (int x = 0; x <  rows.Length-1; x++)
                        {
                            chunk[x, y] = new Block(new Vector2(x, y), byte.Parse(rows[x]));
                        }
                    }
                    Chunk c = new Chunk(chunk,chunkID);
                    activeChunks.TryAdd(chunkID, c);
                }
                //Chunk already loaded
            }
        }
        /// <summary>
        /// Used on the client
        /// </summary>
        /// <param name="chunkID"></param>
        private void ClientLoadChunk(short chunkID)
        {
            lock (RegionLock)
            {
                if (!allChunks.ContainsKey(chunkID))
                {
                    if (!requestedChunks.Keys.Contains(chunkID))
                    {
                        requestedChunks.Add(chunkID, false);
                    }
                }
                else
                {
                    AddChunk(allChunks[chunkID]);
                }
                
            }
        }
        private void ManageChunks(object stateInfo)
        {
            chunkManagerChunks.Clear();
            //Get all chunks that needs to be loaded
            if (!isClient)
            {
                foreach (var loader in chunkLoaders)
                {
                    chunkManagerChunks.AddRange(GetChunks(loader.Value));
                }
            }
            else
            {
                if (localPlayer != null)
                {
                    chunkManagerChunks.AddRange(GetChunks(localPlayer));
                }
            }
            //Check if some chunks are not loaded, or if some chunks that shouldn't be loaded are loaded
            var toUnload = activeChunks.Keys.Where(x => activeChunks[x].Reserved == false).Except(chunkManagerChunks);
            foreach (short ch in toUnload)
            {
                if(ch != 0)
                    UnloadChunk(ch);
            }
            foreach (var toLoad in chunkManagerChunks)
            {
                LoadChunk(toLoad);
            }
            
        }

        private void UnloadChunk(short id)
        {
            Chunk c;
            SaveChunk(activeChunks[id]);
            activeChunks.TryRemove(id, out c);
        }

        private void SaveChunk(Chunk toSave)
        {
            if (!isClient)
            {
                XmlDocument xDoc = new XmlDocument();
                XmlNode xBase = xDoc.CreateElement("CHUNK");
                XmlNode xMap = xDoc.CreateElement("MAP");
                XmlNode xEnt = xDoc.CreateElement("ENTITES");

                xMap.InnerText = toSave.GetChunk();
                xBase.AppendChild(xMap);
                XmlAttribute xAtt = xDoc.CreateAttribute("id");
                xAtt.Value = toSave.ID.ToString();
                xBase.Attributes.Append(xAtt);
                xBase.AppendChild(xEnt);
                xDoc.AppendChild(xBase);
                xDoc.Save(baseRegionPath + toSave.ID + ".xml"); 
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            
            
            foreach (var chunk in activeChunks.Values)
            {
                chunk.Draw(spriteBatch);
            }
            foreach (var cloud in cloudsList)
            {
                cloud.Draw(spriteBatch);
            }
        }

        public short[] GetChunks(Client itr)
        {
            short[] shrtArr = new short[3];
            shrtArr[2] = (short)Math.Floor(itr.Position.X / (chunkSize.X * 40));
            shrtArr[0] = (short)(shrtArr[2] + 1);
            shrtArr[1] = (short)(shrtArr[2] - 1);

            return shrtArr;
        }
        private float lastCloudSpawned = 0;
        internal void Update(GameTime gameTime)
        {
            lastCloudSpawned += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if(localPlayer != null)
                if (localPlayer.Initialized)
                {
                    localPlayer.Update(gameTime);
                }

            if (lastCloudSpawned >= 3000)
            {
                lastCloudSpawned = 0;
                if (cloudsList.Count < 10)
                {
                    cloudsList.Add(new Cloud(new Vector2(localPlayer.Position.X + 400, rnd.Next(200, 300)), rnd.Next(0, 4),(float)(rnd.NextDouble()+1),rnd.Next(1,3)));
                }
            }
            foreach (var cloud in cloudsList.ToList())
            {
                cloud.Update(gameTime);
                if (Vector2.Distance(cloud.Position, localPlayer.Position) > 1000)
                    cloudsList.Remove(cloud);
            }
        }

        internal bool SendPlayer(NetOutgoingMessage netOut)
        {
            if (localPlayer.NeedUpdate)
            {
                localPlayer.DumpPlayerData(netOut);
                return true;
            }
            else return false;
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
            textureData.Add(3, new Rectangle(80, 0, 40, 40));
            textureData.Add(4, new Rectangle(120, 0, 40, 40));
            textureData.Add(5, new Rectangle(0, 80, 40, 80));
            textureData.Add(6, new Rectangle(0, 40, 40, 40));
            
            Sky = content.Load<Texture2D>("AllClouds");
        }
        public static Rectangle GetSourceRectangle(byte id)
        {
            return textureData[id];
        }

        public static Texture2D Sky { get; set; }
    }
}
//160 55
//160 + 55 50 30
//