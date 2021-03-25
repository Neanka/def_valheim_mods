using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace toggle_minimap
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class toggle_minimap : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.toggle_minimap";
        const string pluginName = "toggle minimap";
        const string pluginVersion = "1.0.0.0";
        public static ManualLogSource logger;
        private static ConfigEntry<KeyboardShortcut> configMagicKey;
        private static bool minimap_disabled = false;
        private static object GetInstanceField<T>(T instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = typeof(T).GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        private Harmony _harmony;
        void Awake()
        {
            logger = Logger;
            logger.LogInfo("Hello, world!");
            configMagicKey = Config.Bind("Toggle", "Toggle minimap", new KeyboardShortcut(KeyCode.F8), "Toggle minimap");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
        private void Update()
        {
            var player = Player.m_localPlayer;
            if (configMagicKey.Value.IsDown())
            {
                minimap_disabled = !minimap_disabled;
                int m_mode = (int)GetInstanceField(Minimap.instance, "m_mode");
                if (m_mode == 1)
                {
                    Minimap.instance.m_smallRoot.SetActive(!minimap_disabled);
                }
            }
        }
        [HarmonyPatch(typeof(Minimap), "SetMapMode")]
        private class SetMapMode_Patch
        {
            static void Postfix(int mode)
            {
                if (mode == 1 && minimap_disabled)
                {
                    Minimap.instance.m_smallRoot.SetActive(false);
                }
            }
        }
    }
}
