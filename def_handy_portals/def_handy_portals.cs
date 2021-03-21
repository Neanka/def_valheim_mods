using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace def_handy_portals
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Def_handy_portals : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.def_handy_portals";
        const string pluginName = "DEF handy portals";
        const string pluginVersion = "1.0.0.5";
        public static ManualLogSource logger;

        private static ConfigEntry<string> configPortalNamePrefix;
        private static ConfigEntry<bool> configAlwaysShowPortalPins;
        private static ConfigEntry<bool> configAllowTeleportWithOre;
        private static ConfigEntry<bool> configMagicKeyEnabled;
        private static ConfigEntry<bool> configPortalNameMaxLength;
        private static ConfigEntry<KeyboardShortcut> configMagicKey;

        private Harmony _harmony;

        private static List<ZDO> tplist;
        private static Dictionary<ZDO, Minimap.PinData> tp_list;
        private static int tp_list_size = 0;
        private static bool isTPmode = false;
        private static bool list_populated = false;
        public static readonly string portal_name = "portal_wood";
        public static Minimap.PinType portal_pin_type = (Minimap.PinType)140;

        void Awake()
        {
            configPortalNamePrefix = Config.Bind("General", "Portal name prefix", "$piece_portal:\xA0", "Prefix will added to portal pin name");
            configAlwaysShowPortalPins = Config.Bind("General", "Show portals on map", false, "Always show portal pins on map");
            configAllowTeleportWithOre = Config.Bind("Cheat", "Allow teleport with restricted items", false, "Allow teleport with restricted items");
            configPortalNameMaxLength = Config.Bind("Utility", "Remove portal name length rectriction (need game restart)", false, "Allow override default 10 characters limit");
            configMagicKeyEnabled = Config.Bind("Debug", "Enable", false, "Enable debug key");
            configMagicKey = Config.Bind("Debug", "debug key", new KeyboardShortcut(KeyCode.F9), "debug key");

            logger = Logger;
            logger.LogInfo("Hello, world!");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.F9))
            if (configMagicKeyEnabled.Value && configMagicKey.Value.IsDown())
            {
                logger.LogInfo("debug button pressed");
                ZRoutedRpc.instance.InvokeRoutedRPC("RequestPortalZDOs", new object[] { new ZPackage() });
                //logger.LogWarning(" Manual ResetPins ");
                //tplist = null;
                //tp_list = null;
                //ResetPins();
            }

        }

        [HarmonyPatch(typeof(Game), "Start")]
        public static class GameStartPatch
        {
            private static void Prefix()
            {
                ZRoutedRpc.instance.Register("RequestPortalZDOs", new Action<long, ZPackage>(RPC.RequestPortalZDOs));
                ZRoutedRpc.instance.Register("ProcessZDOs", new Action<long, ZPackage>(RPC.ProcessZDOs));
            }
        }

        public static void ResetPins()
        {
            tplist = new List<ZDO>();
            tp_list = new Dictionary<ZDO, Minimap.PinData>();
            ZDOMan.instance.GetAllZDOsWithPrefab(portal_name, tplist);
            //logger.LogWarning("list size "+ tplist.Count);
            List<Minimap.PinData> m_pins = (List<Minimap.PinData>)GetInstanceField(Minimap.instance, "m_pins");
            foreach (Minimap.PinData item in m_pins.ToList())
            {
                if (item.m_type == portal_pin_type)
                {
                    Minimap.instance.RemovePin(item);
                }
            }
            foreach (ZDO item in tplist)
            {
                Minimap.PinData temp = Minimap.instance.AddPin(item.GetPosition(), portal_pin_type, GetLocalizedString(configPortalNamePrefix.Value) + item.GetString("tag"), false, false);
                tp_list.Add(item, temp);
                if (ZNet.instance.IsServer())
                {
                    ZDOMan.instance.ForceSendZDO(item.m_uid);
                }
            }
        }

        public static void Do_magic()
        {
            if (!list_populated)
            {
                logger.LogWarning("Do_magic");
                //ResetPins();
                list_populated = true;
                if (ZNet.instance.IsServer())
                {
                    ResetPins();
                }
                else
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("RequestPortalZDOs", new object[] { new ZPackage() });
                }
            }
        }

        public static string GetLocalizedString(string param)
        {
            return Localization.instance.Localize(param);
        }

        public static Sprite portal_icon;
        public static void LoadImages()
        {
            if (!portal_icon)
            {
                //string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //string filepath = Path.Combine(directoryName, "TPIcon.png");
                // logger.Log("loading texture");
                // string filepath = "E:\\Games\\SteamLibrary\\steamapps\\common\\Valheim\\BepInEx\\plugins\\TPIcon.png";
                // portal_icon = LoadSprite(filepath);
                portal_icon = Minimap.instance.m_icons.Find((Minimap.SpriteData x) => x.m_name == Minimap.PinType.Icon4).m_icon;
            }
        }

        public static Sprite LoadSprite(string fileName)
        {
            byte[] file = File.ReadAllBytes(fileName);
            if (file.Count() > 0)
            {
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(file))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100f);
                }
            }
            return null;
        }

        public static ZDO GetZDObyValue(Minimap.PinData data)
        {
            return tp_list.FirstOrDefault(x => x.Value == data).Key;
        }

        private static void EnterTPMode()
        {
            isTPmode = true;
            Transform largemap = Minimap.instance.m_selectedIcon0.transform.parent.parent.parent;
            largemap.GetChild(2).gameObject.SetActive(false);
            largemap.GetChild(3).gameObject.SetActive(false);
        }
        private static void LeaveTPMode()
        {
            isTPmode = false;
            Transform largemap = Minimap.instance.m_selectedIcon0.transform.parent.parent.parent;
            largemap.GetChild(2).gameObject.SetActive(true);
            largemap.GetChild(3).gameObject.SetActive(true);
            //MethodInfo dynMethod = Minimap.instance.GetType().GetMethod("UpdatePins", BindingFlags.NonPublic | BindingFlags.Instance);
            //dynMethod.Invoke(Minimap.instance, null);
        }

        [HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private class UpdatePins_Patch
        {
            static void Postfix(List<Minimap.PinData> ___m_pins)
            {
                if (isTPmode)
                {
                    foreach (Minimap.PinData pinData in ___m_pins)
                    {
                        if (pinData.m_type != portal_pin_type) if (pinData.m_uiElement) pinData.m_uiElement.gameObject.SetActive(false);
                    }
                }
                else if (!configAlwaysShowPortalPins.Value)
                {
                    foreach (Minimap.PinData pinData in ___m_pins)
                    {
                        if (pinData.m_type == portal_pin_type) if (pinData.m_uiElement) pinData.m_uiElement.gameObject.SetActive(false);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), "Update")]
        private class Update_Patch
        {
            static void Prefix(Minimap __instance)
            {
                if (isTPmode)
                {
                    if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
                    {
                        LeaveTPMode();
                        SetMapMode(1);
                    }
                    else if (ZInput.GetButtonDown("JoyButtonA"))
                    {
                        DoTP();
                    }
                }
            }
            /* static void Postfix(Minimap __instance)
             {
                 if (!list_populated)
                 {
                     //logger.LogWarning(" Minimap Update ResetPins ");
                     ResetPins();
                     list_populated = true;
                 }
             }*/
        }
        /*
        [HarmonyPatch(typeof(Game), "_RequestRespawn")]
        private class _RequestRespawn_Patch
        {
            static void Postfix(Game __instance)
            {
                if ((bool)GetInstanceField(Game.instance, "m_firstSpawn"))
                {
                    //logger.LogWarning(" _RequestRespawn ResetPins ");
                    ResetPins();
                    list_populated = false;
                }
            }
        }*/

        [HarmonyPatch(typeof(Minimap), "Start")]
        private class Start_Patch
        {
            static void Postfix(Minimap __instance)
            {
                //logger.LogWarning(" Minimap Start ");
                list_populated = false;
                tplist = null;
                tp_list = null;
                LoadImages();
                Minimap.SpriteData new_spritedata = new Minimap.SpriteData
                {
                    m_name = portal_pin_type,
                    m_icon = portal_icon
                };
                Minimap.instance.m_icons.Add(new_spritedata);
            }
        }

        [HarmonyPatch(typeof(Minimap), "UpdateLocationPins")]
        private class UpdateLocationPins_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var domagic = typeof(Def_handy_portals).GetMethod("Do_magic", BindingFlags.Public | BindingFlags.Static);
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Box)
                    {
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, domagic));
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), "Interact")]
        private class Interact_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (configPortalNameMaxLength.Value)
                {
                    for (int i = 0; i < codes.Count; i++)
                    {
                        if (codes[i].opcode == OpCodes.Ldc_I4_S)
                        {
                            codes[i].operand = 127;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(WearNTear), "Destroy")]
        public static class Destroy_Patch
        {
            static void Prefix(ZNetView ___m_nview)
            {
                if (___m_nview)
                {
                    if (___m_nview.GetPrefabName() == "portal_wood")
                    {
                        if (ZNet.instance.IsServer())
                        {
                            ZDO temp_zdo = ___m_nview.GetZDO();
                            tp_list.TryGetValue(temp_zdo, out Minimap.PinData temp_data);
                            Minimap.instance.RemovePin(temp_data);
                            tp_list.Remove(temp_zdo);
                            ZDOMan.instance.ForceSendZDO(temp_zdo.m_uid);
                        }
                        //else
                        //{
                        //    ZRoutedRpc.instance.InvokeRoutedRPC("RequestPortalZDOs", new object[] { new ZPackage() });
                        //}
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "OnPlaced")]
        public static class OnPlaced_Patch
        {
            static void Prefix(ZNetView ___m_nview)
            {
                if (___m_nview)
                {
                    if (___m_nview.GetPrefabName() == "portal_wood")
                    {
                        if (ZNet.instance.IsServer())
                        {
                            ZDO temp_zdo = ___m_nview.GetZDO();
                            Minimap.PinData temp_data = Minimap.instance.AddPin(___m_nview.transform.position, portal_pin_type, GetLocalizedString(configPortalNamePrefix.Value) + "unnamed", false, false);
                            tp_list.Add(temp_zdo, temp_data);
                            ZDOMan.instance.ForceSendZDO(temp_zdo.m_uid);
                        }
                        //else
                        //{
                        //    ZRoutedRpc.instance.InvokeRoutedRPC("RequestPortalZDOs", new object[] { new ZPackage() });
                        //}
                    }
                }
            }
        }

        private static object GetInstanceField<T>(T instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = typeof(T).GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        private static float GetMinimapLargeZoom()
        {
            return (float)GetInstanceField(Minimap.instance, "m_largeZoom");
        }
        public static ZDO GetClosestTP()
        {
            MethodInfo dynMethod = Minimap.instance.GetType().GetMethod("ScreenToWorldPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 pos = (Vector3)dynMethod.Invoke(Minimap.instance, new object[] { Input.mousePosition });
            Minimap.PinData pinData = null;
            ZDO zdo = null;
            float zoom = GetMinimapLargeZoom() * 2f * 128f * 2f;
            float num = 999999f;
            foreach (KeyValuePair<ZDO, Minimap.PinData> entry in tp_list)
            {
                float num2 = Utils.DistanceXZ(pos, entry.Value.m_pos);
                if (num2 < zoom && (num2 < num || pinData == null))
                {
                    pinData = entry.Value;
                    zdo = entry.Key;
                    num = num2;
                }
            }
            return zdo;
        }
        private static void DoTP()
        {
            ZDO zdo = GetClosestTP();
            if (zdo != null && Player.m_localPlayer.IsTeleportable())
            {
                Vector3 position = zdo.GetPosition();
                Quaternion rotation = zdo.GetRotation();
                Vector3 pos = position + rotation * Vector3.forward + Vector3.up;
                LeaveTPMode();
                SetMapMode(1);
                Player.m_localPlayer.TeleportTo(pos, rotation, true);
            }
        }

        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        private class OnMapLeftClick_Patch
        {
            static bool Prefix(Minimap __instance)
            {
                if (isTPmode)
                {
                    DoTP();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), "OnMapDblClick")]
        private class OnMapDblClick_Patch
        {
            static bool Prefix(Minimap __instance)
            {
                if (isTPmode)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), "OnMapRightClick")]
        private class OnMapRightClick_Patch
        {
            static bool Prefix(Minimap __instance, UIInputHandler handler)
            {
                if (isTPmode)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), "OnMapMiddleClick")]
        private class OnMapMiddleClick_Patch
        {
            static bool Prefix(Minimap __instance, UIInputHandler handler)
            {
                if (isTPmode)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), "UpdatePortal")]
        private class UpdatePortal_Patch
        {
            static bool Prefix(TeleportWorld __instance)
            {
                Player closestPlayer = Player.GetClosestPlayer(__instance.m_proximityRoot.position, __instance.m_activationRange);
                __instance.m_target_found.SetActive(closestPlayer && closestPlayer.IsTeleportable());
                return false;
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), "GetHoverText")]
        private class GetHoverText_Patch
        {
            static bool Prefix(TeleportWorld __instance, ref string __result)
            {
                string text = __instance.GetText();
                __result = Localization.instance.Localize(string.Concat(new string[] { configPortalNamePrefix.Value, text, "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag" }));
                return false;
            }
        }

        [HarmonyPatch(typeof(Humanoid), "IsTeleportable")]
        private class IsTeleportable_Patch
        {
            static void Postfix(ref bool __result)
            {
                __result = __result || configAllowTeleportWithOre.Value;
            }
        }

        [HarmonyPatch(typeof(ZDOMan), "RPC_ZDOData")]
        private class RPC_ZDOData_Patch
        {
            static void Postfix()
            {
                if (!ZNet.instance.IsServer())
                {
                    tplist = new List<ZDO>();
                    ZDOMan.instance.GetAllZDOsWithPrefab(portal_name, tplist);
                    //logger.LogWarning("list size " + tplist.Count);
                    //if (tp_list_size != tplist.Count)
                    {
                        //logger.LogWarning("tp_list_size changed: " + tplist.Count);
                        tp_list_size = tplist.Count;
                        ResetPins();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), "SetText")]
        private class SetText_Patch
        {
            static void Postfix(TeleportWorld __instance, ZNetView ___m_nview, string text)
            {
                if (___m_nview != null)
                {
                    tp_list.TryGetValue(___m_nview.GetZDO(), out Minimap.PinData temp_data);
                    if (temp_data != null)
                    {
                        temp_data.m_name = GetLocalizedString(configPortalNamePrefix.Value) + text;
                    }
                }
            }
        }

        static public void SetMapMode(int mode) // 1 small 2 large
        {
            MethodInfo dynMethod = Minimap.instance.GetType().GetMethod("SetMapMode", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(Minimap.instance, new object[] { mode });
        }

        [HarmonyPatch(typeof(TeleportWorldTrigger), "OnTriggerEnter")]
        private class OnTriggerEnter_Patch
        {
            static bool Prefix(Collider collider)
            {
                Player component = collider.GetComponent<Player>();
                if (component == null)
                {
                    return false;
                }
                if (Player.m_localPlayer != component)
                {
                    return false;
                }
                if (Player.m_localPlayer.IsTeleportable())
                {
                    EnterTPMode();
                    SetMapMode(2);
                    component.Message(MessageHud.MessageType.Center, "Select your destination", 0, null);
                }
                else
                {
                    component.Message(MessageHud.MessageType.Center, "$msg_noteleport", 0, null);
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ZDOMan), "CreateSyncList")]
        private class CreateSyncList_Patch
        {
            static void Prefix(List<ZDO> toSync)
            {
                if (ZNet.instance.IsServer())
                {
                    ZDOMan.instance.GetAllZDOsWithPrefab(portal_name, toSync);
                    if (tp_list_size != toSync.Count)
                    {
                        tp_list_size = toSync.Count;
                        logger.LogWarning("CreateSyncList_Patch: tp list size " + toSync.Count);
                    }
                }
            }
        }
    }
}
