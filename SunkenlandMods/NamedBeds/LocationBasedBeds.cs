using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Fusion;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace NamedBeds
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class LocationBasedBeds : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.locationbasedbeds";
        public const string NAME = "Location Based Beds";
        public const string VERSION = "0.1.0";

        //public static ManualLogSource logger;

        public void Awake()
        {
            //logger = Logger;

            Logger.LogWarning($"{NAME} Loaded");
            //Logger.LogWarning($"- {Config.Definition.Key}: {Config.Value}");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }
    }

    [HarmonyPatch]
    public static class UIDeathPatch
    {
        private static Location FindClosestLocation(Vector3 worldPos)
        {
            //LocationBasedBeds.logger.LogWarning("Find Closest Location:");
            Location closest = null;
            var distanceSq = float.MaxValue;
            foreach (var location in WorldScene.code.locationsList)
            {
                var d = worldPos - location.mapLocator.position;
                var dSq = d.sqrMagnitude;
                //LocationBasedBeds.logger.LogWarning($"    {location.locationName}: {Mathf.Sqrt(dSq):N1}");
                if (dSq < distanceSq)
                {
                    distanceSq = dSq;
                    closest = location;
                }
            }

            return closest;
        }

        [HarmonyPatch(typeof(UIDeath), nameof(UIDeath.Open))]
        [HarmonyPostfix]
        public static void Open_Postfix(UIDeath __instance)
        {
            var interactButtons = Traverse.Create(__instance).Field<List<Button>>("InteractBtns").Value;

            for (var index = 0; index < WorldScene.code.Beds.Count; ++index)
            {
                var bed = WorldScene.code.Beds[index];
                if (Mainframe.code.WorldManager.IsSpawnPointShared || !(bed.BuilderID != Mainframe.code.SaveManager.CurrentCharacterGuid))
                {
                    var button = interactButtons[index];
                    var closestLocation = FindClosestLocation(bed.transform.position);
                    button.GetComponentInChildren<Text>().text = closestLocation.locationName;
                }
            }
        }
    }
}
