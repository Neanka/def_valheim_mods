﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DEF_handy_ward
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Def_handy_ward : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.def_handy_ward";
        const string pluginName = "DEF handy wards";
        const string pluginVersion = "1.0.0.0";
        public static ManualLogSource logger;

        private static ConfigEntry<bool> configEnableAutoRepair;
        private static ConfigEntry<long> configRepairInterval;
        private static ConfigEntry<float> configRepairAmountPercent;

        private static ConfigEntry<bool> configEnableStaminaDrainReduction;
        private static ConfigEntry<float> configStaminaDrainReductionPercent;

        private static ConfigEntry<bool> configEnableFoodDrainReduction;
        private static ConfigEntry<float> configFoodDrainReductionPercent;

        private static ConfigEntry<float> configPrivateAreaRadius;
        private static ConfigEntry<float> configConstructionDamageReduction;

        public static long l_saved_time;

        private Harmony _harmony;

        private static object GetInstanceField<T>(T instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = typeof(T).GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        void Awake()
        {
            configEnableAutoRepair = Config.Bind("Autorepair", "Enable autorepair", true, "Enable autorepair structures in the ward area");
            configRepairInterval = Config.Bind("Autorepair", "Repair interval", 10L, "Repair interval in seconds");
            configRepairAmountPercent = Config.Bind("Autorepair", "Repair amount", 1f, new ConfigDescription("Repair amount in percents", new AcceptableValueRange<float>(0, 100)));

            configEnableStaminaDrainReduction = Config.Bind("Stamina drain", "Enable stamina drain reduction", true, "Enable stamina drain reduction in the ward area");
            configStaminaDrainReductionPercent = Config.Bind("Stamina drain", "Stamina drain reduction amount", 50f, new ConfigDescription("Stamina drain reduction amount", new AcceptableValueRange<float>(0, 100)));

            configEnableFoodDrainReduction = Config.Bind("Food drain", "Enable food drain reduction", true, "Enable food drain reduction in the ward area");
            configFoodDrainReductionPercent = Config.Bind("Food drain", "Food drain reduction amount", 50f, new ConfigDescription("Food drain reduction amount", new AcceptableValueRange<float>(0, 100)));

            configPrivateAreaRadius = Config.Bind("Tweaks", "Private area radius", 32f, new ConfigDescription("Set up ward privat area radius", new AcceptableValueRange<float>(0, 200)));
            configConstructionDamageReduction = Config.Bind("Tweaks", "Reduce construction incoming damage", 0f, new ConfigDescription("Reduce construction incoming damage", new AcceptableValueRange<float>(0, 100)));

            logger = Logger;
            logger.LogInfo("Hello, world!");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            l_saved_time = DateTime.Now.Ticks;
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
            if (configEnableAutoRepair.Value)
            {
                var cur_time = DateTime.Now.Ticks;
                if ((cur_time - l_saved_time) / TimeSpan.TicksPerSecond >= configRepairInterval.Value)
                {
                    l_saved_time = cur_time;
                    List<WearNTear> list = WearNTear.GetAllInstaces();
                    if (list.Count > 0)
                    {
                        foreach (WearNTear item in list)
                        {
                            ZNetView view = (ZNetView)GetInstanceField(item, "m_nview");
                            if (view != null)
                            {
                                if (PrivateArea.CheckInPrivateArea(item.transform.position))
                                {
                                    float health = view.GetZDO().GetFloat("health");
                                    if (health > 0 && health < item.m_health)
                                    {
                                        float res_health = health + item.m_health * configRepairAmountPercent.Value / 100;
                                        if (res_health > item.m_health) res_health = item.m_health;
                                        view.GetZDO().Set("health", res_health);
                                        view.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", new object[] { res_health });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Player), "RPC_UseStamina")]
        static class RPC_UseStamina_Patch
        {
            static void Prefix(long sender, ref float v)
            {
                if (configEnableStaminaDrainReduction.Value)
                {
                    if (PrivateArea.CheckInPrivateArea(Player.m_localPlayer.transform.position))
                    {
                        v -= v * configStaminaDrainReductionPercent.Value / 100;
                    }
                }
            }
        }
        public static float getFoodDrainMod()
        {
            if (configEnableFoodDrainReduction.Value)
            {
                if (PrivateArea.CheckInPrivateArea(Player.m_localPlayer.transform.position))
                {
                    return 1 - configFoodDrainReductionPercent.Value / 100;
                }
            }
            return 1f;
        }
        [HarmonyPatch(typeof(Player), "UpdateFood")]
        private class UpdateFood_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var getfooddrain = typeof(Def_handy_ward).GetMethod("getFoodDrainMod", BindingFlags.Public | BindingFlags.Static);
                var codes = new List<CodeInstruction>(instructions);
                var counter = 0;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ldfld && codes[i - 2].opcode == OpCodes.Ldfld)
                    {

                        counter++;
                        if (counter == 2 || counter == 4)
                        {
                            codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, getfooddrain));
                            codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(PrivateArea), "IsInside")]
        static class IsInside_Patch
        {
            static void Prefix(ref float ___m_radius)
            {
                ___m_radius = configPrivateAreaRadius.Value;
            }
        }
        [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
        static class RPC_Damage_Patch
        {
            static bool Prefix(WearNTear __instance, ref HitData hit)
            {
                ZNetView view = (ZNetView)GetInstanceField(__instance, "m_nview");
                if (view != null)
                {
                    if (PrivateArea.CheckInPrivateArea(__instance.transform.position))
                    {
                        hit.m_damage.m_blunt = hit.m_damage.m_blunt * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_slash = hit.m_damage.m_slash * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_pierce = hit.m_damage.m_pierce * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_chop = hit.m_damage.m_chop * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_pickaxe = hit.m_damage.m_pickaxe * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_fire = hit.m_damage.m_fire * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_frost = hit.m_damage.m_frost * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_lightning = hit.m_damage.m_lightning * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_poison = hit.m_damage.m_poison * (1 - configConstructionDamageReduction.Value / 100);
                        hit.m_damage.m_spirit = hit.m_damage.m_spirit * (1 - configConstructionDamageReduction.Value / 100);
                    }
                }
                return true;
            }
        }
        /*public static float getShieldScale()
        {
            logger.LogWarning("scale: "+ configPrivateAreaRadius.Value / 10f);
            return configPrivateAreaRadius.Value/10f;
        }
        [HarmonyPatch(typeof(PrivateArea), "RPC_FlashShield")]
        private class RPC_FlashShield_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var gss = typeof(Def_handy_ward).GetMethod("getShieldScale", BindingFlags.Public | BindingFlags.Static);
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4)
                    {
                        codes[i].opcode = OpCodes.Call;
                        codes[i].operand = gss;
                    }
                }
                return codes.AsEnumerable();
            }
        }*/
    }
}
