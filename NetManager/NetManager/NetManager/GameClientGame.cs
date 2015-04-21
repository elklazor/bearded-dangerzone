using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetManager
{
    partial class GameClient:IFocusable
    {
        KeyboardState kState;
        Vector2 velocity;
        public void Update(GameTime gameTime)
        { 
            kState = Keyboard.GetState();
            velocity = Vector2.Zero;
            if(kState.IsKeyDown(Keys.D))
            {
                velocity.X = (float)gameTime.ElapsedGameTime.TotalMilliseconds/5;
            }
            else if(kState.IsKeyDown(Keys.A))
            {
                velocity.X = -(float)gameTime.ElapsedGameTime.TotalMilliseconds/5;
            }
            if(kState.IsKeyDown(Keys.S))
            {
                velocity.Y = (float)gameTime.ElapsedGameTime.TotalMilliseconds/5;
            }
            else if(kState.IsKeyDown(Keys.S))
            {
                velocity.Y = -(float)gameTime.ElapsedGameTime.TotalMilliseconds/5;
            }
            worldMap.LocalPlayer.Velocity(velocity);
            worldMap.Update(gameTime);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            worldMap.Draw(spriteBatch);
            worldMap.LocalPlayer.Draw(spriteBatch);
        }

        public Vector2 Position
        {
            get 
            { 
                if(worldMap != null)
                {
                    if (worldMap.LocalPlayer != null)
                        return worldMap.LocalPlayer.Position;
                    else return Vector2.Zero;
                }
                
                else
                    return Vector2.Zero;
            }
        }


    }
}
