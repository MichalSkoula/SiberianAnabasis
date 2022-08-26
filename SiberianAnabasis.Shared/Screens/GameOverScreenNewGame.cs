﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Screens;
using SiberianAnabasis.Controls;
using SiberianAnabasis.Shared;
using System.Collections.Generic;
using static SiberianAnabasis.Enums;

namespace SiberianAnabasis.Screens
{
    class GameOverScreenNewGame : GameScreen
    {
        private new Game1 Game => (Game1)base.Game;

        public GameOverScreenNewGame(Game1 game) : base(game) { }

        private Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        public override void Initialize()
        {
            buttons.Add("yes", new Button(Offset.MenuX, 60, null, ButtonSize.Large, "Yes", true));
            buttons.Add("no", new Button(Offset.MenuX, 100, null, ButtonSize.Large, "No"));

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // update buttons
            foreach (KeyValuePair<string, Button> button in this.buttons)
            {
                button.Value.Update();
            }

            // iterate through buttons up/down
            if (Controls.Keyboard.HasBeenPressed(Keys.Down) || Controls.Gamepad.HasBeenPressed(Buttons.DPadDown))
            {
                Tools.ButtonsIterateWithKeys(Direction.Down, this.buttons);
            }
            else if (Controls.Keyboard.HasBeenPressed(Keys.Up) || Controls.Gamepad.HasBeenPressed(Buttons.DPadUp))
            {
                Tools.ButtonsIterateWithKeys(Direction.Up, this.buttons);
            }

            // enter? some button has focus? click!
            if (Controls.Keyboard.HasBeenPressed(Keys.Enter) || Controls.Gamepad.HasBeenPressed(Buttons.A))
            {
                foreach (KeyValuePair<string, Button> button in this.buttons)
                {
                    if (button.Value.Focus)
                    {
                        button.Value.Clicked = true;
                        break;
                    }
                }
            }

            // main menu - NO
            if (this.buttons.GetValueOrDefault("no").HasBeenClicked() || Controls.Keyboard.HasBeenPressed(Keys.Escape) || Controls.Gamepad.HasBeenPressed(Buttons.B))
            {
                this.Game.LoadScreen(typeof(Screens.GameOverScreen));
            }

            // new game -YES
            if (this.buttons.GetValueOrDefault("yes").HasBeenClicked())
            {
                // delete save
                FileIO saveFile = new FileIO(Game.SaveSlot);
                saveFile.Delete();

                // reset some variables
                this.Game.Village = 1;

                // back to menu
                this.Game.LoadScreen(typeof(Screens.MenuScreen));
            }
        }

        public override void Draw(GameTime gameTime)
        {
            this.Game.Matrix = null;
            this.Game.DrawStart();

            // title
            this.Game.SpriteBatch.DrawString(Assets.Fonts["Large"], "Start a new game?", new Vector2(Offset.MenuX, Offset.MenuY), Color.White);

            // buttons
            foreach (KeyValuePair<string, Button> button in this.buttons)
            {
                button.Value.Draw(this.Game.SpriteBatch);
            }

            // messages
            Game1.MessageBuffer.Draw(Game.SpriteBatch);

            this.Game.DrawEnd();
        }
    }
}
