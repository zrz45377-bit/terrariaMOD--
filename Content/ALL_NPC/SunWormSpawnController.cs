using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TEACHER.Content.ALL_NPC.M4;   // RemiliaBat
using TEACHER.Content.ALL_NPC.M7;  // FlandreEliteBat
using TEACHER.Content.ITEM_S.N13;  // SunWormPlayer

namespace TEACHER.Content.ALL_NPC
{
    public class SunWormSpawnController : GlobalNPC
    {
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            // 玩家没戴太阳虫头盔 → 什么都不做
            if (!spawnInfo.Player.GetModPlayer<SunWormPlayer>().sunWormHelmet)
                return;

            // 戴了 → 把这两个怪从当前生成池里抹掉
            int remilia = ModContent.NPCType < RemiliaBat > ();
            int flandre = ModContent.NPCType<FlandreEliteBat>();

            if (pool.ContainsKey(remilia))
                pool[remilia] = 0f;

            if (pool.ContainsKey(flandre))
                pool[flandre] = 0f;
        }
    }

}