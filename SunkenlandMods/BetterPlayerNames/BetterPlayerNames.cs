using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Crest;
using Fusion;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BetterPlayerNames
{
	[BepInPlugin(GUID, NAME, VERSION)]
    public class BetterPlayerNames : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.betterplayernames";
        public const string NAME = "Better Player Names";
        public const string VERSION = "0.1.0";

        public static ConfigEntry<float> NearThreshold;
        public static ConfigEntry<float> FarThreshold;
        public static ConfigEntry<float> HiddenThreshold;
        public static ConfigEntry<float> NearScale;
        public static ConfigEntry<float> FarScale;

        public static ManualLogSource logger;

        public void Awake()
        {
            logger = Logger;

            NearThreshold = Config.Bind("Config", "Near Threshold", 30.0f, "Distance before the player name begins scaling down.");
            FarThreshold = Config.Bind("Config", "Far Threshold", 100.0f, "Maximum distance to scale the player name.");
            HiddenThreshold = Config.Bind("Config", "Hidden Threshold", -1.0f, "If greater than zero, if the player is farther than this threshold, the player name does not show.");
            NearScale = Config.Bind("Config", "Near Scale", 1.0f, "Scale of the player name when it is at or closer than the Near Threshold.");
            FarScale = Config.Bind("Config", "Far Scale", 0.3f, "Scale of the player name when it is at or farther than the Far Threshold.");

            Logger.LogWarning($"{NAME} Loaded");
            Logger.LogWarning($"- {NearThreshold.Definition.Key}: {NearThreshold.Value}");
            Logger.LogWarning($"- {FarThreshold.Definition.Key}: {FarThreshold.Value}");
            Logger.LogWarning($"- {HiddenThreshold.Definition.Key}: {HiddenThreshold.Value}");
            Logger.LogWarning($"- {NearScale.Definition.Key}: {NearScale.Value}");
            Logger.LogWarning($"- {FarScale.Definition.Key}: {FarScale.Value}");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }
    }

    [HarmonyPatch]
    public static class PlayerDummyPatch
    {
        private static readonly FieldInfo NameHintFieldInfo = typeof(PlayerDummy).GetField("NameHint", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RefreshDisplayOfHintMethodInfo = typeof(PlayerDummy).GetMethod("RefreshDisplayOfHint", BindingFlags.NonPublic | BindingFlags.Instance);

        private static int _counter = 0;

        [HarmonyPatch(typeof(PlayerDummy), "RefreshDisplayOfHint")]
        [HarmonyPrefix]
        public static bool RefreshDisplayOfHint_Prefix(PlayerDummy __instance)
        {
            if (!WorldScene.code)
                return false;

            foreach (var dummyTransform in WorldScene.code.allPlayerDummies.items)
            {
                if (dummyTransform == __instance.transform || dummyTransform == null)
                    continue;

                var dummy = dummyTransform.GetComponent<PlayerDummy>();

                var d = dummyTransform.position - FPSPlayer.code.transform.position;
                var distance = d.magnitude;
                var hiddenThreshold = BetterPlayerNames.HiddenThreshold.Value;
                var angle = Utility.ContAngle(d, OceanRenderer.Instance.ViewCamera.transform.forward, Vector3.up);
                if ((hiddenThreshold < 0 || distance < hiddenThreshold) && dummyTransform.parent == null && angle > -90.0f && angle < 90.0f)
                {
                    dummy.ChangeDisplayOfHint(true);
                }
                else
                {
                    dummy.ChangeDisplayOfHint(false);
                    continue;
                }

                var nearThreshold = BetterPlayerNames.NearThreshold.Value;
                var farThreshold = BetterPlayerNames.FarThreshold.Value;
                var nearScale = BetterPlayerNames.NearScale.Value;
                var farScale = BetterPlayerNames.FarScale.Value;

                var nameHint = NameHintFieldInfo.GetValue(dummy) as Transform;
                if (nameHint == null)
                {
                    if (_counter % 60 == 0)
                        BetterPlayerNames.logger.LogWarning("Could not get NameHint");
                    _counter++;
                    continue;
                }

                var scale = Mathf.Lerp(nearScale, farScale, Mathf.InverseLerp(nearThreshold, farThreshold, distance));
                nameHint.localScale = new Vector3(scale, scale, 1);

                nameHint.GetComponent<Text>().text = $"{dummy.CharacterName} ({(int)distance}m)";

                var outline = nameHint.GetComponent<UnityEngine.UI.Outline>();
                if (outline == null)
                {
                    outline = nameHint.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(2, 2);
                }
            }

            return false;
        }

        //private static void CharacterNameChanged(Changed<PlayerDummy> changed)
        [HarmonyPatch(typeof(PlayerDummy), "CharacterNameChanged")]
        [HarmonyPostfix]
        private static void CharacterNameChanged_Postfix(Changed<PlayerDummy> changed)
        {
            var behavior = changed.Behaviour;
            //BetterPlayerNames.logger.LogWarning($"Player name changed ({behavior.CharacterName})");
            RefreshDisplayOfHintMethodInfo.Invoke(behavior, new object[]{});
        }
    }

}
