using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TEACHER.Content.ALL_NPC.M8
{
    public class DownedBossSystem : ModSystem
    {
        public static bool downedDespairEye;

        public override void ClearWorld()
        {
            downedDespairEye = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["downedDespairEye"] = downedDespairEye;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            downedDespairEye = tag.GetBool("downedDespairEye");
        }
    }
}
