using System.Collections.Generic;

namespace def_handy_portals_DS
{
    public class RPC
    {
        public static void RequestPortalZDOs(long sender, ZPackage pkg)
        {
            //Def_handy_portals_DS.logger.LogWarning("RequestPortalZDOs");
            //ZPackage newPkg = new ZPackage();
            List<ZDO> tplist = new List<ZDO>();
            ZDOMan.instance.GetAllZDOsWithPrefab(Def_handy_portals_DS.portal_name, tplist);
            if (tplist.Count != 0)
            {
                //Def_handy_portals_DS.logger.LogWarning("tplist.Count: " + tplist.Count);
                //newPkg.Write(tplist.Count);
                foreach (ZDO item in tplist)
                {
                    //    newPkg.Write(item.m_uid);
                    //Def_handy_portals_DS.logger.LogWarning("ZDOID: " + item.m_uid);
                    ZDOMan.instance.ForceSendZDO(item.m_uid);
                }
            }
        }

        public static void ProcessZDOs(long sender, ZPackage pkg)
        {
        }
    }
}
