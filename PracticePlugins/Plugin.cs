using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using PracticePlugins.Plugins;
using static PracticePlugins.GunGame;

namespace PracticePlugins
{
    public enum EventType
    {
        NONE = 0,

        Infection = 1,
        Battle = 2,
        Hush = 3,
        Gungame = 4
    }


    public class Plugin
    {
        public static bool EventInProgress => CurrentEvent != EventType.NONE;
        public static EventType CurrentEvent = EventType.NONE;

        [PluginEntryPoint("Practice Plugins", "1.0.0", "My collection of random SL plugins", "SpiderBuh")]
        public void OnPluginStart()
        {
            Log.Info($"Plugin is loading...");

            EventManager.RegisterEvents<Events>(this);
            //EventManager.RegisterEvents<Pocket914Cards>(this);
            EventManager.RegisterEvents<GunGame>(this);
            //EventManager.RegisterEvents<DryFireFunni>(this);
            EventManager.RegisterEvents<GunGameEventCommand>(this);

        }


    }
}
