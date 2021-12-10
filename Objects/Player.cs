﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame.Objects
{
    public class Player : Component
    {
        private int speed = 300;

        public Player(Texture2D sprite, int x, int y)
        {
            Sprite = sprite;
            Hitbox = new Rectangle(x, y, Sprite.Width, Sprite.Height);
        }

        public override void Update(float deltaTime)
        {
            Rectangle newHitbox = Hitbox;
            if (Controls.Keyboard.IsPressed(Keys.Right))
            {
                newHitbox.X += (int)(deltaTime * speed);
            }
            if (Controls.Keyboard.IsPressed(Keys.Left))
            {
                newHitbox.X -= (int)(deltaTime * speed);
            }
            if (Controls.Keyboard.IsPressed(Keys.Up))
            {
                newHitbox.Y -= (int)(deltaTime * speed);
            }
            if (Controls.Keyboard.IsPressed(Keys.Down))
            {
                newHitbox.Y += (int)(deltaTime * speed);
            }

            Hitbox = newHitbox;
        }
    }
}
