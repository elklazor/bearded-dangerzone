using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Bearded_Dangerzone.GUI
{
    class TextBox:InputBox,IGUIComponent
    {
        private Rectangle boxRectangle;

        
        private string label;
        public Rectangle BoxRectangle { get; private set; }
        private bool mbUp = true;
        public event BoxClicked MouseClicked;
        
        public TextBox(Rectangle boxRectangle,string label,int length, BoxClicked mbclick)
        {
            Text = "";
            MaxLength = length;
            BoxRectangle = boxRectangle;
            this.label = label;
            
            MouseClicked += mbclick;
        }

        public override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

            if (BoxRectangle.Contains(new Point(ms.X,ms.Y)))
            {
                if (mbUp && ms.LeftButton == ButtonState.Pressed)
                {
                    mbUp = false;
                    MouseClicked(this);
                }
            }
            else if (ms.LeftButton == ButtonState.Released)
                mbUp = true;

        }

        
        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(GamePart.TextureManager.SpriteSheet, BoxRectangle, GamePart.TextureManager.GetSourceRectangle((IsActive)? "menuTexture4":"menuTexture3" ), Color.White);
            spriteBatch.DrawString(GamePart.TextureManager.GameFont, label, new Vector2(BoxRectangle.Left - (GamePart.TextureManager.GameFont.MeasureString(label).X + 5),BoxRectangle.Top), Color.Black);
            spriteBatch.DrawString(GamePart.TextureManager.GameFont, Text, new Vector2(BoxRectangle.X + 2, BoxRectangle.Y - 3), Color.Black);
        }
    }
}
