using Aurora.Settings;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aurora.Profiles.Move_or_Die
{
    public class MoD : Application
    {
        public MoD()
            : base(new LightEventConfig { Name = "Move or Die", ID = "MoD", ProcessNames = new[] { "love.exe" }, ProfileType = typeof(WrapperProfile), OverviewControlType = typeof(Control_MoD), GameStateType = typeof(GameState_Wrapper), Event = new GameEvent_Generic(), IconURI = "Resources/MoD.png" })
        {
            Config.ExtraAvailableLayers.Add("WrapperLights");
        }
    }
}
