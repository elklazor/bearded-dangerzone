using Microsoft.Xna.Framework;
using System;
namespace Bearded_Dangerzone_Client.GUI
{
    interface IMenu
    {
        void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch);
        void Update(GameTime gameTime);
    }
}
