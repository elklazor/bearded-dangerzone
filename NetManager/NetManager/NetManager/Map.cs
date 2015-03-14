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

namespace NetManager
{
    interface ITrackable
    {
        Vector2 Position { get; set; }
        ushort ID { get; set; }
    }
    class Map
    {
        private Point chunkSize;
        
        private short maxChunk;
        private short minChunk;
        private ConcurrentDictionary<short, string> regions = new ConcurrentDictionary<short, string>();
        private ConcurrentDictionary<short,Chunk> activeChunks = new ConcurrentDictionary<short, Chunk>();
        private ConcurrentDictionary<ushort, ITrackable> chunkLoaders = new ConcurrentDictionary<ushort, ITrackable>();
        private string baseRegionPath;
        private TimerCallback chunkCallback;
        private Timer chunkManagerTimer;

        public Map(string mapPath)
        {
            //Load Config
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(mapPath + "World.xml");
            string[] sizeArr = xDoc.SelectSingleNode("WORLD/CHUNKSIZE").InnerText.Split('x');
            chunkSize = new Point(int.Parse(sizeArr[0]), int.Parse(sizeArr[1]));
            maxChunk = short.Parse(xDoc.SelectSingleNode("WORLD/MAXCHUNK").InnerText);
            minChunk = short.Parse(xDoc.SelectSingleNode("WORLD/MINCHUNK").InnerText);
            Chunk.ChunkSize = chunkSize;
            //Identify Regions
            baseRegionPath = mapPath + "Regions/";
            foreach (var region in Directory.GetFiles(mapPath + "Regions/"))
            {
                xDoc.Load(region);
                regions.TryAdd(short.Parse(xDoc.DocumentElement.Attributes["id"].Value), region);
            }
            chunkCallback = new TimerCallback(ManageChunks);
            chunkManagerTimer = new Timer(chunkCallback, null, 0, 500);
            //Load Active Regions
            LoadChunk(0);
        }
        /// <summary>
        /// Testing only
        /// </summary>
        public Map()
        {
            chunkSize = new Point(32, 32);
            Chunk.ChunkSize = chunkSize;
            LoadChunk(0);
            LoadChunk(-1);
            LoadChunk(1);
            LoadChunk(2);
            LoadChunk(3);
            LoadChunk(4);
        }
        public void SaveMap()
        {
            foreach (var chunk in activeChunks.Values)
            {
                SaveChunk(chunk);
            }
        }
        public void AddTrackable(ITrackable toTrack)
        {
            chunkLoaders.TryAdd(toTrack.ID,toTrack);
        }
        public void RemoveTrackable(ITrackable toRemove)
        {
            ITrackable itr;
            chunkLoaders.TryRemove(toRemove.ID, out itr);
        }
        public void RemoveTrackable(ushort id)
        {
            ITrackable itr;
            chunkLoaders.TryRemove(id, out itr);
        }

        private void LoadChunk(short chunkID,bool force = false)
        {
            if (!regions.ContainsKey(chunkID) || force)
            {
                if (chunkID > 0)
                {
                    LoadChunk((short)(chunkID - 1));
                    //LastY
                    Chunk c = Chunk.GenerateChunk(activeChunks.Values.First(x => x.ID == (chunkID - 1)).GetY(false), false);
                    Console.WriteLine(c.GetY(false));
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID,c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".xml");
                }

                else if (chunkID < 0)
                {
                    LoadChunk((short)(chunkID + 1));
                    //LastY
                    Chunk c = Chunk.GenerateChunk(activeChunks.Values.First(x => x.ID == (chunkID + 1)).GetY(true), true);
                    Console.WriteLine(c.GetY(true));
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID, c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".xml");
                }
                else
                {
                    Chunk c = Chunk.GenerateChunk(chunkSize.Y / 2, false, true);
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
                    Chunk c = new Chunk(chunk);
                    c.ID = chunkID;
                    activeChunks.TryAdd(chunkID, c);
                }
                //Chunk already loaded
            }
        }
        private List<short> chunkManagerChunks = new List<short>();
        private void ManageChunks(object stateInfo)
        {
            chunkManagerChunks.Clear();
            //Get all chunks that needs to be loaded
            foreach (var loader in chunkLoaders)
            {
                chunkManagerChunks.AddRange(GetChunks(loader.Value));
            }
            //Check if some chunks are not loaded, or if some chunks that shouldn't be loaded are loaded
            var toUnload = activeChunks.Keys.Except(chunkManagerChunks);
            foreach (var ch in toUnload)
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

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var chunk in activeChunks.Values)
            {
                chunk.Draw(spriteBatch);
            }
        }

        private short[] GetChunks(ITrackable itr)
        {
            short[] shrtArr = new short[3];
            shrtArr[2] = (short)Math.Floor(itr.Position.X / (chunkSize.X * 40));
            shrtArr[0] = (short)(shrtArr[2] + 1);
            shrtArr[1] = (short)(shrtArr[2] - 1);

            return shrtArr;
        }

        class Chunk
        {
            private Block[,] blocks;
            private List<Block> drawableBlocks = new List<Block>();
            private List<Entity> entities;
            public static Point BlockSize = new Point(40, 40);
            public static Point ChunkSize { get; set; }
            private static Random rnd = new Random();
            private Vector2 chunkCorner;
            private short id;
            public short ID
            {
                get { return id; }
                set
                {
                    id = value;
                    chunkCorner = new Vector2(id * ChunkSize.X * BlockSize.X, 0);
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
            public int GetY(bool left)
            {
                if (!left)
                {
                    for (int i = 0; i < ChunkSize.Y; i++)
                    {
                        if (blocks[ChunkSize.X - 1, i].ID != 0)
                            return i;
                    }
                    return 0;
                }
                else
                {
                    for (int i = 0; i < ChunkSize.Y; i++)
                    {
                        if (blocks[0, i].ID != 0)
                            return i;
                    }
                    return 0;
                }

            }
            public void Draw(SpriteBatch spriteBatch)
            {
                foreach (var block in drawableBlocks)
                {
                    block.Draw(spriteBatch, chunkCorner);
                }
            }
            public string GetChunk()
            {
                StringBuilder sb = new StringBuilder();
                for (int y = 0; y < ChunkSize.Y; y++)
                {
                    string row = "";
                    for (int x = 0; x < ChunkSize.X; x++)
                    {
                        row += blocks[x, y].ID.ToString() + ",";
                    }
                    row.Remove(row.Length - 1, 1);
                    row += "|";
                    sb.Append(row);
                }
                return sb.ToString();
            }
            public static Chunk GenerateChunk(int lastY, bool negative, bool first = false)
            {
                Block[,] chunk = new Block[ChunkSize.X, ChunkSize.Y];
                if (!first)
                {
                    int nextY = 0;
                    int prevY = lastY;
                    bool up = rnd.Next(0, 2) == 1;
                    int changeInRow = 0;
                    int nextChange = 0;
                    bool wasFlat = true;
                    int maxChange = 4;
                    if (!negative)
                    {
                        for (int x = 0; x < ChunkSize.X; x++)
                        {
                            //Elevation change logic
                            if (changeInRow == 0)
                            {
                                if(wasFlat)
                                    up = !up;

                                if (!up)
                                {
                                    while (prevY + (changeInRow = rnd.Next(1, maxChange)) <= 3) ;
                                    nextChange = 1;
                                }
                                else
                                {
                                    while (prevY - (changeInRow = rnd.Next(1, maxChange)) >= ChunkSize.Y -4) ;
                                    nextChange = -1;
                                }

                                if (!wasFlat)
                                {
                                    changeInRow = rnd.Next(3, 12);
                                    nextChange = 0;
                                    wasFlat = true;
                                }
                                else
                                    wasFlat = false;

                                nextY = prevY + (nextChange * changeInRow);
                            }
                            
                            for (int y = 0; y < ChunkSize.Y; y++)
                            {
                                if (y == prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 2);
                                else if (y < prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 0);
                                else
                                    chunk[x, y] = new Block(new Vector2(x, y), (byte)((y > prevY + nextChange + 3) ? 3 : 1));

                            }
                            changeInRow--;
                            prevY += nextChange;
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
                                if (wasFlat)
                                    up = !up;

                                if (!up)
                                {
                                    while (prevY + (changeInRow = rnd.Next(1, maxChange)) <= 3) ;
                                    nextChange = 1;
                                }
                                else
                                {
                                    while (prevY - (changeInRow = rnd.Next(1, maxChange)) >= ChunkSize.Y - 4) ;
                                    nextChange = -1;
                                }

                                if (!wasFlat)
                                {
                                    changeInRow = rnd.Next(3, 12);
                                    nextChange = 0;
                                    wasFlat = true;
                                }
                                else
                                    wasFlat = false;

                                nextY = prevY + (nextChange * changeInRow);
                            }

                            for (int y = 0; y < ChunkSize.Y; y++)
                            {
                                if (y == prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 2);
                                else if (y < prevY + nextChange)
                                    chunk[x, y] = new Block(new Vector2(x, y), 0);
                                else
                                    chunk[x, y] = new Block(new Vector2(x, y), (byte)((y > prevY + nextChange + 3) ? 3 : 1));

                            }
                            changeInRow--;
                            prevY += nextChange;
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
                                chunk[x, y] = new Block(new Vector2(x, y), 0);
                            else if (y == lastY)
                            {
                                if (x > (ChunkSize.X / 2 - 2) && x < ChunkSize.X / 2 + 2)
                                    chunk[x, y] = new Block(new Vector2(x, y), 4);
                                else
                                    chunk[x, y] = new Block(new Vector2(x, y), 2);
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
                if(id != 0)
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
            textureData.Add(3, new Rectangle(80, 0, 40, 40));
            textureData.Add(4, new Rectangle(120, 0, 40, 40));
            textureData.Add(5, new Rectangle(120, 0, 20, 20));
        }
        public static Rectangle GetSourceRectangle(byte id)
        {
            return textureData[id];
        }
    }
}
