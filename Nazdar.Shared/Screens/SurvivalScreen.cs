﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Screens;
using Nazdar.Controls;
using Nazdar.Shared;
using System.Collections.Generic;
using static Nazdar.Enums;

namespace Nazdar.Screens
{
    public class SurvivalScreen : GameScreen
    {
        private new Game1 Game => (Game1)base.Game;

        public SurvivalScreen(Game1 game) : base(game) { }

        private Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        private string[] saveDataLines;

        // every mission (village) should have a description
        private float descriptionY = 300;
        private readonly int descriptionYStop = 62;
        private readonly int descriptionSpeed = 40;
        private readonly string[] description = new string[] {
            "Endless farming.",
            "Endless slaughter.",
            "This is the life of a legionnaire.",
            "This is the survival.",
            "",
            "MISSION GOALS",
            "Unlimited time. Survive as long as you can.",
            "",
            "TIPS",
            "Cholera epidemic struck hard! Make sure to have",
            "a lot of medics.",
            "Stronger enemies will attack very soon."
        };

        public override void Initialize()
        {
            buttons.Add("startButton", new Button(Offset.MenuX, 60, null, ButtonSize.Large, "Start", true));
            buttons.Add("deleteButton", new Button(Offset.MenuX, 100, null, ButtonSize.Medium, "Delete save"));
            buttons.Add("menuButton", new Button(Offset.MenuX, 310, null, ButtonSize.Medium, "Back to Menu"));

            this.Load();

            Audio.SongVolume = 0.25f;

            base.Initialize();
        }

        private void Load()
        {
            FileIO saveFile = new FileIO(Game.SaveSlot);

            dynamic saveData = saveFile.Load();
            this.saveDataLines = Tools.ParseSaveData(saveData);
            // survival = village 0
            this.Game.Village = 0;

            if (saveData == null || !saveData.ContainsKey("village"))
            {
                buttons.GetValueOrDefault("deleteButton").Active = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            Button.UpdateButtons(this.buttons);

            // start game - villages
            if (this.buttons.GetValueOrDefault("startButton").HasBeenClicked() || Controls.Gamepad.HasBeenPressed(Buttons.Start))
            {
                this.Game.LoadScreen(typeof(Screens.VillageScreen));
            }

            // delete save
            if (this.buttons.GetValueOrDefault("deleteButton").HasBeenClicked())
            {
                this.Game.LoadScreen(typeof(Screens.MapScreenDeleteSave));
            }

            // Back to Menu
            if (this.buttons.GetValueOrDefault("menuButton").HasBeenClicked() || Controls.Keyboard.HasBeenPressed(Keys.Escape) || Controls.Gamepad.HasBeenPressed(Buttons.B))
            {
                this.Game.LoadScreen(typeof(Screens.MenuScreen));
            }

            // move description
            if (this.descriptionY > descriptionYStop)
            {
                this.descriptionY -= this.Game.DeltaTime * descriptionSpeed;
            }
            else
            {
                // to be precise
                this.descriptionY = descriptionYStop;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            this.Game.Matrix = null;
            this.Game.DrawStart();

            this.Game.SpriteBatch.DrawString(Assets.Fonts["Large"], ((Villages)this.Game.Village).ToString(), new Vector2(Offset.MenuX, Offset.MenuY), MyColor.White);

            // buttons
            foreach (KeyValuePair<string, Button> button in this.buttons)
            {
                button.Value.Draw(this.Game.SpriteBatch);
            }

            // messages
            Game1.MessageBuffer.Draw(Game.SpriteBatch);

            // save data - show only some things
            int i = 0;
            foreach (string line in this.saveDataLines)
            {
                i++;
                if (i == 1)
                {
                    continue;
                }
                this.Game.SpriteBatch.DrawString(Assets.Fonts["Medium"], line, new Vector2(Offset.MenuX, Offset.MenuY + 72 + 28 * i), MyColor.White);
            }

            // descriptions
            i = 0;
            foreach (string line in this.description)
            {
                i++;
                this.Game.SpriteBatch.DrawString(Assets.Fonts["Small"], line, new Vector2(Offset.MenuX + 225, Offset.MenuY + descriptionY + 18 * i), MyColor.Gray1);
            }

            this.Game.DrawEnd();
        }
    }
}
