using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetManager
{
    partial class Chunk
    {
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
    }
}
