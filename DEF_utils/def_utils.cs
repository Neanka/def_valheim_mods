using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
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
                foreach (Player.Food food in Player.m_localPlayer.GetFoods())
                {
                    logger.LogWarning("trace_values " + food.m_health + " " + food.m_stamina); 
                }
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
    }
}
