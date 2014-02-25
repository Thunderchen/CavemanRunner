﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CavemanRunner
{
    class GameObject : DrawableGameComponent
    {
        // private fields
        Renderer renderer;
        Physics physics;
        Transform transform;
        SpriteBatch spriteBatch;

        public GameObject(CavemanRunner game, Texture2D texture, Vector2 position)
            : base(game)
        {
            spriteBatch = game.spriteBatch;

            renderer = new Renderer();
            renderer.Texture = texture;

            physics = new Physics();

            transform = new Transform();
            transform.Position = position;
        }

        public override void Initialize()
        {
            base.Initialize();
        }
        public override void Update(GameTime gameTime)
        {
            // update physics
            if (physics != null)
                physics.Update(gameTime);

            // update transform
            transform.Position += physics.Velocity;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(renderer.Texture, transform.Position, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
