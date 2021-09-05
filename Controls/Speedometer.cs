using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rendannor.SpeedIndicator.Controls
{
    public class Speedometer : Control
    {

        #region Load Static

        private static readonly Texture2D _speedNeedle;
        private static readonly Texture2D _speedCounter;

        static Speedometer()
        {
            _speedNeedle = Module.ModuleInstance.ContentsManager.GetTexture("speed_needle.png");
            _speedCounter = Module.ModuleInstance.ContentsManager.GetTexture("speed_counter.png"); 
        }

        #endregion

        public int MinSpeed = 0;
        public float MaxSpeed = 2000;
        public float Speed { get; set; } = 0;
        private float DrawedOnIndicatorSpeed = 0;

        public bool ShowSpeedValue { get; set; } = false;

        public Speedometer()
        {
            this.ClipsBounds = true;
            this.Size = new Point(256, 128);

            UpdateLocation(null, null);

            Graphics.SpriteScreen.Resized += UpdateLocation;
        }

        private void UpdateLocation(object sender, EventArgs e)
        {
            this.Location = new Point(Graphics.SpriteScreen.Width / 2 - 128, Graphics.SpriteScreen.Height - 210);
        }

        protected override CaptureType CapturesInput() => CaptureType.ForceNone;

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            DrawedOnIndicatorSpeed = Speed;
            float ang = (float)(this.DrawedOnIndicatorSpeed / MaxSpeed * 3);

            spriteBatch.DrawOnCtrl(this,
                                   _speedNeedle,
                                   new Rectangle(_size.X / 2, 128, 256, 256),
                                   null,
                                   Color.GreenYellow,
                                   ang,
                                   new Vector2(_speedNeedle.Bounds.Width / 2, 128)); 

            spriteBatch.DrawOnCtrl(this,
                                   _speedCounter,
                                   _size.InBounds(bounds),
                                   null,
                                   Color.White,
                                   0f,
                                   Vector2.Zero);

            if (this.ShowSpeedValue)
            {
                spriteBatch.DrawStringOnCtrl(this,
                                             Math.Round(this.Speed).ToString(),
                                             Content.DefaultFont32,
                                             new Rectangle(0, 0, _size.X, 50),
                                             Color.White,
                                             false);
            }
        }

    }
}
