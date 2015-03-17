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
        private List<short> chunkManagerChunks = new List<short>();

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
