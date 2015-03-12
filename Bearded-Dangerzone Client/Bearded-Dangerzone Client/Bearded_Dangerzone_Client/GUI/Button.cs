using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone_Client.GUI
{
    public delegate void MouseButtonClick();

    class Button
    {
        private Texture2D texture;
        private Rectangle buttonRectangle;
        private Rectangle textureSourceRectangle;

       
        public event MouseButtonClick ButtonClicked;

        private Color buttonColor = Color.White;
        private Color buttonBaseColor;
        private bool buttonPressed;
        private Color textColor;
        private string text;
        private Vector2 textPosition;
        private MouseState mouseState;
        /// <summary>
        /// Remember to scale position prior to adding button
        /// </summary>
        /// <param name="buttonPosition"></param>
        /// <param name="sheetName"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="buttonText"></param>
        /// <param name="buttonTextColor"></param>
        public Button(string buttonText,Color backgroundColor,Color buttonTextColor)
        {
            textureSourceRectangle = new Rectangle(0,80,40,40);
            texture = Textures.GetTexture("Sheet1");
            text = buttonText;
            textColor = buttonTextColor;
            
        }
        public void Setup(Rectangle buttonRect)
        {
            buttonRectangle = buttonRect;
            textPosition = new Vector2((int)((buttonRectangle.Center.X) - (Textures.GameFont.MeasureString(text).X / 2)), (int)((buttonRectangle.Center.Y - (Textures.GameFont.MeasureString(text).Y / 2))));
        }
        public void Update(GameTime gameTime)
        {
            mouseState = Mouse.GetState();
            
            if (!buttonPressed)
            {
                buttonColor = buttonBaseColor;
            }
            if (mouseState.X > buttonRectangle.Left && mouseState.X < buttonRectangle.Right && mouseState.Y > buttonRectangle.Top && mouseState.Y < buttonRectangle.Bottom)
            {

                if (!buttonPressed)
                    buttonColor = Color.Silver;
                else
                    buttonColor = Color.DarkBlue;

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    buttonPressed = true;
                }
                if (buttonPressed)
                {
                    if(mouseState.LeftButton == ButtonState.Released)
                    {
                        ButtonClicked();
                        buttonPressed = true;
                        Console.WriteLine("Button pressed");
                        buttonPressed = false;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, buttonRectangle, textureSourceRectangle, buttonColor);
            spriteBatch.DrawString(Textures.GameFont,text,textPosition,textColor);
        }
    }
}
