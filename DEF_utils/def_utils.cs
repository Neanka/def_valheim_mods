using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DEF_utils
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Def_utils : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.def_utils";
        const string pluginName = "DEF utils";
        const string pluginVersion = "1.0.0.0";
        public static ManualLogSource logger;

        private Harmony _harmony;

        private static object GetInstanceField<T>(T instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = typeof(T).GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
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

            //

            if (Input.GetKeyDown(KeyCode.F11))
            {
                logger.LogInfo("debug button pressed");
                logger.LogWarning(player.transform.position);
                
                logger.LogInfo("pvp status: "+ player.IsPVPEnabled());


                /*
                logger.LogWarning("object db se " + ObjectDB.instance.m_StatusEffects.Count);
                foreach (StatusEffect item in ObjectDB.instance.m_StatusEffects)
                {
                    logger.LogWarning("name "+ item.m_name+ " icon " + item.m_icon);
                    player.GetSEMan().AddStatusEffect(item);

                }*/
            }
        }
        [HarmonyPatch(typeof(FejdStartup), "OnJoinIPOpen")]
        static class FejdStartup_OnJoinIPOpen_Patch
        {
            static void Postfix(FejdStartup __instance)
            {
                string text = "192.168.31.143";
                __instance.m_joinIPAddress.text = text;
            }
        }
        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        static class RPC_ClientHandshake_Patch
        {
            static void Postfix(RectTransform ___m_passwordDialog)
            {
                string text = "secret";
                InputField componentInChildren = ___m_passwordDialog.GetComponentInChildren<InputField>();
                componentInChildren.text = text;
            }
        }
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        static class Awake_Patch
        {
            static void Postfix()
            {
                logger.LogWarning("object db se " + ObjectDB.instance.m_StatusEffects.Count);
            }
        }
    }
}
