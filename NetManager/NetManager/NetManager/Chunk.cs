using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace NetManager
{
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
                else
                {
                    for (int x = ChunkSize.X - 1; x >= 0; x--)
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
            if (id != 0)
                source = TextureManager.GetSourceRectangle(id);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 chunkOffset)
        {
            spriteBatch.Draw(TextureManager.SpriteSheet, chunkOffset + Position, source, Color.White);
        }
    }
}
