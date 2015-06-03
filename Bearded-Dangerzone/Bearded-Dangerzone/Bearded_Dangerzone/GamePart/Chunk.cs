using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Security.Cryptography;

namespace Bearded_Dangerzone.GamePart
{
    partial class Chunk
    {
        private Block[,] blocks;
        private List<Block> drawableBlocks = new List<Block>();
        private List<Entity> entities;
        public static Point BlockSize = new Point(40, 40);
        public static Point ChunkSize { get; set; }
        private static Random rnd = new Random();
        private Vector2 chunkCorner;
        private short id;
        private static object md5Lock = new object();
        private static MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
        public bool Requested { get; set; }
        public bool Reserved { get; set; }
        public List<Block> Blocks { get { return drawableBlocks; } }
        private List<Rectangle> collisionRectangles = new List<Rectangle>();
        public List<Rectangle> CollisionRectangles { get { return collisionRectangles; } }

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
        public Chunk(Block[,] _blocks,short id)
        {
            ID = id;
            blocks = _blocks;
            foreach (var block in _blocks)
            {
                if (block.ID != 0)
                {
                    block.Position = new Vector2(block.Position.X * BlockSize.X, block.Position.Y * BlockSize.Y);
                    drawableBlocks.Add(block);
                }

            }
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (x == 0 || x == 31)
                    {
                        if(_blocks[x,y].ID != 0)
                            collisionRectangles.Add(new Rectangle((int)chunkCorner.X + (int)_blocks[x, y].Position.X, (int)_blocks[x, y].Position.Y, 40, 40));
                    }
                    else if (_blocks[x, y].ID != 0)
                    { 
                        collisionRectangles.Add(new Rectangle((int)chunkCorner.X + (int)_blocks[x,y].Position.X,(int)_blocks[x,y].Position.Y,40,40));
                        break;
                    }
                    
                }
            }
        }
        public int GetY(bool left)
        {
            
            if (!left)
            {
                for (int i = 0; i < 32; i++)
                {
                    if (blocks[ChunkSize.X - 1, i].ID != 0)
                    {
                        Console.WriteLine("Chunk: " + id + " . " + i);
                        return i;
                    }
                    
                }
                return 0;
            }
            else
            {
                for (int i = 0; i < 32; i++)
                {
                    if (blocks[0, i].ID != 0)
                    {
                        Console.WriteLine("Chunk: " + id + " . " + i);
                        return i;
                    }
                        
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
        /// <summary>
        /// Gets the hash for the map
        /// </summary>
        /// <returns>A Base64 string representation of the hash</returns>
        public string GetHash()
        {
            lock (md5Lock)
            {
                return Convert.ToBase64String(md5Hasher.ComputeHash(blocks.Cast<Block>().Select(x => x.ID).ToArray()));
            }
        }
        /// <summary>
        /// Returns a Chunk from a map string
        /// </summary>
        public static Chunk GetChunkFromString(string cData,short id)
        {
            Block[,] chunk;
            string[] columns, rows;
            columns = cData.Split('|');
            chunk = new Block[32, 32];

            for (int y = 0; y < columns.Length; y++)
            {
                rows = columns[y].Split(',');
                for (int x = 0; x < rows.Length - 1; x++)
                {
                    chunk[x, y] = new Block(new Vector2(x, y), byte.Parse(rows[x]));
                }
            }
            Chunk c = new Chunk(chunk,id);
            
            return c;
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
            if (id != 0)
                source = TextureManager.GetSourceRectangle(id);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 chunkOffset)
        {
            spriteBatch.Draw(TextureManager.SpriteSheet, chunkOffset + Position, source, Color.White);
        }
    }
}
