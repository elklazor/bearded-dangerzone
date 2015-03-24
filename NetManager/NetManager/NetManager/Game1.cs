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
    class Playersa:IFocusable,ITrackable
    {
        public Vector2 Position { get; set; }
        public Playersa()
        {
            Position = Vector2.Zero;
        }

        private ushort id;
        public ushort ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }


        public byte AnimationState
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool Disconnected
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte Health
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Lidgren.Network.NetConnection Connection
        {
            get { throw new NotImplementedException(); }
        }
    }
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Map map;
        Camera2D camera;
        Playersa player = new Playersa();
        Texture2D tx;
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 700;
            graphics.PreferredBackBufferWidth = 1300;
            Content.RootDirectory = "Content";
            camera = new Camera2D(this);
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
            camera.Focus = player;
            tx = Content.Load<Texture2D>("Sheet");
            map = new Map("./Map/",500);
            map.AddTrackable(player);
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            map.SaveMap();
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
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                player.Position -= new Vector2(0, speed);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                player.Position += new Vector2(0, speed);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                player.Position -= new Vector2(speed, 0);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                player.Position += new Vector2(speed, 0);
            }
            // TODO: Add your update logic here
            if (Keyboard.GetState().IsKeyDown(Keys.F))
            {
                camera.Scale -= 0.05f;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                camera.Scale += 0.05f;
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, DepthStencilState.Default, null, null, camera.Transform);
            //spriteBatch.Begin();
            map.Draw(spriteBatch);
            spriteBatch.Draw(tx, player.Position,TextureManager.GetSourceRectangle(5), Color.White);
            spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
