﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Screens;
using SiberianAnabasis.Objects;
using SiberianAnabasis.Shared;
using static SiberianAnabasis.Enums;

namespace SiberianAnabasis.Screens
{
    public class VillageScreen : GameScreen
    {
        private new Game1 Game => (Game1)base.Game;

        public VillageScreen(Game1 game) : base(game) { }

        private readonly Camera camera = new Camera();

        private readonly FileIO saveFile = new FileIO();

        // will be calculated
        public static int MapWidth = Enums.Screen.Width * 4; // 640 * 4 = 2560px

        // game components
        private Player player;
        private List<Enemy> enemies = new List<Enemy>();
        private List<Soldier> soldiers = new List<Soldier>();
        private List<Homeless> homelesses = new List<Homeless>();
        private List<Building> buildings = new List<Building>();
        private List<Coin> coins = new List<Coin>();

        // day and night, timer
        private DayPhase dayPhase = DayPhase.Day;
        private double dayPhaseTimer = (int)DayNightLength.Day;

        public override void Initialize()
        {
            // create player in the center of the map
            this.player = new Player(MapWidth / 2, Offset.Floor);

            // load objects from tileset
            foreach (var building in Assets.TilesetGroups["village1"].GetObjects("objects", "building"))
            {
                this.buildings.Add(new Building((int)building.x, (int)building.y, (int)building.width, (int)building.height));
            }

            // set save slot and maybe load?
            this.saveFile.File = Game.SaveSlot;
            this.Load();

            // play songs
            Audio.StopSong();
            Audio.CurrentSongCollection = this.dayPhase == DayPhase.Day ? "Day" : "Night";

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (Controls.Keyboard.HasBeenPressed(Keys.Escape) || Controls.Gamepad.HasBeenPressed(Buttons.Start))
            {
                // save
                this.Save();

                // back to menu
                this.Game.LoadScreen(typeof(Screens.MapScreen));
            }

            this.player.Update(this.Game.DeltaTime);

            this.camera.Follow(this.player);

            this.UpdateEnemies();
            this.UpdateSoldiers();
            this.UpdateHomelesses();
            this.UpdateCoins();
            this.UpdateCollisions();
            this.UpdateDayPhase();
        }

        private void UpdateEnemies()
        {
            // create enemy?
            // at night AND in first half on night AND random
            if (this.dayPhase == DayPhase.Night && this.dayPhaseTimer > (int)Enums.DayNightLength.Night / 2 && Tools.GetRandom(256) < 8)
            {
                Audio.PlayRandomSound("EnemySpawns");

                // choose direction
                if (Tools.GetRandom(2) == 0)
                {
                    this.enemies.Add(new Enemy(0, Offset.Floor, Direction.Right));
                }
                else
                {
                    this.enemies.Add(new Enemy(MapWidth, Offset.Floor, Direction.Left));
                }
            }

            // update enemies
            foreach (Enemy enemy in this.enemies)
            {
                enemy.Update(this.Game.DeltaTime);
            }
        }

        private void UpdateSoldiers()
        {
            // update
            foreach (var soldier in this.soldiers)
            {
                soldier.Update(this.Game.DeltaTime);
            }
        }

        private void UpdateHomelesses()
        {
            // create new?
            if (Tools.GetRandom(2000) < 2)
            {
                // choose side 
                if (Tools.GetRandom(2) == 0)
                {
                    this.homelesses.Add(new Homeless(0, Offset.Floor, Direction.Right));
                }
                else
                {
                    this.homelesses.Add(new Homeless(MapWidth, Offset.Floor, Direction.Left));
                }
            }

            // update 
            foreach (var homeless in this.homelesses)
            {
                homeless.Update(this.Game.DeltaTime);
            }
        }

        private void UpdateCoins()
        {
            // create new?
            if (Tools.GetRandom(1000) < 2)
            {
                this.coins.Add(new Coin(Tools.GetRandom(VillageScreen.MapWidth), Offset.Floor2));
            }

            // update 
            foreach (var coin in this.coins)
            {
                coin.Update(this.Game.DeltaTime);
            }
        }

        private void UpdateCollisions()
        {
            // enemies
            foreach (Enemy enemy in this.enemies.Where(enemy => enemy.Dead == false))
            {
                // enemies and bullets
                foreach (Bullet bullet in this.player.Bullets)
                {
                    if (enemy.Hitbox.Intersects(bullet.Hitbox))
                    {
                        bullet.ToDelete = true;

                        if (!enemy.TakeHit(bullet.Caliber))
                        {
                            enemy.Dead = true;
                            Audio.PlayRandomSound("EnemyDeaths");
                            Game.MessageBuffer.AddMessage("Bullet kill");
                        }
                    }
                }

                // enemies and player
                if (!enemy.ToDelete && this.player.Hitbox.Intersects(enemy.Hitbox))
                {
                    enemy.Dead = true;
                    Audio.PlayRandomSound("EnemyDeaths");
                    Game.MessageBuffer.AddMessage("Bare hands kill");

                    if (!this.player.TakeHit(enemy.Caliber))
                    {
                        this.Game.LoadScreen(typeof(Screens.GameOverScreen));
                    }
                }
            }
            

            // soldiers
            foreach (var soldier in this.soldiers.Where(soldier => soldier.Dead == false))
            {
                // enemies and soldiers
                foreach (var enemy in this.enemies.Where(enemy => enemy.Dead == false))
                {
                    if (enemy.Hitbox.Intersects(soldier.Hitbox))
                    {
                        if (!enemy.TakeHit(soldier.Caliber))
                        {
                            Audio.PlayRandomSound("EnemyDeaths");
                            enemy.Dead = true;
                        }
                        else if (!soldier.TakeHit(enemy.Caliber))
                        {
                            Audio.PlayRandomSound("SoldierDeaths");
                            soldier.Dead = true;
                        }
                    }
                }
            }

            // coins
            foreach (var coin in this.coins)
            {
                if (this.player.Hitbox.Intersects(coin.Hitbox))
                {
                    Audio.PlaySound("Coin");
                    this.player.Money++;
                    coin.ToDelete = true;
                    break;
                }
            }
            this.coins.RemoveAll(p => p.ToDelete);

            // buildings
            this.player.Action = null;
            foreach (var building in this.buildings)
            {
                if (this.player.Hitbox.Intersects(building.Hitbox))
                {
                    this.player.Action = Enums.PlayerAction.Build;
                    break;
                }
            }

            // homelesses
            foreach (var homeless in this.homelesses)
            {
                if (this.player.Hitbox.Intersects(homeless.Hitbox))
                {
                    this.player.Action = Enums.PlayerAction.Hire;

                    // hire homeless man?
                    if (Controls.Keyboard.HasBeenPressed(Keys.LeftControl) && this.player.Money >= homeless.Cost)
                    {
                        Audio.PlaySound("SoldierSpawn");
                        homeless.ToDelete = true;
                        this.player.Money -= homeless.Cost;
                        this.soldiers.Add(new Soldier(homeless.Hitbox.X, Offset.Floor, homeless.Direction, homeless.Health));
                    }
                    break;
                }
            }
            this.homelesses.RemoveAll(p => p.ToDelete);

            this.enemies.RemoveAll(p => p.ToDelete);
            this.soldiers.RemoveAll(p => p.ToDelete);
        }

        private void UpdateDayPhase()
        {
            this.dayPhaseTimer -= this.Game.DeltaTime;
            if (this.dayPhaseTimer <= 0)
            {
                
                if (this.dayPhase == DayPhase.Day)
                {
                    this.dayPhase = DayPhase.Night;
                    this.dayPhaseTimer = (int)DayNightLength.Night;
                }
                else
                {
                    this.player.Days++;
                    this.dayPhase = DayPhase.Day;
                    this.dayPhaseTimer = (int)DayNightLength.Day;
                }

                Audio.StopSong();
                Audio.CurrentSongCollection = this.dayPhase == DayPhase.Day ? "Day" : "Night";
            }
        }

        public override void Draw(GameTime gameTime)
        {
            this.Game.Matrix = this.camera.Transform;
            this.Game.DrawStart();

            // day or night sky
            this.GraphicsDevice.Clear(this.dayPhase == DayPhase.Day ? Color.CornflowerBlue : Color.DarkBlue);

            // background - tileset
            Assets.TilesetGroups["village1"].Draw("ground", this.Game.SpriteBatch);

            // stats
            this.Game.SpriteBatch.DrawString(
                Assets.Fonts["Small"],
                "Village " + this.Game.Village.ToString(),
                new Vector2(10 - this.camera.Transform.Translation.X, Offset.StatusBar + 10),
                Color.Black);
            this.Game.SpriteBatch.DrawString(
               Assets.Fonts["Small"],
               "Days " + this.player.Days.ToString(),
               new Vector2(10 - this.camera.Transform.Translation.X, Offset.StatusBar + 20),
               Color.Black);
            this.Game.SpriteBatch.DrawString(
                Assets.Fonts["Small"],
                "Timer: " + Math.Ceiling(this.dayPhaseTimer).ToString(),
                new Vector2(10 - this.camera.Transform.Translation.X, Offset.StatusBar + 30),
                Color.Black);
            this.Game.SpriteBatch.DrawString(
                Assets.Fonts["Small"],
                "Health: " + (this.player.Health).ToString(),
                new Vector2(10 - this.camera.Transform.Translation.X, Offset.StatusBar + 40),
                Color.Black);
            this.Game.SpriteBatch.DrawString(
                Assets.Fonts["Small"],
                "Money: " + (this.player.Money).ToString(),
                new Vector2(10 - this.camera.Transform.Translation.X, Offset.StatusBar + 50),
                Color.Black);

            // messages
            Game.MessageBuffer.Draw(this.Game.SpriteBatch, this.camera.Transform.Translation.X);

            // player - camera follows him
            this.player.Draw(this.Game.SpriteBatch);

            // game objects
            foreach (Building building in this.buildings)
            {
                building.Draw(this.Game.SpriteBatch);
            }

            foreach (Enemy enemy in this.enemies)
            {
                enemy.Draw(this.Game.SpriteBatch);
            }

            foreach (Soldier soldier in this.soldiers)
            {
                soldier.Draw(this.Game.SpriteBatch);
            }

            foreach (Homeless homeless in this.homelesses)
            {
                homeless.Draw(this.Game.SpriteBatch);
            }

            foreach (Coin coin in this.coins)
            {
                coin.Draw(this.Game.SpriteBatch);
            }

            this.Game.DrawEnd();
        }

        private void Load()
        {
            dynamic saveData = this.saveFile.Load();
            if (saveData == null)
            {
                return;
            }

            if (saveData.ContainsKey("player"))
            {
                this.player.Load(saveData.GetValue("player"));
            }

            if (saveData.ContainsKey("enemies"))
            {
                foreach (var enemy in saveData.GetValue("enemies"))
                {
                    if ((bool)enemy.Dead)
                    {
                        continue;
                    }
                    this.enemies.Add(new Enemy((int)enemy.Hitbox.X, (int)enemy.Hitbox.Y, (Direction)enemy.Direction, (int)enemy.Health, (int)enemy.Caliber));
                }
            }

            if (saveData.ContainsKey("soldiers"))
            {
                foreach (var soldier in saveData.GetValue("soldiers"))
                {
                    if ((bool)soldier.Dead)
                    {
                        continue;
                    }
                    this.soldiers.Add(new Soldier((int)soldier.Hitbox.X, (int)soldier.Hitbox.Y, (Direction)soldier.Direction, (int)soldier.Health, (int)soldier.Caliber));
                }
            }

            if (saveData.ContainsKey("homelesses"))
            {
                foreach (var homeless in saveData.GetValue("homelesses"))
                {
                    this.homelesses.Add(new Homeless((int)homeless.Hitbox.X, (int)homeless.Hitbox.Y, (Direction)homeless.Direction, (int)homeless.Health));
                }
            }

            if (saveData.ContainsKey("coins"))
            {
                foreach (var coin in saveData.GetValue("coins"))
                {
                    this.coins.Add(new Coin((int)coin.Hitbox.X, (int)coin.Hitbox.Y));
                }
            }

            if (saveData.ContainsKey("dayPhase") && saveData.ContainsKey("dayPhaseTimer"))
            {
                this.dayPhase = (DayPhase)saveData.GetValue("dayPhase");
                this.dayPhaseTimer = (double)saveData.GetValue("dayPhaseTimer");
            }

            Game.MessageBuffer.AddMessage("Game loaded");
        }

        private void Save()
        {
            this.saveFile.Save(new
            {
                player = this.player,
                enemies = this.enemies,
                soldiers = this.soldiers,
                homelesses = this.homelesses,
                coins = this.coins,
                dayPhase = this.dayPhase,
                dayPhaseTimer = this.dayPhaseTimer,
                village = this.Game.Village,
            });
        }
    }
}
