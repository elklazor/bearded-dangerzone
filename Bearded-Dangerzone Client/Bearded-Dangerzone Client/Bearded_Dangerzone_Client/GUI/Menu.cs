using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone_Client.GUI
{
    class Menu
    {
        protected List<Button> buttons = new List<Button>();
        protected Rectangle menuRectangle;
        protected Texture2D sheet;
        protected Rectangle backgroundSourceRectangle;
        protected int buttonMargin;
        protected int buttonDistance;

        public Menu()
        {
            
        }
        public virtual void Load(Rectangle baseRectangle,Rectangle textureSource,string sheetName,int margin,int distance)
        { 
            menuRectangle = Helper.TransformRectangle(baseRectangle);
            sheet = Textures.GetTexture(sheetName);
            backgroundSourceRectangle = textureSource;
            buttonMargin = margin;
            buttonDistance = distance;
        }

        public virtual void AddButton(Button btn)
        { 
            
        }
        public virtual void Update(GameTime gameTime)
        {
            foreach (var button in buttons)
            {
                button.Update(gameTime);

            }
        }
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(sheet, menuRectangle, backgroundSourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }
    }
}
