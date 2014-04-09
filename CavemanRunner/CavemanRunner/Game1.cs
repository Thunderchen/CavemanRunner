﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CavemanRunner
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class CavemanRunner : Game
    {
        public static float toleranceAtStartingTempo = 100;

        public float scaleToReference;
        public SpriteFont font;
        public Texture2D halfScreen;

        public enum GameState
        {
            Menu = 0,
            InGame,
            Paused,
            EndRun,
            PreRun
        }

        public enum CollisionID
        {
            Player,
            Platform,
            HeathCollectible,
            ScoreCollectible,
            Obstacle
        }

        Texture2D background;
        ParallaxingBackground treesBack = new ParallaxingBackground();
        ParallaxingBackground treesFront = new ParallaxingBackground();
        DrumSide.side previousDrumSide;
        Drum leftDrum, rightDrum;
        SoundEffect click, bongo1, bongo2;
        GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;
        public TouchCollection touches;
        public bool jumpDoubleTap;
        Player player;
        GameObject dino;
        GameState gameState;
        public int currentTempo, startingTempo = 100, maxTempo = 200, currentTolerance, successCounter = 0;
        int[] clickTimes = { 0, 0, 0, 0 };
        float distance;
        bool hit = false;

        Pool<Platform> platformPool;
        Pool<ScoreCollectible> scoreCollectiblePool;

        public CavemanRunner()
        {
            currentTempo = startingTempo;
            clickTimes[0] = 0 * 60 * 1000 / currentTempo;
            clickTimes[1] = 1 * 60 * 1000 / currentTempo;
            clickTimes[2] = 2 * 60 * 1000 / currentTempo;
            clickTimes[3] = 3 * 60 * 1000 / currentTempo;
            platformPool = new Pool<Platform>(10);
            scoreCollectiblePool = new Pool<ScoreCollectible>(5);
            graphics = new GraphicsDeviceManager(this);
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
            font = Content.Load<SpriteFont>("Fonts/font");
            scaleToReference = (float)GraphicsDevice.Viewport.Width / 800f;
            halfScreen = Content.Load<Texture2D>("Graphics/halfscreen");

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            player = new Player();
            player.Initialize(this, Content.Load<Texture2D>("Graphics/caveman"),
                Vector2.Zero, 100, false, Renderer.AnchorPoint.BottomMiddle);
            player.collider.SetSize(player.renderer.Texture.Width / 4, player.renderer.Texture.Height);
            player.renderer.AddAnimation("running", player.renderer.Texture, 150, 150, 4, startingTempo * 2, Color.White,
                player.transform.Scale.X, true, true, true);
            player.transform.Position = new Vector2(graphics.GraphicsDevice.Viewport.Width/4 * scaleToReference, 200 * scaleToReference);
            player.renderer.SetAnchorPoint(Renderer.AnchorPoint.BottomMiddle);

            dino = new GameObject();
            dino.Initialize(this, Content.Load<Texture2D>("Graphics/dino"), Vector2.Zero, 1000, false, Renderer.AnchorPoint.BottomMiddle);
            dino.collider.SetSize(dino.renderer.Texture.Width, dino.renderer.Texture.Height);
            dino.transform.Position = new Vector2(-dino.collider.Bounds.Width / 4, Platform.bottom * GraphicsDevice.Viewport.Height);
            dino.renderer.SetAnchorPoint(Renderer.AnchorPoint.BottomMiddle);

            leftDrum = new Drum();
            leftDrum.Initialize(this, halfScreen, Vector2.Zero, 100, true, Renderer.AnchorPoint.TopLeft);
            leftDrum.drumSide = DrumSide.side.LEFT;
            leftDrum.transform.Position = Vector2.Zero;
            
            rightDrum = new Drum();
            rightDrum.Initialize(this, halfScreen, Vector2.Zero, 100, true, Renderer.AnchorPoint.TopLeft);
            rightDrum.drumSide = DrumSide.side.RIGHT;
            rightDrum.transform.Position = new Vector2(GraphicsDevice.Viewport.Width / 2, 0f);

            platformPool.InitializeObjects(this, Content.Load<Texture2D>("Graphics/grass"), new Vector2(-1, 0), 1, true, Renderer.AnchorPoint.TopLeft);
            platformPool.ActivateNewObject().transform.Position = new Vector2(platformPool.Objects[0].collider.Bounds.Width * 0, GraphicsDevice.Viewport.Height
                * Platform.bottom);
            platformPool.ActivateNewObject().transform.Position = new Vector2(platformPool.Objects[0].collider.Bounds.Width * 1, GraphicsDevice.Viewport.Height
                * Platform.bottom);
            platformPool.ActivateNewObject().transform.Position = new Vector2(platformPool.Objects[0].collider.Bounds.Width * 2, GraphicsDevice.Viewport.Height
                * Platform.bottom);
            platformPool.ActivateNewObject().transform.Position = new Vector2(platformPool.Objects[0].collider.Bounds.Width * 3, GraphicsDevice.Viewport.Height
                * Platform.bottom);

            scoreCollectiblePool.InitializeObjects(this, Content.Load<Texture2D>("Graphics/scoreCollectible"), Vector2.Zero, 1, true);
            
            // background texture and parallaxers
            background = Content.Load<Texture2D>("Graphics/background");
            treesBack.Initialize(Content, "Graphics/trees_dark", GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 1);
            treesFront.Initialize(Content, "Graphics/trees_light", GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 2);
            
            // sound effects
            click = Content.Load<SoundEffect>("Sounds/click");
            bongo1 = Content.Load<SoundEffect>("Sounds/bongo1");
            bongo2 = Content.Load<SoundEffect>("Sounds/bongo2");
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
            // recalculate touch tolerance
            currentTolerance = (int)(toleranceAtStartingTempo * ((float)startingTempo / (float)currentTempo));

            // update background
            treesBack.Update(gameTime);
            treesFront.Update(gameTime);

            // check collision against platforms
            //bool collisionHelper = false;
            for (int i = 0; i < platformPool.Objects.Count; i++ )
            {
                if (player.collider.CheckCollisions(platformPool.Objects[i].collider))
                {
                    if (player.physics.Velocity.Y >= 0
                        && player.transform.Position.Y <= platformPool.Objects[i].transform.Position.Y + player.physics.Velocity.Y + 1)
                    {
                        player.transform.Position = new Vector2(player.transform.Position.X,
                            platformPool.Objects[i].transform.Position.Y + 1f);
                        player.SetGrounded(true);
                        break;
                    }
                }
                else
                {
                    player.SetGrounded(false);
                }
            }

            int millis = (int)Math.Round(gameTime.TotalGameTime.TotalMilliseconds);

            if (clickTimes[3] < millis)
            {
                clickTimes[0] = Convert.ToInt32(millis);
                clickTimes[1] = clickTimes[0] + 60 * 1000 / currentTempo / 4;
                clickTimes[2] = clickTimes[1] + 60 * 1000 / currentTempo / 4;
                clickTimes[3] = clickTimes[2] + 60 * 1000 / currentTempo / 4;
                click.Play();
                dino.physics.Velocity += Vector2.UnitX * 0.03f;
            }

            CheckTapInput(gameTime);

            foreach(GameObject go in platformPool.Objects)
            {
                go.Update(gameTime);
                if(go.transform.Position.X < 0 - go.renderer.Texture.Width)
                {
                    GameObject tempPlatform = platformPool.ActivateNewObject();
                    tempPlatform.transform.Position = new Vector2(platformPool.Objects[platformPool.Objects.Count - 2].transform.Position.X
                         + platformPool.Objects[platformPool.Objects.Count - 2].collider.Bounds.Width,
                         Platform.RandomHeight(this.GraphicsDevice.Viewport.Height));
                    platformPool.ReleaseObject((Platform)go);
                    return;
                }
                go.physics.Velocity = Vector2.UnitX * ((-1 * GraphicsDevice.Viewport.Width) / (4 * 60f / (float)currentTempo))
                    * gameTime.ElapsedGameTime.Milliseconds * 0.001f;
            }

            leftDrum.Update(gameTime);
            rightDrum.Update(gameTime);
            player.Update(gameTime);
            
            dino.Update(gameTime);
            if (dino.transform.Position.X < -dino.collider.Bounds.Width / 4)
            {
                dino.transform.Position = new Vector2(-dino.collider.Bounds.Width / 4, Platform.bottom * GraphicsDevice.Viewport.Height);
                dino.physics.Velocity = Vector2.Zero;
            }

            float dinoSpeedHelper = 0;
            dinoSpeedHelper = Math.Max(dino.physics.Velocity.X, -1f);
            dinoSpeedHelper = Math.Min(dino.physics.Velocity.X, 1f);
            dino.physics.Velocity = Vector2.UnitX * dinoSpeedHelper;

            base.Update(gameTime);

            CheckGameEnd();
        }

        void CheckGameEnd ()
        {
            if (dino.transform.Position.X >= player.transform.Position.X)
            {
                dino.transform.Position = new Vector2(-dino.collider.Bounds.Width / 4, Platform.bottom * GraphicsDevice.Viewport.Height);
                dino.physics.Velocity = Vector2.Zero;
            }
        }

        void CheckTapInput (GameTime gameTime)
        {
            // jump on two finger tap
            touches = TouchPanel.GetState();

            if (!player.IsGrounded)
                return;

            if (touches.Count == 1 && touches[0].State == TouchLocationState.Pressed)
            {
                if (CheckDrumHit(touches, leftDrum))
                {
                    bongo1.Play();
                    if (CheckDrumTiming(leftDrum, gameTime))
                    {
                        successCounter++;
                        if (successCounter % 10 == 0)
                        {
                            currentTempo += 2;
                            successCounter = 0;
                        }
                        dino.physics.Velocity -= Vector2.UnitX * 0.05f;
                        previousDrumSide = leftDrum.drumSide;
                        //player.physics.AddForce(Vector2.UnitX * 2000);
                    }
                    else
                    {
                        dino.physics.Velocity += Vector2.UnitX * 0.08f;
                    }
                }
                else if (CheckDrumHit(touches, rightDrum))
                {
                    bongo2.Play();
                    if (CheckDrumTiming(rightDrum, gameTime))
                    {
                        successCounter++;
                        if(successCounter % 10 == 0)
                        {
                            currentTempo += 2;
                            successCounter = 0;
                        }
                        dino.physics.Velocity -= Vector2.UnitX * 0.1f;
                        previousDrumSide = rightDrum.drumSide;
                        //player.physics.AddForce(Vector2.UnitX * 2000);
                    }
                    else
                    {
                        dino.physics.Velocity += Vector2.UnitX * 0.25f;
                    }
                }
            }
            else if (touches.Count == 2 && CheckDrumHit(touches, leftDrum, rightDrum)
                && (touches[0].State == TouchLocationState.Pressed || touches[0].State == TouchLocationState.Moved)
                && (touches[1].State == TouchLocationState.Pressed || touches[1].State == TouchLocationState.Moved))
            {
                player.Jump();
            }

            // clamp tempo to maxTempo
            if (currentTempo > maxTempo)
                currentTempo = maxTempo;
        }

        public void PlayBothBongos()
        {
            bongo1.Play();
            bongo2.Play();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();

            spriteBatch.Draw(background, Vector2.Zero, Color.White);
            treesBack.Draw(spriteBatch);
            treesFront.Draw(spriteBatch);

            dino.Draw(gameTime);

            if (touches.Count == 1)
            {
                spriteBatch.DrawString(font, touches[0].State.ToString(), Vector2.UnitY * 20, Color.Black);
            }
            else if (touches.Count == 2)
            {
                spriteBatch.DrawString(font, touches[0].State.ToString(), Vector2.UnitY * 20, Color.Black);
                spriteBatch.DrawString(font, touches[0].State.ToString(), Vector2.UnitY * 40, Color.Black);
            }

            if(hit)
            {
                spriteBatch.DrawString(font, "HIT", Vector2.Zero, Color.Black);
            }

            spriteBatch.DrawString(font, "Player: " + player.transform.Position.ToString(),
                    new Vector2(0, 100), Color.Black);
            spriteBatch.DrawString(font, "Dino: " + dino.transform.Position.ToString(),
                   new Vector2(0, 120), Color.Black);
            spriteBatch.DrawString(font, "Current tempo: " + currentTempo.ToString(),
                   new Vector2(0, 140), Color.Black);
            spriteBatch.DrawString(font, "Current tolerance: " + currentTolerance.ToString(),
                  new Vector2(0, 160), Color.Black);

            leftDrum.Draw(gameTime);
            rightDrum.Draw(gameTime);

            foreach (GameObject go in platformPool.Objects)
            {
                go.Draw(gameTime);
            }

            player.Draw(gameTime);
            spriteBatch.End();
            base.Draw(gameTime);
        }

        private bool CheckDrumTiming(Drum drum, GameTime gameTime)
        {
            hit = false;
            if (drum.drumSide != previousDrumSide)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds <= clickTimes[0] + currentTolerance ||
                    gameTime.TotalGameTime.TotalMilliseconds >= clickTimes[3] - currentTolerance ||
                    (gameTime.TotalGameTime.TotalMilliseconds >= clickTimes[1] - currentTolerance &&
                    gameTime.TotalGameTime.TotalMilliseconds <= clickTimes[1] + currentTolerance) ||
                    (gameTime.TotalGameTime.TotalMilliseconds >= clickTimes[2] - currentTolerance &&
                    gameTime.TotalGameTime.TotalMilliseconds <= clickTimes[2] + currentTolerance))
                {
                    hit = true;
                }
            }

            return hit;
        }

        private bool CheckDrumHit(TouchCollection touches, params Drum[] drums)
        {
            bool result = false;
            foreach (TouchLocation location in touches)
            {
                foreach (Drum drum in drums)
                {
                    if (location.Position.X > drum.collider.Bounds.Left && location.Position.X < drum.collider.Bounds.Right &&
                        location.Position.Y > drum.collider.Bounds.Top && location.Position.Y < drum.collider.Bounds.Bottom)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }
    }
}
