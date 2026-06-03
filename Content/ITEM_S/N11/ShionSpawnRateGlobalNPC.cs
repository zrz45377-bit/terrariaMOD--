using TEACHER.Content.ITEM_S.N11;
using Terraria;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N11
{
    public class ShionSpawnRateGlobalNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            // 只有玩家身上有紫苑 Buff 时才生效
            if (player.HasBuff(ModContent.BuffType < ShionYorigamiBuff > ()))
            {
                spawnRate = (int)(spawnRate / 10.1f);   // 间隔 ÷10 = 10倍刷怪
                maxSpawns = (int)(maxSpawns * 10.1f);   // 上限 ×10
            }
        }
    }
}