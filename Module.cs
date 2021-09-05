using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Rendannor.SpeedIndicator.Controls;
using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;

namespace Rendannor.SpeedIndicator
{

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {

        internal static Module ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        private SettingEntry<bool> _settingOnlyShowAtHighSpeeds;
        private SettingEntry<bool> _settingShowSpeedNumber;
        private SettingEntry<bool> _settingIgnoreVerticalAxis;

        private Speedometer _speedometer;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings)
        {
            _settingOnlyShowAtHighSpeeds = settings.DefineSetting("OnlyShowAtHighSpeeds", false, "Only Show at High Speeds", "Only show the speedometer if you're going at least 1/4 the max speed.");
            _settingShowSpeedNumber = settings.DefineSetting("ShowSpeedNumber", false, "Show Speed Value", "Shows the speed (in approx. inches per second) above the speedometer.");
            _settingIgnoreVerticalAxis = settings.DefineSetting("IgnoreVerticalAxis", false, "Ignore Vertical Axis", "Ignore the vertical axis (Z axis) from the speed calculation");
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            _speedometer = new Speedometer()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Speed = 0
            };

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private Vector3 _lastPos = Vector3.Zero;
        private long _lastUpdate = 0;
        private double _leftOverTime = 0;
        private readonly Queue<double> _sampleBuffer = new Queue<double>();
        private double _lastUsedGameTime = 0;

        protected override void Update(GameTime gameTime)
        {
            if (_lastUsedGameTime == 0 || gameTime.TotalGameTime.TotalMilliseconds >= _lastUsedGameTime + 60)
            {
                // Unless we're in game running around, don't show the speedometer
                if (!GameService.GameIntegration.IsInGame)
                {
                    _speedometer.Visible = false;
                    _lastPos = Vector3.Zero;
                    _sampleBuffer.Clear();
                    return;
                }

                _leftOverTime = gameTime.ElapsedGameTime.TotalSeconds * 4;

                if (_lastPos != Vector3.Zero && _lastUpdate != GameService.Gw2Mumble.Tick)
                {
                    double velocity = 0;
                    if (_settingIgnoreVerticalAxis.Value)
                    {
                        velocity = Vector2.Distance(new Vector2(GameService.Gw2Mumble.PlayerCharacter.Position.X, GameService.Gw2Mumble.PlayerCharacter.Position.Y), new Vector2(_lastPos.X, _lastPos.Y)) * 39.3700787f / _leftOverTime;
                    }
                    else
                    {
                        velocity = Vector3.Distance(GameService.Gw2Mumble.PlayerCharacter.Position, _lastPos) * 39.3700787f / _leftOverTime;
                    }
                    _leftOverTime = 0;
                    _sampleBuffer.Enqueue(velocity);
                    if (_sampleBuffer.Count > 2)
                    {
                        double sped = _sampleBuffer.Average(i => i);
                        _speedometer.Speed = (float)Math.Round(sped, 1);
                        _speedometer.Visible = !_settingOnlyShowAtHighSpeeds.Value || _speedometer.Speed / _speedometer.MaxSpeed >= 0.25;
                        _speedometer.ShowSpeedValue = _settingShowSpeedNumber.Value;
                        int nomberOfDequeue = getBufferMaxSetting(sped, velocity, _sampleBuffer.Count);
                        if (nomberOfDequeue != 0)
                        {
                            for (int i = 0; i < nomberOfDequeue; i++)
                            {
                                _sampleBuffer.Dequeue();
                            }
                        }
                    }
                }

                _lastPos = GameService.Gw2Mumble.PlayerCharacter.Position;
                _lastUpdate = GameService.Gw2Mumble.Tick;
                _lastUsedGameTime = gameTime.TotalGameTime.TotalMilliseconds;
            }
        }
        private int getBufferMaxSetting(double averageSpeedValue, double actualVelocity, int numberofValueInqueue)
        {
            if (actualVelocity > averageSpeedValue * 5 || actualVelocity < averageSpeedValue * 0.125)
            {
                return (int)(numberofValueInqueue * 0.5);
            }
            else if (actualVelocity > averageSpeedValue * 4 || actualVelocity < averageSpeedValue * 0.25)
            {
                return (int)(numberofValueInqueue * 0.25);
            }
            else if (actualVelocity > averageSpeedValue * 3 || actualVelocity < averageSpeedValue * 0.33)
            {
                return (int)(numberofValueInqueue * 0.12);
            }
            else if (actualVelocity > averageSpeedValue * 2 || actualVelocity < averageSpeedValue * 0.5)
            {
                return (int)(numberofValueInqueue * 0.06);
            }
            else if (actualVelocity > averageSpeedValue * 1.5 || actualVelocity < averageSpeedValue * 0.75)
            {
                return (int)(numberofValueInqueue * 0.03);
            }
            else if (actualVelocity > averageSpeedValue * 1.2 || actualVelocity < averageSpeedValue * 0.9)
            {
                return (int)(numberofValueInqueue * 0.01);
            }
            else if (actualVelocity > averageSpeedValue * 1.1 || actualVelocity < averageSpeedValue * 0.95)
            {
                return (int)(numberofValueInqueue * 0.01);
            }
            else if (numberofValueInqueue < 100)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        /// <inheritdoc />
        protected override async void Unload()
        {
            // Unload
            await GameService.Gw2WebApi.AnonymousConnection.Connection.CacheMethod.ClearAsync();

            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
