using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace NoBedtime
{
	[BepInPlugin(GUID, NAME, VERSION)]
    public class NoBedtime : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.nobedtime";
        public const string NAME = "No Bedtime";
        public const string VERSION = "0.1.0";

        public static ConfigEntry<int> SleepHoursOverride;
        public static ConfigEntry<int> BedtimeHoursOverride;

        public static ManualLogSource logger;

        public void Awake()
        {
            logger = Logger;

            SleepHoursOverride = Config.Bind("Config", "Sleep Hours Override", -1, "The number of in-game hours to pass when sleeping. If set to -1, does not override the game's default value (10 hours).");
            BedtimeHoursOverride = Config.Bind("Config", "Bedtime Hours Override", 0, "The number of in-game hours before you can sleep again. If set to -1, does not override the game's default value (24 hours).");
            Logger.LogWarning($"No Bedtime Loaded");
            Logger.LogWarning($"- Sleep Hours Override: {SleepHoursOverride.Value}");
            Logger.LogWarning($"- Bedtime Hours Override: {BedtimeHoursOverride.Value}");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7) && GlobalDataHelper.IsGlobalDataValid())
            {
                Mainframe.code.M_GlobalData.CanSleep = true;
                Mainframe.code.M_GlobalData.SleepCoolingTime = 0;
            }

            if (Input.GetKeyDown(KeyCode.F8) && GlobalDataHelper.IsGlobalDataValid())
            {
                Logger.LogWarning($"           CanSleep: {Mainframe.code.M_GlobalData.CanSleep}");
                Logger.LogWarning($"   SleepCoolingTime: {Mainframe.code.M_GlobalData.SleepCoolingTime}");
                Logger.LogWarning($"MaxSleepCoolingTime: {Mainframe.code.M_GlobalData.MaxSleepCoolingTime}");
            }
        }
    }

    [HarmonyPatch]
    public static class GlobalDataPatch
    {
        [HarmonyPatch(typeof(GlobalData), nameof(GlobalData.SetNextSleepTime))]
        [HarmonyPostfix]
        public static void SetNextSleepTime_Postfix()
        {
            if (NoBedtime.SleepHoursOverride.Value >= 0)
            {
                Mainframe.code.M_GlobalData.SleepTime = NoBedtime.SleepHoursOverride.Value;
            }

            var bedtimeHours = (NoBedtime.BedtimeHoursOverride.Value < 0) ? 24 : NoBedtime.BedtimeHoursOverride.Value;
            Mainframe.code.M_GlobalData.MaxSleepCoolingTime = Mathf.Max(0, (bedtimeHours * 60) - (Mainframe.code.M_GlobalData.SleepTime * 60));
            Mainframe.code.M_GlobalData.CanSleep = Mainframe.code.M_GlobalData.MaxSleepCoolingTime == 0;

            NoBedtime.logger.LogWarning("Got here");
        }
    }
}
