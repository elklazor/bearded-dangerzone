using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Lidgren.Network.Xna;

namespace NetManager
{
    class Player : Client
    {
        #region tempBTN
        private bool drawCollisionRectangles = false;

        private bool drawCollisionRectangelsBeenUp = false;
        #endregion
        private bool jumped = false;
        private bool jumpedUp = true;
        public bool Initialized { get; set; }
        private Rectangle playerRectangle;
        private float animTimer = 0f;
        private float maxAnimationTimer = 200;
        float updateStartTimer = 3;
        private bool doneJumpCollision = false;
        public bool IsLocal { get; set; }
        private Rectangle textureSourceRectangle;
        private Vector2 previousPosition;
        public bool MapLoaded { get; set; }
        public Player()
        {
            textureSourceRectangle = TextureManager.GetSourceRectangle(5);
            AnimationState = 0;
        }
        public void SetAllFields(Vector2 position, string name, ushort id, byte health)
        {
            Position = position;
            Name = name;
            ID = id;
            Health = health;
            Initialized = true;
        }
        private bool moving = false;
        internal void Velocity(Vector2 velocity)
        {
            Position += velocity;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsLocal)
            {
                playerRectangle = new Rectangle((int)Position.X, (int)Position.Y, 40, 80);
                textureSourceRectangle = textureSourceRectangle = new Rectangle(40 * AnimationState, 80, 40, 80);
            }

            spriteBatch.Draw(TextureManager.SpriteSheet, playerRectangle, textureSourceRectangle, Color.White,0f,Vector2.Zero,(Flip)? SpriteEffects.FlipHorizontally : SpriteEffects.None,0f);
            
            if (drawCollisionRectangles)
            {
                DrawRectangle(spriteBatch, rt);
                DrawRectangle(spriteBatch, rl);
                DrawRectangle(spriteBatch, rr);
                DrawRectangle(spriteBatch, rb);

                foreach (var c in MapRef.activeChunks.Values)
                {
                    foreach (var r in c.CollisionRectangles)
                    {
                        DrawRectangle(spriteBatch, r);
                    }
                } 
            }
            if (false)
            {
                spriteBatch.DrawString(Game1.GameFont, "Jumped: " + jumped.ToString(), new Vector2(Position.X, Position.Y - 50), Color.Black);
                spriteBatch.DrawString(Game1.GameFont, "JumpedUp: " + jumpedUp.ToString(), new Vector2(Position.X, Position.Y - 70), Color.Black);
                spriteBatch.DrawString(Game1.GameFont, "Bot: " + bot.ToString(), new Vector2(Position.X, Position.Y - 90), Color.Red);
                spriteBatch.DrawString(Game1.GameFont, "Left: " + left.ToString(), new Vector2(Position.X, Position.Y - 110), Color.Red);
                spriteBatch.DrawString(Game1.GameFont, "Right: " + right.ToString(), new Vector2(Position.X, Position.Y - 130), Color.Red); 
            }
        }
        
        internal void Update(GameTime gameTime)
        {
            doneJumpCollision = false;
            Input(gameTime);
            if (MapLoaded)
            {
                velocity.Y += (float)gameTime.ElapsedGameTime.TotalMilliseconds / 17;
                playerRectangle = new Rectangle((int)Position.X, (int)Position.Y, 40, 80);
                
                foreach (var c in MapRef.activeChunks.Values)
                {
                    foreach (var r in c.CollisionRectangles)
                    {
                        CheckCollision(r);
                    }
                }

                if (moving)
                {
                    animTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (animTimer >= maxAnimationTimer)
                    {
                        animTimer = 0;
                        AnimationState++;
                        if (AnimationState > 3)
                            AnimationState = 0;

                        textureSourceRectangle = new Rectangle(40 * AnimationState, 80, 40, 80);
                    }
                }
                else
                    textureSourceRectangle =  new Rectangle(0,80,40,80);
                previousPosition = Position;
                Position += velocity;
                if (Position != previousPosition)
                    NeedUpdate = true;
                else
                    NeedUpdate = false;

            }
        }
        public void DumpPlayerData(NetOutgoingMessage netOut)
        {
         
            netOut.Write((byte)MessageType.PlayerUpdate);
            netOut.Write(ID);
            netOut.Write(Name);
            netOut.Write(Health);
            netOut.Write(Position);
            netOut.Write(AnimationState);
            netOut.Write(Type);
            netOut.Write(Flip);
            
        }
        private void DrawRectangle(SpriteBatch sb, Rectangle r)
        {
            sb.Draw(TextureManager.SpriteSheet, new Rectangle(r.X, r.Y, r.Width, 1), TextureManager.GetSourceRectangle(6), Color.White);
            sb.Draw(TextureManager.SpriteSheet, new Rectangle(r.X, r.Y, 1, r.Height), TextureManager.GetSourceRectangle(6), Color.White);
            sb.Draw(TextureManager.SpriteSheet, new Rectangle(r.Right, r.Y, 1, r.Height), TextureManager.GetSourceRectangle(6), Color.White);
            sb.Draw(TextureManager.SpriteSheet, new Rectangle(r.X, r.Bottom, r.Width, 1), TextureManager.GetSourceRectangle(6), Color.White);
        }
        private Rectangle rt, rl, rr, rb;
        private bool bot, left, right;
        private void CheckCollision(Rectangle bRect)
        {
            bot = left = right = false;
            rt = new Rectangle(playerRectangle.X + 3, playerRectangle.Y - 1, playerRectangle.Width -6, 3);
            rl = new Rectangle(playerRectangle.X - 2, playerRectangle.Y + 20, 4, playerRectangle.Height -40);
            rr = new Rectangle(playerRectangle.X + 38, playerRectangle.Y +20, 4, playerRectangle.Height -40);
            rb = new Rectangle(playerRectangle.X + 6, playerRectangle.Y + 78, playerRectangle.Width -12,5);

            if (rt.Intersects(bRect))
            {
                //Unlikely Scenario (Touch the top of a block)
                //Position = new Vector2(Position.X, bRect.Bottom);
            }
            if (rl.Intersects(bRect))
            {
                Position = new Vector2(bRect.Right, Position.Y);
                left = true;
            }
            if (rr.Intersects(bRect))
            {
                Position = new Vector2(bRect.Left - playerRectangle.Width, Position.Y);
                right = true;
            }
            if (rb.Intersects(bRect))
            {
                if (!jumped && !doneJumpCollision)
                {
                    Position = new Vector2(Position.X, bRect.Top - playerRectangle.Height);
                    velocity.Y = 0;
                    jumpedUp = true;
                }
                else
                {
                    jumped = false;
                    doneJumpCollision = true;
                }
                bot = true;
            }
            
            
        }
        private KeyboardState kState;
        private Vector2 velocity;
        private void Input(GameTime gameTime)
        {
            
            kState = Keyboard.GetState();
            if (kState.IsKeyDown(Keys.D))
            {
                velocity.X = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 7;
                Flip = false;
                moving = true;
            }
            else if (kState.IsKeyDown(Keys.A))
            {
                velocity.X = -(float)gameTime.ElapsedGameTime.TotalMilliseconds / 7;
                Flip = true;
                moving = true;
            }
            else
            {
                velocity.X = 0;
                moving = false;
                AnimationState = 0;
            }
            if (kState.IsKeyDown(Keys.S))
            {
                //velocity.Y = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 7;
            }

            if (kState.IsKeyDown(Keys.LeftShift))
            {
                velocity.X *= 2f;
            }

            if (kState.IsKeyDown(Keys.Space) && jumpedUp)
            {
                velocity.Y -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                jumped = true;
                jumpedUp = false;
            }

            if (kState.IsKeyDown(Keys.H))
            {
                if (drawCollisionRectangelsBeenUp)
                {
                    drawCollisionRectangles = !drawCollisionRectangles;
                    drawCollisionRectangelsBeenUp = false;
                }
            }
            else
                drawCollisionRectangelsBeenUp = true;

        }

        public Map MapRef { get; set; }

        public bool NeedUpdate { get; set; }
    }
}
