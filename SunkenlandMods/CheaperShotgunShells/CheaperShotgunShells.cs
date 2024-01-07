using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CheaperShotgunShells
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CheaperShotgunShells : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.cheapershotgunshells";
        public const string NAME = "Cheaper Shotgun Shells";
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
    public static class WorkstationPatch
    {
        private static bool _patchComplete = false;

        [HarmonyPatch(typeof(Workstation), nameof(Workstation.Spawned))]
        [HarmonyPrefix]
        public static void Spawned_Prefix()
        {
            if (_patchComplete)
                return;

            var shotgunAmmoCraftable = RM.code.AllCraftables.FirstOrDefault(x => x.name == "Shotgun Ammo");
            if (shotgunAmmoCraftable != null)
            {
                //CheaperShotgunShells.logger.LogWarning("Found Shotgun Shells!");
                var req = shotgunAmmoCraftable.itemRequirements.FirstOrDefault(x => x.item.name == "A3_Components");
                if (req != null)
                {
                    req.amount = 1;
                    _patchComplete = true;
                }
                else
                {
                    //CheaperShotgunShells.logger.LogWarning("Did not find Component requirement!");
                }
            }
            else
            {
                //CheaperShotgunShells.logger.LogWarning("Did not find Shotgun Shells!");
            }
        }
    }
}
