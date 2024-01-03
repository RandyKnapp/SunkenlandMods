using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

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
        public static ConfigEntry<int> AlarmHour;

        public static ManualLogSource logger;

        public void Awake()
        {
            logger = Logger;

            SleepHoursOverride = Config.Bind("Config", "Sleep Hours Override", -1, "The number of in-game hours to pass when sleeping. If set to -1, does not override the game's default value (10 hours).");
            BedtimeHoursOverride = Config.Bind("Config", "Bedtime Hours Override", 0, "The number of in-game hours before you can sleep again. If set to -1, does not override the game's default value (24 hours).");
            AlarmHour = Config.Bind("Config", "Alarm Hour", -1, "What time of day to wake, regardless of when you slept. If set to -1, does nothing. If in use, 'Sleep Hours Override' is ignored.");
            Logger.LogWarning("No Bedtime Loaded");
            Logger.LogWarning($"- Sleep Hours Override: {SleepHoursOverride.Value}");
            Logger.LogWarning($"- Bedtime Hours Override: {BedtimeHoursOverride.Value}");
            Logger.LogWarning($"- Alarm Hour: {AlarmHour.Value}");

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
        private static int GetMinutesSinceSleep()
        {
            if (!GlobalDataHelper.IsGlobalDataValid())
                return 0;

            if (EnviroSkyMgr.instance.GetCurrentHour() >= 24)
                return 0;

            var globalData = Mainframe.code.M_GlobalData;
            var sleepStart = new DateTime(2024, 1, 1, globalData.SleepHour, globalData.SleepMinute, 0);
            var now = new DateTime(2024, 1, EnviroSkyMgr.instance.GetCurrentHour() < globalData.SleepHour ? 2 : 1, EnviroSkyMgr.instance.GetCurrentHour(), EnviroSkyMgr.instance.GetCurrentMinute(), 0);
            var timeSinceSleep = now - sleepStart;
            return (int)timeSinceSleep.TotalMinutes;
        }

        [HarmonyPatch(typeof(Bed), nameof(Bed.GetSleepState))]
        [HarmonyPrefix]
        public static bool Bed_GetSleepState_Prefix(ref float __result)
        {
            if (!GlobalDataHelper.IsGlobalDataValid())
                return true;

            if (Mainframe.code.M_GlobalData.MaxSleepCoolingTime == 0)
            {
                __result = 0;
                return false;
            }

            var minutesSinceSleep = GetMinutesSinceSleep();
            __result = minutesSinceSleep / (float)Mainframe.code.M_GlobalData.MaxSleepCoolingTime;
            return false;
        }

        [HarmonyPatch(typeof(GlobalData), nameof(GlobalData.SetNextSleepTime))]
        [HarmonyPostfix]
        public static void SetNextSleepTime_Postfix(GlobalData __instance)
        {
            var sleepTime = __instance.SleepTime;
            var useAlarm = NoBedtime.AlarmHour.Value >= 0;

            if (useAlarm)
            {
                __instance.SleepTime = 0;
                var currentHour = EnviroSkyMgr.instance.GetCurrentHour();
                sleepTime = (24 + NoBedtime.AlarmHour.Value - currentHour) % 24;
                if (sleepTime == 0)
                    sleepTime = 24;
                NoBedtime.logger.LogWarning($"Alarm set for {NoBedtime.AlarmHour.Value}. Sleeping for {sleepTime} hours");
                
            }
            else if (NoBedtime.SleepHoursOverride.Value >= 0)
            {
                __instance.SleepTime = sleepTime = NoBedtime.SleepHoursOverride.Value;
            }

            var bedtimeHours = (NoBedtime.BedtimeHoursOverride.Value < 0) ? 24 : NoBedtime.BedtimeHoursOverride.Value;
            __instance.MaxSleepCoolingTime = Mathf.Max(0, bedtimeHours * 60);
            __instance.CanSleep = __instance.MaxSleepCoolingTime == 0;

            if (useAlarm && __instance.Object.HasStateAuthority)
            {
                __instance.SetCurTime((EnviroSkyMgr.instance.GetCurrentHour() + sleepTime));
            }
        }

        private static int lastTimeSinceSleep = 0;

        [HarmonyPatch(typeof(GlobalData), "Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(GlobalData __instance)
        {
            if (__instance.CanSleep)
                return;

            var minutesSinceSleep = GetMinutesSinceSleep();
            if (minutesSinceSleep != lastTimeSinceSleep)
            {
                lastTimeSinceSleep = minutesSinceSleep;
                NoBedtime.logger.LogInfo($"Time Since Sleep: {minutesSinceSleep} / ({__instance.MaxSleepCoolingTime})");
            }
            
            if (minutesSinceSleep >= __instance.MaxSleepCoolingTime)
            {
                NoBedtime.logger.LogWarning("Can Sleep!");
                __instance.CanSleep = true;
                __instance.SleepCoolingTime = 0;
            }
        }
    }
}
