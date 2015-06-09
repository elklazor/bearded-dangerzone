using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TextureManager = Bearded_Dangerzone.GamePart.TextureManager;
namespace Bearded_Dangerzone.GUI
{
    public class Menu
    {
        protected List<IGUIComponent> buttons = new List<IGUIComponent>();
        protected Rectangle menuRectangle;
        protected Texture2D sheet;
        protected Rectangle backgroundSourceRectangle;
        protected int buttonMargin;
        protected int buttonDistance;
        protected Color textColor;
        public InputBox CurrentControl { get; set; }

        public Menu()
        {
            
        }

        public virtual void Load(Rectangle baseRectangle,Rectangle textureSource,string sheetName,int margin,int distance,Color _textColor)
        {
            menuRectangle = baseRectangle;
            sheet = TextureManager.SpriteSheet;
            backgroundSourceRectangle = textureSource;
            buttonMargin = margin;
            buttonDistance = distance;
            textColor = _textColor;
        }

        public virtual void AddButton(string text,int y,int height,MouseButtonClick btnEvent)
        {
            Button btn = new Button(text, textColor, y, buttonMargin, height, menuRectangle.Width, menuRectangle);
            btn.ButtonClicked += btnEvent;
            buttons.Add(btn);

        }
        public virtual void AddBox(int y, int height,int width,string label,int lngth,int boxType,BoxClicked mbc)
        {
            int p = (int)GamePart.TextureManager.GameFont.MeasureString(label).X;
            if(boxType == 1)
                buttons.Add(new NumberBox(new Rectangle(menuRectangle.X + buttonMargin + p, y, width,height),label,lngth,mbc));
            else if(boxType == 2)
                buttons.Add(new TextBox(new Rectangle(menuRectangle.X + buttonMargin + p, y, width, height), label, lngth, mbc));
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
