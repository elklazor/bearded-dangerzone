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
        Vector2 Position;
        ushort ID;
    }
    class Map
    {
        private Point chunkSize;
        
        private short maxChunk;
        private short minChunk;
        private Thread chunkCheckerThread;
        private ConcurrentDictionary<short, string> regions = new ConcurrentDictionary<short, string>();
        private ConcurrentDictionary<short,Chunk> activeChunks = new ConcurrentDictionary<short, Chunk>();
        private ConcurrentDictionary<ushort, ITrackable> chunkLoaders = new ConcurrentDictionary<ushort, ITrackable>();
        private string baseRegionPath;
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
            baseRegionPath = mapPath + "/Regions/";
            foreach (var region in Directory.GetFiles(mapPath + "/Regions/"))
            {
                xDoc.Load(region);
                regions.TryAdd(short.Parse(xDoc.DocumentElement.Attributes["id"].Value), region);
            }
            //Load Active Regions

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

        private void LoadChunk(short chunkID)
        {
            if (!regions.ContainsKey(chunkID))
            {
                if (chunkID > 0)
                {
                    LoadChunk((short)(chunkID - 1));
                    //LastY
                    Chunk c = Chunk.GenerateChunk(activeChunks.Values.First(x => x.ID == chunkID - 1).GetY(false), false);
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID,c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".txt");
                }

                else if (chunkID < 0)
                {
                    LoadChunk((short)(chunkID + 1));
                    //LastY
                    Chunk c = Chunk.GenerateChunk(activeChunks.Values.First(x => x.ID == chunkID + 1).GetY(true), false);
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID, c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".txt");
                }
                else
                {
                    Chunk c = Chunk.GenerateChunk(chunkSize.Y / 2, false, true);
                    c.ID = chunkID;
                    activeChunks.TryAdd(c.ID, c);
                    regions.TryAdd(chunkID, baseRegionPath + chunkID.ToString() + ".txt");
                }
            }
            else
            {
                if (!activeChunks.ContainsKey(chunkID))
                { 
                    //Needs to be loaded
                }
            }
        }
        private void SaveChunk(Chunk chunk)
        {

        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var chunk in activeChunks.Values)
            {
                chunk.Draw(spriteBatch);
            }
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
                if (left)
                {
                    for (int i = 0; i < ChunkSize.Y; i++)
                    {
                        if (blocks[0, i].ID != 0)
                            return i;
                    }
                    return 0;
                }
                else
                {
                    for (int i = 0; i < ChunkSize.Y; i++)
                    {
                        if (blocks[ChunkSize.X - 1, i].ID != 0)
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
                        for (int x = ChunkSize.X - 1; x >= 0; x--)
                        {
                            //Elevation change logic
                            if (changeInRow == 0)
                            {
                                up = !up;
                                if (up)
                                {
                                    while (prevY + (changeInRow = rnd.Next(0, maxChange)) <= 0) ;
                                    nextChange = 1;
                                }
                                else
                                {
                                    while (prevY - (changeInRow = rnd.Next(0, maxChange)) >= ChunkSize.Y) ;
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
        }
        public static Rectangle GetSourceRectangle(byte id)
        {
            return textureData[id];
        }
    }
}
