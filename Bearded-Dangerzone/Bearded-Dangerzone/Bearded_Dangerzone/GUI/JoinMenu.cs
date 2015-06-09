using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bearded_Dangerzone.GUI
{
    public delegate void BoxClicked(InputBox sender);
    class JoinMenu:Menu
    {
        
        public event BoxClicked BoxActiveChange;
        private BoxClicked bxc;
        Game1 baseGame;
        public JoinMenu(Game1 baseGame) :
            base()
        {
            base.Load(new Rectangle(450, 50, 400, 560), GamePart.TextureManager.GetSourceRectangle("menuTexture2"), "Sheet1", 20, 20, Color.Black);
            this.baseGame = baseGame;
            BoxActiveChange += JoinMenu_BoxActiveChange;
            bxc = new BoxClicked(JoinMenu_BoxActiveChange);
            AddBox(80, 30,190,"IP",15,1,bxc);
            AddBox(130, 30, 70,"Port",5,1,bxc);
            AddBox(180, 30, 190, "Username", 8, 2, bxc);
            AddButton("Connect!", 230, 40, new MouseButtonClick(ConnectButtonClicked));
            AddButton("Host!", 290, 40, new MouseButtonClick(HostButtonClicked));
            AddButton("Back",500,40,new MouseButtonClick(BackButtonClicked));
        }

        private void BackButtonClicked()
        {
            baseGame.CurrentMenu = new MainMenu(baseGame);
        }

        private void HostButtonClicked()
        {
            baseGame.CurrentMenu = null;
            baseGame.BaseGameState = Game1.GameState.Join;
            baseGame.StartServerAndConnect((buttons[2] as TextBox).Text,int.Parse((buttons[1] as NumberBox).Text));
        }

        void JoinMenu_BoxActiveChange(InputBox sender)
        {
            foreach (IGUIComponent item in buttons)
            {
                if (item is InputBox)
                {   
                    (item as InputBox).IsActive = false;
                }
            }
            sender.IsActive = true;
            CurrentControl = sender;
        }
        void ConnectButtonClicked()
        {
            baseGame.CurrentMenu = null;
            baseGame.BaseGameState = Game1.GameState.Join;
            baseGame.Connect((buttons[0] as NumberBox).Text,int.Parse((buttons[1] as NumberBox).Text),(buttons[2] as TextBox).Text);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(GamePart.TextureManager.MenuBackground, Vector2.Zero, Color.White);
            base.Draw(spriteBatch);
        }
        
    }
}
