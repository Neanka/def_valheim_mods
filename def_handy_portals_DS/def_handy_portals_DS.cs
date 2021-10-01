using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace def_handy_portals_DS
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Def_handy_portals_DS : BaseUnityPlugin
    {
        const string pluginGUID = "neanka.def_handy_portals_DS";
        const string pluginName = "DEF handy portals DS";
        const string pluginVersion = "1.0.0.7";

        private Harmony _harmony;

        public static readonly string portal_name = "portal_wood";
        //private static int portal_name_hash;
        private static int tp_list_size = 0;
        public static ManualLogSource logger;

        void Awake()
        {
            logger = Logger;
            logger.LogInfo("Hello, world!");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            //portal_name_hash = StringExtensionMethods.GetStableHashCode(portal_name);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
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

        [HarmonyPatch(typeof(ZDOMan), "CreateSyncList")]
        private class CreateSyncList_Patch
        {
            static void Prefix(List<ZDO> toSync)
            {
                //ZLog.LogWarning("CreateSyncList_Patch");
                ZDOMan.instance.GetAllZDOsWithPrefab(portal_name, toSync);
                if (tp_list_size != toSync.Count)
                {
                    tp_list_size = toSync.Count;
                    logger.LogWarning("CreateSyncList_Patch: tp list size " + toSync.Count);
                }
            }
        }

        /*
        [HarmonyPatch(typeof(ZDOPool), "Release", new Type[] { typeof(ZDO) })]
        public static class Release_Patch
        {
            static void Prefix(ZDO zdo)
            {
                if (zdo.IsValid() && zdo.GetPrefab() == portal_name_hash)
                {
                   // logger.LogWarning("portal_wood destroyed");
                }
            }
        }

        [HarmonyPatch(typeof(ZDOMan), "AddToSector")]
        public static class AddToSector_Patch
        {
            static void Postfix(ZDO zdo)
            {
                //if (zdo.IsValid() && zdo.GetPrefab() == portal_name_hash)
                //{
                 //   logger.LogWarning("portal_wood AddToSector " + zdo.GetPrefab());
                //}
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "RPC_SpawnObject")]
        public static class RPC_SpawnObject_Patch
        {
            static void Postfix(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
            {
                if (prefabHash == portal_name_hash)
                {
                     logger.LogWarning("portal_wood created");
                }
            }
        }
        [HarmonyPatch(typeof(ZNetScene), "Destroy", new Type[] { typeof(GameObject) })]
        public static class Destroy_Patch
        {
            static void Prefix(GameObject go)
            {
                ZNetView component = go.GetComponent<ZNetView>();
                if (component && component.GetZDO() != null)
                {
                    ZDO zdo = component.GetZDO();
                    if (zdo.IsValid() && zdo.GetPrefab() == portal_name_hash)
                    {
                        logger.LogWarning("portal_wood destroyed");
                    }
                }
            }
        }*/
    }
}
