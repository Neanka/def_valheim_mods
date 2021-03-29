using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DEF_More_minimap_icons
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Def_more_minimap_icons : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.def_more_minimap_icons";
        const string pluginName = "DEF more minimap icons";
        const string pluginVersion = "1.0.0.0";
        public static ManualLogSource logger;

        private Harmony _harmony;
        void Awake()
        {
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
            var player = Player.m_localPlayer;
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Logger.LogInfo("debug button pressed"); Transform largemap = Minimap.instance.m_selectedIcon0.transform.parent.parent.parent;
                Component[] objs = largemap.gameObject.GetComponents(typeof(Component));
                foreach (var item in objs)
                {
                    logger.LogWarning(item);
                }
                for (int i = 0; i < largemap.childCount; i++)
                {
                    logger.LogWarning(largemap.GetChild(i));
                }
                RectTransform iconpanel = (RectTransform)largemap.GetChild(2);
                iconpanel.sizeDelta = new Vector2(500, 500);
            }
        }
    }
}
