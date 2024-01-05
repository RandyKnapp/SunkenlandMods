using System.Reflection;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterQuickSlots
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BetterQuickSlots : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.betterquickslots";
        public const string NAME = "Better Quick Slots";
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
    public static class UIInventoryStorageSlotPatch
    {
        [HarmonyPatch(typeof(UIInventoryStorageSlot), nameof(UIInventoryStorageSlot.Init))]
        [HarmonyPostfix]
        public static void Init_Postfix(UIInventoryStorageSlot __instance, int index)
        {
            if (!(__instance is UIQuickSlot))
                return;

            var displayText = __instance.transform.Find("slotDisplayText");
            if (displayText != null)
                return;

            var go = Object.Instantiate(__instance.transform.Find("tmpAmount").gameObject, __instance.transform);
            go.name = "slotDisplayText";
            go.SetActive(true);
            var rt = (RectTransform)go.transform;

            rt.localPosition = new Vector3(0, 0);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 30);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            switch (index)
            {
                default: t.text = $"[{index + 1}]"; break;
                case 9:  t.text = "[0]";            break;
                case 10: t.text = "[-]";            break;
                case 11: t.text = "[=]";            break;
            }
        }
    }

    // UICombat.RefreshQuickSlot(): add 4 new slots
    [HarmonyPatch]
    public static class UICombatPatch
    {
        [HarmonyPatch(typeof(UICombat), "RefreshQuickSlot")]
        [HarmonyPrefix]
        public static bool RefreshQuickSlot_Prefix(UICombat __instance)
        {
            if (Global.code.Player.quickSlotStorage.MaxItemsAmount < 12)
            {
                Global.code.Player.quickSlotStorage.MaxItemsAmount = 12;

                var armorPanel = (RectTransform)__instance.transform.Find("Root/Combat GUI Group/Player Health Panel/Armor Panel");
                armorPanel.anchoredPosition = new Vector2(418, 0);

                var prefab = __instance.quickSlotIconGroup.GetChild(0);
                for (var i = 0; i < 4; ++i)
                {
                    var newQuickSlot = Object.Instantiate(prefab.gameObject, __instance.quickSlotIconGroup);
                    newQuickSlot.name = $"QuickSlot_{9+i}";
                }
            }
            return true;
        }

        // modified copy of local function in UICombat.CalculateQuickSlotIndex
        private static void SelectNextItemOnQuickBar(int step)
        {
            var quickSlotStorage = Global.code.Player.quickSlotStorage;
            var uiCombat = Global.code.uiCombat;

            var count = quickSlotStorage.Items.Count;

            var currentIndex = 0;
            if (!uiCombat.QuickSlotIndex.HasValue)
            {
                if (step > 0)
                    uiCombat.QuickSlotIndex = 0;
                else if (step < 0)
                    uiCombat.QuickSlotIndex = count - 1;
            }
            else
            {
                currentIndex = uiCombat.QuickSlotIndex.Value;
            }

            var nextIndex = (count + currentIndex + step) % count;
            var overflowCounter = 0;
            while (overflowCounter < count && quickSlotStorage.GetItemAtIndex(nextIndex) == null)
            {
                nextIndex = (count + nextIndex + step) % count;
                overflowCounter++;
            }
            uiCombat.QuickSlotIndex = nextIndex;
        }

        [HarmonyPatch(typeof(UICombat), "CalculateQuickSlotIndex")]
        [HarmonyPostfix]
        public static void CalculateQuickSlotIndex_Postfix(UICombat __instance)
        {
            if (Global.code.Player.IsBusy || Global.code.Player.CurHelicopter != null || Global.code.uiCombat.IsFocusedInteraction || Global.code.Player.IsDead)
                return;

            var uiMain = __instance.transform.parent.Find("UIMain");
            var uiGameMenu = __instance.transform.parent.Find("UIGameMenu");
            if (!uiMain.gameObject.activeSelf && !uiGameMenu.gameObject.activeSelf)
            {
                if (Input.mouseScrollDelta.y > 0)
                {
                    SelectNextItemOnQuickBar(-1);
                }
                else if (Input.mouseScrollDelta.y < 0)
                {
                    SelectNextItemOnQuickBar(1);
                }
            }
        }
    }

    // PlayerCharacter.UpdateKeys(): add input for new slots, add scroll wheel input
    [HarmonyPatch]
    public static class PlayerCharacterPatch
    {
        private static void OnQuickSlotPressed(PlayerCharacter __instance, int index)
        {
            __instance.UseItem(__instance.quickSlotStorage.GetItemAtIndex(index), true);
            Global.code.uiCombat.QuickSlotIndex = new int?(index);
        }

        [HarmonyPatch(typeof(PlayerCharacter), "UpdateKeys")]
        [HarmonyPrefix]
        public static bool UpdateKeys_Prefix(PlayerCharacter __instance)
        {
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                OnQuickSlotPressed(__instance, 8);
                return false;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                OnQuickSlotPressed(__instance, 9);
                return false;
            }
            else if (Input.GetKeyDown(KeyCode.Minus))
            {
                OnQuickSlotPressed(__instance, 10);
                return false;
            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {
                OnQuickSlotPressed(__instance, 11);
                return false;
            }

            if (Global.code.Player.IsBusy || Global.code.Player.CurHelicopter != null || Global.code.uiCombat.IsFocusedInteraction || Global.code.Player.IsDead)
                return true;

            if (Global.code.uiCombat.QuickSlotIndex.HasValue && Input.GetMouseButtonDown(2))
            {
                var selectedQuickSlotItem = Global.code.uiCombat.SelectedQuickSlotItem;
                if (selectedQuickSlotItem != null)
                {
                    __instance.UseItem(selectedQuickSlotItem, true);
                    return false;
                }
            }

            return true;
        }
    }
}
