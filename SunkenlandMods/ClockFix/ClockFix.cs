using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;

namespace ClockFix
{
	[BepInPlugin(GUID, NAME, VERSION)]
    public class ClockFix : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.clockfix";
        public const string NAME = "Clock Fix";
        public const string VERSION = "0.1.0";

        public static ConfigEntry<bool> Use12HourTime;

        public void Awake()
        {
            Use12HourTime = Config.Bind("Config", "Use 12 Hour Time", true, "If true, displays the clock using 12 hour time with AM/PM indicators. If false, displays 24 hour time.");

            Logger.LogWarning($"{NAME} Loaded");
            Logger.LogWarning($"- {Use12HourTime.Definition.Key}: {Use12HourTime.Value}");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }
    }

    [HarmonyPatch(typeof(UICombat), "Update")]
    public static class UICombat_Update_Patch
    {
        public static void Postfix(Text ___txtHour, Text ___txtTimer)
        {
            var hour = EnviroSkyMgr.instance.GetCurrentHour();
            var minute = EnviroSkyMgr.instance.GetCurrentMinute();

            if (ClockFix.Use12HourTime.Value)
            {
                var hourDisplay = hour % 12;
                if (hourDisplay == 0)
                    hourDisplay = 12;
                ___txtHour.text = $"{hourDisplay}:{minute:D2} {(hour >= 12 ? "PM" : "AM")}";
            }
            else
            {
                ___txtHour.text = $"{hour}:{minute:D2}";
            }

            ___txtTimer.gameObject.SetActive(false);
        }
    }
}
