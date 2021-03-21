using System.Collections.Generic;

namespace def_handy_portals
{
    public class RPC
    {
        public static void RequestPortalZDOs(long sender, ZPackage pkg)
        {
            //Def_handy_portals.logger.LogWarning("RequestPortalZDOs");
            //ZPackage newPkg = new ZPackage();
            List<ZDO> tplist = new List<ZDO>();
            ZDOMan.instance.GetAllZDOsWithPrefab(Def_handy_portals.portal_name, tplist);
            if (tplist.Count != 0)
            {
                //Def_handy_portals.logger.LogWarning("tplist.Count: " + tplist.Count);
                //newPkg.Write(tplist.Count);
                foreach (ZDO item in tplist)
                {
                    //    newPkg.Write(item.m_uid);
                    //Def_handy_portals.logger.LogWarning("ZDOID: " + item.m_uid);
                    ZDOMan.instance.ForceSendZDO(item.m_uid);
                }
                //ZRoutedRpc.instance.InvokeRoutedRPC(sender, "ProcessZDOs", new object[] { newPkg });
            }
        }
        public static void ProcessZDOs(long sender, ZPackage pkg)
        {
            //Def_handy_portals.logger.LogWarning("ProcessZDOs pkg.size:" + pkg.Size());
            if (pkg != null && pkg.Size() > 0)
            {
                int count = pkg.ReadInt();
                //Def_handy_portals.logger.LogWarning("zdos count: " + count);
                while (count > 0)
                {
                    ZDOID zdoid = pkg.ReadZDOID();
                    //Def_handy_portals.logger.LogWarning("zdoid: " + zdoid + "prefab: ");
                    count--;
                }
            }
        }
    }
}
