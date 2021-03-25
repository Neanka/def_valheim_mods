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

namespace always_announce_biome_entering
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Always_announce_biome_entering : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.always_announce_biome_entering";
        const string pluginName = "always_announce_biome_entering";
        const string pluginVersion = "1.0.0.0";
        public static ManualLogSource logger;

        private static ConfigEntry<bool> configModEnabled;
        private static ConfigEntry<bool> configSoundEnabled;
        Heightmap.Biome saved_biome = (Heightmap.Biome)0;

        private Harmony _harmony;
        void Awake()
        {
            logger = Logger;
            logger.LogInfo("Hello, world!");
            configModEnabled = Config.Bind("Main", "Enable mod", true, "Enable mod");
            configSoundEnabled = Config.Bind("Main", "Enable discover sound", true, "Enable discover sound");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
        private void Update()
        {
            var player = Player.m_localPlayer;
            if (configModEnabled.Value && player != null)
            {
                Heightmap.Biome currentBiome = player.GetCurrentBiome();
                if (currentBiome != saved_biome)
                {
                    saved_biome = currentBiome;
                    if (saved_biome != Heightmap.Biome.None)
                    {
                        string text = Localization.instance.Localize("$biome_" + currentBiome.ToString().ToLower());
                        MessageHud.instance.ShowBiomeFoundMsg(text, configSoundEnabled.Value);
                    }
                }
            }
        }
    }
}
