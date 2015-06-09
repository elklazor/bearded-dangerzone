using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TextureManager = Bearded_Dangerzone.GamePart.TextureManager;
namespace Bearded_Dangerzone.GUI
{
    class MainMenu:Menu, GamePart.IFocusable
    {
        Game1 baseGame;
        public MainMenu(Game1 game):
            base()
        {
            base.Load(new Rectangle(450, 50, 400, 560), GamePart.TextureManager.GetSourceRectangle("menuTexture2"),"Sheet1",20,20,Color.Black);
            baseGame = game;

            AddButton("Play",20, 40,new MouseButtonClick(ButtonPlayClicked));
            AddButton("Toggle Fullscreen", 80, 40,new MouseButtonClick(ButtonToggleClicked));
            AddButton("Reset Map", 140, 40,new MouseButtonClick(ResetMapClicked));
            AddButton("Exit", 500, 40,new MouseButtonClick(ExitButtonClicked));
        }

        void ButtonPlayClicked()
        {
            baseGame.CurrentMenu = new JoinMenu(baseGame);
        }
        void ButtonToggleClicked()
        {
            baseGame.ToggleFullScreen();
        }
        void ResetMapClicked()
        {
            Directory.Delete("./Map/Regions",true);
            Directory.CreateDirectory("./Map/Regions");
        }
        private void ExitButtonClicked()
        {
            baseGame.Exit();
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
            spriteBatch.Draw(TextureManager.MenuBackground, Vector2.Zero, Color.White);
            spriteBatch.Draw(sheet, menuRectangle, backgroundSourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }

        public Vector2 Position
        {
            get { return new Vector2(menuRectangle.X + menuRectangle.Width / 2, menuRectangle.Y + menuRectangle.Height / 2); }
        }
    }
}
