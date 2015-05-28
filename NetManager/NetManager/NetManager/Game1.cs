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

namespace NetManager 
{
    
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Camera2D camera;
        GameClient gameClient;
        GameServer gameServer;
        private bool serverEnabled;
        private static SpriteFont gameFont;
        
        public static SpriteFont GameFont
        {
            get { return Game1.gameFont; }
            set { Game1.gameFont = value; }
        }
        
        public Game1(bool server)
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 700;
            graphics.PreferredBackBufferWidth = 1300;
            Content.RootDirectory = "Content";
            //.IsFullScreen = true;
            camera = new Camera2D(this);
            serverEnabled = server;
            Exiting += Game1_Exiting;

        }

        void Game1_Exiting(object sender, EventArgs e)
        {
            if (serverEnabled)
            {
                gameServer.Stop();
            }
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

            if (serverEnabled)
            {
                gameServer = new GameServer(29);
                gameServer.Start();
            }
            gameFont = Content.Load<SpriteFont>("gameFont");
            gameClient = new GameClient(25452, "127.0.0.1", (serverEnabled)?"ServerPlayer":"ClientPlayer");
            camera.Focus = gameClient;
            camera.Scale = 1.45f;
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
        float speed = 8f;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //camera.Update(gameTime);
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            // TODO: Add your update logic here
            if (Keyboard.GetState().IsKeyDown(Keys.F))
            {
                camera.Scale -= 0.05f;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                camera.Scale += 0.05f;
            }
            if(gameClient.Initialized)
                gameClient.Update(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.FromNonPremultiplied(110,161,255,255));
            spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, DepthStencilState.Default, null, null, camera.Transform);
            if(gameClient.Initialized)
            gameClient.Draw(spriteBatch);

            //spriteBatch.DrawString(GameFont, "Zoom: " + camera.Scale, new Vector2(0, 400), Color.Blue);
            spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
