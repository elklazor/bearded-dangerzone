using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Bearded_Dangerzone.GUI
{
    public class NumberBox:InputBox,IGUIComponent
    {
        private bool[] keyBeenUp = new bool[12];
        
        private string label;
        public Rectangle BoxRectangle { get; private set; }
        private bool mbUp = true;
        public event BoxClicked MouseClicked;
        private int maxLength;

        public NumberBox(Rectangle boxRectangle,string label,int length, BoxClicked mbclick)
        {
            Text = "";
            maxLength = length;
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
            else if (ms.LeftButton == ButtonState.Pressed)
                mbUp = true;

            Input();
        }

        

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(GamePart.TextureManager.SpriteSheet, BoxRectangle, GamePart.TextureManager.GetSourceRectangle((IsActive)? "menuTexture4":"menuTexture3" ), Color.White);
            spriteBatch.DrawString(GamePart.TextureManager.GameFont, label, new Vector2(BoxRectangle.Left - (GamePart.TextureManager.GameFont.MeasureString(label).X + 5),BoxRectangle.Top), Color.Black);
            spriteBatch.DrawString(GamePart.TextureManager.GameFont, Text, new Vector2(BoxRectangle.X + 2, BoxRectangle.Y - 3), Color.Black);
        }
        private void Input()
        {
            KeyboardState k = Keyboard.GetState();
            if (IsActive && Text.Length < maxLength)
            {
                
                if (k.IsKeyDown(Keys.D0))
                {
                    if (keyBeenUp[0])
                    {
                        Text += "0";
                        keyBeenUp[0] = false;
                    }
                }
                else
                    keyBeenUp[0] = true;

                if (k.IsKeyDown(Keys.D1))
                {
                    if (keyBeenUp[1])
                    {
                        Text += "1";
                        keyBeenUp[1] = false;
                    }
                }
                else
                    keyBeenUp[1] = true;

                if (k.IsKeyDown(Keys.D2))
                {
                    if (keyBeenUp[2])
                    {
                        Text += "2";
                        keyBeenUp[2] = false;
                    }
                }
                else
                    keyBeenUp[2] = true;

                if (k.IsKeyDown(Keys.D3))
                {
                    if (keyBeenUp[3])
                    {
                        Text += "3";
                        keyBeenUp[3] = false;
                    }
                }
                else
                    keyBeenUp[3] = true;

                if (k.IsKeyDown(Keys.D4))
                {
                    if (keyBeenUp[4])
                    {
                        Text += "4";
                        keyBeenUp[4] = false;
                    }
                }
                else
                    keyBeenUp[4] = true;

                if (k.IsKeyDown(Keys.D5))
                {
                    if (keyBeenUp[5])
                    {
                        Text += "5";
                        keyBeenUp[5] = false;
                    }
                }
                else
                    keyBeenUp[5] = true;

                if (k.IsKeyDown(Keys.D6))
                {
                    if (keyBeenUp[6])
                    {
                        Text += "6";
                        keyBeenUp[6] = false;
                    }
                }
                else
                    keyBeenUp[6] = true;

                if (k.IsKeyDown(Keys.D7))
                {
                    if (keyBeenUp[7])
                    {
                        Text += "7";
                        keyBeenUp[7] = false;
                    }
                }
                else
                    keyBeenUp[7] = true;

                if (k.IsKeyDown(Keys.D8))
                {
                    if (keyBeenUp[8])
                    {
                        Text += "8";
                        keyBeenUp[8] = false;
                    }
                }
                else
                    keyBeenUp[8] = true;

                if (k.IsKeyDown(Keys.D9))
                {
                    if (keyBeenUp[9])
                    {
                        Text += "9";
                        keyBeenUp[9] = false;
                    }
                }
                else
                    keyBeenUp[9] = true;

                if (k.IsKeyDown(Keys.OemPeriod))
                {
                    if (keyBeenUp[10])
                    {
                        Text += ".";
                        keyBeenUp[10] = false;
                    }
                }
                else
                    keyBeenUp[10] = true;

            }
            if (IsActive)
            {
                if (k.IsKeyDown(Keys.Back))
                {
                    if (keyBeenUp[11])
                    {
                        if (Text.Length > 0)
                        {
                            Text = Text.Substring(0, Text.Length - 1);
                        }
                        keyBeenUp[11] = false;
                    }
                }
                else
                    keyBeenUp[11] = true;
            }
        }

        
    }
}
