using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bearded_Dangerzone.GamePart
{
    class Cloud
    {
        private Rectangle cloudRectangle;
        private Rectangle sourceRectangle;
        private static float cloudSpeed = 0.3f;
        private Vector2 position;
        public Vector2 Position
        { get { return position; } }
        private float localCloudModifier;
        public Cloud(Vector2 pos,int type,float speedModifier,int sizeModifier)
        {
            cloudRectangle = new Rectangle((int)pos.X, (int)pos.Y, 160*sizeModifier, 50*sizeModifier);
            position = pos;
            sourceRectangle = new Rectangle(0, 50 * type, 160, 50);
            localCloudModifier = speedModifier;
        }
        public void Update(GameTime gameTime)
        {
            cloudRectangle = new Rectangle((int)position.X, (int)position.Y, cloudRectangle.Width, cloudRectangle.Height);
            position.X -= cloudSpeed * localCloudModifier;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(TextureManager.Sky, cloudRectangle, sourceRectangle, Color.White);
        }

    }
}
