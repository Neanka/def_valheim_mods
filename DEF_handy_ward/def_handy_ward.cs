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

namespace DEF_handy_ward
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Def_handy_ward : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.def_handy_ward";
        const string pluginName = "DEF handy wards";
        const string pluginVersion = "1.0.0.0";
        public static ManualLogSource logger;

        private static ConfigEntry<long> configRepairInterval;
        private static ConfigEntry<float> configRepairAmountPercent;

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
            configRepairInterval = Config.Bind("Main", "Repair interval", 10L, "Repair interval in seconds");
            configRepairAmountPercent = Config.Bind("Main", "Repair amount", 1f, "Repair amount in percents");

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
            var cur_time = DateTime.Now.Ticks;
            if ((cur_time - l_saved_time) / TimeSpan.TicksPerSecond >= configRepairInterval.Value)
            {
                l_saved_time = cur_time;
                //Logger.LogInfo(l_saved_time);
                List<WearNTear> list = WearNTear.GetAllInstaces();
                //Logger.LogInfo("size: " + list.Count);
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
                                //Logger.LogInfo("item in private area. health: " + health + "/" + item.m_health + " " + item);
                            }
                        }
                    }
                }

                //WearNTear.m_allInstances;// (List<WearNTear>)GetInstanceField(WearNTear, "m_allInstances");

            }
        }
    }
}
