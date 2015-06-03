using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Bearded_Dangerzone.GamePart;
using Bearded_Dangerzone.GUI;
namespace Bearded_Dangerzone
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game, GamePart.IFocusable
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GUI.MainMenu mainMenu;
        GUI.Menu currentMenu;
        Camera2D camera;
        GameServer gameServer;
        GameClient gameClient;

        public enum GameState
        {
            MainMenu,
            Host,
            Join
        }
        GameState gameState = GameState.MainMenu;
        public Menu CurrentMenu
        {
            get { return currentMenu; }
            set { currentMenu = value; }
        }
        public GameState BaseGameState
        {
            get { return gameState; }
            set { gameState = value; }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 700;
            graphics.PreferredBackBufferWidth = 1300;
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
            camera = new Camera2D(this);
            camera.Focus = this;
            Exiting += Game1_Exiting;
        }
        public void StartClient()
        { 
        
        }
        public void StartServer()
        { 
            
        }
        void Game1_Exiting(object sender, EventArgs e)
        {
            if (gameServer != null)
                gameServer.Stop();
            if (gameClient != null)
                gameClient.Stop();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            Components.Add(camera);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            TextureManager.Load(Content);
            mainMenu = new GUI.MainMenu(this);
            currentMenu = mainMenu;
            camera.Focus = mainMenu;
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            if (currentMenu != null)
                currentMenu.Update(gameTime);
            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.FromNonPremultiplied(110, 161, 255, 255));
            if (gameState != GameState.MainMenu)
                spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, DepthStencilState.Default, null, null, camera.Transform);
            else
                spriteBatch.Begin();
            
            if (currentMenu != null)
                currentMenu.Draw(spriteBatch);
            
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public Vector2 Position
        {
            get { return Vector2.Zero; }
        }
    }
}
