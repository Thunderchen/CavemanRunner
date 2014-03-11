﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CavemanRunner
{
    class GameObject
    {
        // private fields
        protected Renderer renderer;
        protected Transform transform;
        protected Physics physics;
        SpriteBatch spriteBatch;

        public void Initialize(CavemanRunner game, Texture2D texture)
        {
            renderer = new Renderer();
            renderer.Texture = texture;

            physics = new Physics();
            physics.Collider = texture.Bounds;

            transform = new Transform();

            spriteBatch = game.spriteBatch;
        }

        public void Initialize(CavemanRunner game, Texture2D texture, Vector2 velocity, int mass, bool isStatic = false)
        {
            renderer = new Renderer();
            renderer.Texture = texture;

            physics = new Physics(mass, isStatic, velocity);

            transform = new Transform();

            spriteBatch = game.spriteBatch;
        }

        public void Update(GameTime gameTime)
        {
            // update physics
            if (physics != null)
                physics.Update(gameTime);

            // update transform
            transform.Position += physics.Velocity;
            physics.Collider = new Rectangle(Convert.ToInt32(transform.Position.X),
                Convert.ToInt32(transform.Position.Y), physics.Collider.Width, physics.Collider.Height);
        }

        public void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(renderer.Texture, transform.Position, Color.White);
            spriteBatch.End();
        }

        public bool CheckCollision(GameObject gameObject)
        {
            if(physics.Collider.Intersects(gameObject.physics.Collider))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
