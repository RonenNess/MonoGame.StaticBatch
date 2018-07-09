/// <summary>
/// Test the static batch utility.
/// Author: Ronen Ness.
/// Since: 2018.
/// </summary>
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace MonoGame.StaticBatch
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class TestStaticBatch : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        /// <summary>
        /// Different rendering modes.
        /// </summary>
        public enum DemoRenderMode
        {
            // draw level using static grid
            Use_Static_Batch,

            // draw all sprites without grid
            No_Batch_Draw_All,

            // draw sprites without grid, but only the ones in screen boundaries
            No_Batch_Only_Visible,

            // to mark end of modes
            Invalid,
        }

        // for testing
        private readonly int LevelSize = 10000;
        private StaticBatch staticGrid;
        private Texture2D tilesTexture;
        private Texture2D skeletonTexture;
        private Texture2D boneTexture;
        private Texture2D treeTexture;
        private Texture2D filledRect;
        private Vector2 cameraPos = Vector2.Zero;
        private SpriteFont font;

        // current drawing mode
        DemoRenderMode mode = DemoRenderMode.Use_Static_Batch;

        // list of all sprites to draw
        List<StaticBatch.StaticSprite> _sprites;
        string spritesCountStr;

        public TestStaticBatch()
        {
            graphics = new GraphicsDeviceManager(this);
            IsFixedTimeStep = false;
            graphics.PreferredBackBufferWidth = 1000;
            graphics.PreferredBackBufferHeight = 800;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
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

            // load texture and create grid
            filledRect = Content.Load<Texture2D>("test/fillrect");
            tilesTexture = Content.Load<Texture2D>("test/floortileset");
            skeletonTexture = Content.Load<Texture2D>("test/skeleton");
            boneTexture = Content.Load<Texture2D>("test/bone");
            treeTexture = Content.Load<Texture2D>("test/tree");
            font = Content.Load<SpriteFont>("test/deffont");
            staticGrid = new StaticBatch(GraphicsDevice, new Point(512, 512));

            // create list of sprites to render in the test scene
            _sprites = new List<StaticBatch.StaticSprite>();

            // some consts
            int tileSize = 32;
            Vector2 tileSizeVec = new Vector2(tileSize);
            Point tileSizePoint = new Point(tileSize, tileSize);

            // for random
            System.Random rand = new System.Random(15);

            // add tiles
            for (int i = 0; i < LevelSize / tileSize; ++i)
            {
                for (int j = 0; j < LevelSize / tileSize; ++j)
                {
                    Point srcIndex = new Point(rand.Next(5), rand.Next(2));
                    _sprites.Add(new StaticBatch.StaticSprite(tilesTexture,
                        new Rectangle(tileSizePoint * new Point(i, j), tileSizePoint),
                        new Rectangle(tileSizePoint * srcIndex, tileSizePoint), zindex: 0f));
                }
            }

            // add random bones
            for (int i = 0; i < LevelSize * 3; ++i)
            {
                var posx = rand.Next(LevelSize);
                var posy = rand.Next(LevelSize);
                _sprites.Add(new StaticBatch.StaticSprite(boneTexture,
                        new Rectangle(posx, posy, 16, 16),
                        rotation: (float)rand.NextDouble() * MathHelper.TwoPi,
                        zindex: 0.000001f + (posx + posy) / 100000f));
            }

            // add random skeletons
            for (int i = 0; i < LevelSize * 3; ++i)
            {
                var posx = rand.Next(LevelSize);
                var posy = rand.Next(LevelSize);
                _sprites.Add(new StaticBatch.StaticSprite(skeletonTexture,
                        new Rectangle(posx, posy, 32, 32), zindex: (float)(posy + 32) / (float)(LevelSize) + posx / 1000000f,
                        color: rand.Next(5) < 1 ? Color.Red : Color.White));
            }

            // add random trees
            for (int i = 0; i < LevelSize * 3; ++i)
            {
                var posx = rand.Next(LevelSize);
                var posy = rand.Next(LevelSize);
                _sprites.Add(new StaticBatch.StaticSprite(treeTexture,
                        new Rectangle(posx, posy, 32, 64), zindex: (float)(posy + 60) / (float)(LevelSize) + posx / 1000000f));
            }

            // total number of sprites
            spritesCountStr = _sprites.Count.ToString();

            // build static grid
            staticGrid.AddSprites(_sprites);
            staticGrid.Build(spriteBatch);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        // to capture space released event
        private bool previousFrameSpace;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // move camera
            float camMove = 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (Keyboard.GetState().IsKeyDown(Keys.Left))
                cameraPos.X -= camMove;
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
                cameraPos.X += camMove;
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
                cameraPos.Y -= camMove;
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
                cameraPos.Y += camMove;

            // change demo mode
            if (Keyboard.GetState().IsKeyUp(Keys.Space) && previousFrameSpace)
            {
                mode = (DemoRenderMode)((int)mode + 1);
                if (mode == DemoRenderMode.Invalid) mode = DemoRenderMode.Use_Static_Batch;
            }
            previousFrameSpace = Keyboard.GetState().IsKeyDown(Keys.Space);

            base.Update(gameTime);
        }

        // to measure and show fps
        private float _timeToMeasureFps = 0f;
        private string framerateStr = "";

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // get camera viewport
            var viewport = GraphicsDevice.Viewport.Bounds;
            viewport.Location += cameraPos.ToPoint();

            // count draw calls
            int drawCalls = 0;

            // draw using static grid grid
            if (mode == DemoRenderMode.Use_Static_Batch)
            {
                staticGrid.Draw(spriteBatch, viewport, offset: (-cameraPos).ToPoint());
                drawCalls = staticGrid.LastDrawCallsCount;
            }
            // draw entities manually
            else
            {
                // should we cull invisible sprites?
                bool needCulling = mode == DemoRenderMode.No_Batch_Only_Visible;

                // begin batch
                spriteBatch.Begin(SpriteSortMode.FrontToBack,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    transformMatrix: Matrix.CreateTranslation(new Vector3(-cameraPos.X, -cameraPos.Y, 0)));

                // iterate and draw all sprites
                foreach (var sprite in _sprites)
                {
                    // if need to cull, do it
                    if (needCulling && !viewport.Intersects(sprite.BoundingRect))
                    {
                        continue;
                    }

                    // draw the sprite itself
                    drawCalls++;
                    spriteBatch.Draw(sprite.Texture,
                        sprite.DestRect,
                        sprite.SourceRect,
                        sprite.Color,
                        sprite.Rotation,
                        sprite.Origin,
                        sprite.SpriteEffect,
                        sprite.ZIndex);
                }
                spriteBatch.End();
            }

            // draw metadata
            spriteBatch.Begin();
            spriteBatch.Draw(filledRect, new Rectangle(0, 0, 240, 110), Color.Black);
            int textPosY = 4;
            int lineHeight = 20;
            spriteBatch.DrawString(font, "Sprites Count: " + spritesCountStr, new Vector2(4, textPosY), Color.White); textPosY += lineHeight;
            spriteBatch.DrawString(font, "Draw Calls: " + drawCalls.ToString(), new Vector2(4, textPosY), Color.White); textPosY += lineHeight;
            spriteBatch.DrawString(font, "Mode: " + mode.ToString(), new Vector2(4, textPosY), Color.White); textPosY += lineHeight;
            spriteBatch.DrawString(font, "- Press arrows to move camera.", new Vector2(4, textPosY), Color.White); textPosY += lineHeight;
            spriteBatch.DrawString(font, "- Press space to change mode.", new Vector2(4, textPosY), Color.White); textPosY += lineHeight;

            // draw fps counter
            spriteBatch.Draw(filledRect, new Rectangle(GraphicsDevice.Viewport.Bounds.Right - 95, 0, 95, lineHeight + 4), Color.Black);
            if (_timeToMeasureFps <= 0f)
            {
                framerateStr = ((int)(1 / gameTime.ElapsedGameTime.TotalSeconds)).ToString();
                _timeToMeasureFps = 1f;
            }
            else
            {
                _timeToMeasureFps -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            spriteBatch.DrawString(font, "FPS: " + framerateStr, new Vector2(GraphicsDevice.Viewport.Bounds.Right - 91, 4), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
