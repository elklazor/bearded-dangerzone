using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone_Client.GUI
{
    class MainMenu:Menu
    {

        public MainMenu():
            base()
        {
            base.Load(new Rectangle((640 / 2) - 100, 50, 200, 260), new Rectangle(40, 40, 40, 40),"Sheet1",20,20,Color.Black);

            AddButton("Play", 20, 20);
        }
        
        void Button1_ButtonClicked()
        {
            
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var button in buttons)
            {
                button.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(sheet, menuRectangle, backgroundSourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }
    }
}
