using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Bearded_Dangerzone.GUI
{
    class NumberBox:IGUIComponent
    {
        public string Text { get; set; }
        private bool[] keyBeenUp = new bool[11];
        public bool IsActive { get; set; }
        private string label;
        public Rectangle BoxRectangle { get; private set; }
        public NumberBox(Rectangle boxRectangle,string label)
        {
            Text = "";
            BoxRectangle = boxRectangle;
            this.label = label;
        }

        public void Update(GameTime gameTime)
        { 
            KeyboardState k = Keyboard.GetState();
            if (k.IsKeyDown(Keys.D0) && keyBeenUp[0])
            {
                Text += "0";
                keyBeenUp[0] = false;
            }
            else
                keyBeenUp[0] = true;

            if (k.IsKeyDown(Keys.D1) && keyBeenUp[1])
            {
                Text += "1";
                keyBeenUp[1] = false;
            }
            else
                keyBeenUp[1] = true;

            if (k.IsKeyDown(Keys.D2) && keyBeenUp[2])
            {
                Text += "2";
                keyBeenUp[2] = false;
            }
            else
                keyBeenUp[2] = true;

            if (k.IsKeyDown(Keys.D3) && keyBeenUp[3])
            {
                Text += "3";
                keyBeenUp[3] = false;
            }
            else
                keyBeenUp[3] = true;

            if (k.IsKeyDown(Keys.D4) && keyBeenUp[4])
            {
                Text += "4";
                keyBeenUp[4] = false;
            }
            else
                keyBeenUp[4] = true;

            if (k.IsKeyDown(Keys.D5) && keyBeenUp[5])
            {
                Text += "5";
                keyBeenUp[5] = false;
            }
            else
                keyBeenUp[5] = true;

            if (k.IsKeyDown(Keys.D6) && keyBeenUp[6])
            {
                Text += "6";
                keyBeenUp[6] = false;
            }
            else
                keyBeenUp[6] = true;

            if (k.IsKeyDown(Keys.D7) && keyBeenUp[7])
            {
                Text += "7";
                keyBeenUp[7] = false;
            }
            else
                keyBeenUp[7] = true;

            if (k.IsKeyDown(Keys.D8) && keyBeenUp[8])
            {
                Text += "8";
                keyBeenUp[8] = false;
            }
            else
                keyBeenUp[8] = true;

            if (k.IsKeyDown(Keys.D9) && keyBeenUp[9])
            {
                Text += "9";
                keyBeenUp[9] = false;
            }
            else
                keyBeenUp[9] = true;

            if (k.IsKeyDown(Keys.OemPeriod) && keyBeenUp[1])
            {
                Text += ".";
                keyBeenUp[10] = false;
            }
            else
                keyBeenUp[10] = true;

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(GamePart.TextureManager.SpriteSheet, BoxRectangle, GamePart.TextureManager.GetSourceRectangle("menuTexture3"), Color.White);
            spriteBatch.DrawString(GamePart.TextureManager.GameFont, Text, new Vector2(BoxRectangle.X + 2, BoxRectangle.Y - 3), Color.Black);
        }


        
    }
}
