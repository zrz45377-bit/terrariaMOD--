using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content
{
    public class DungeonChestWeaponLoot : GlobalNPC
    {
        // 地牢生物群落箱武器掉落池（等概率）
        private static readonly short[] DungeonChestWeapons = new short[]
        {
            ItemID.VampireKnives,         // 吸血鬼刀：副手/射手召唤通用，依然好用
            ItemID.RainbowGun,            // 彩虹枪：随缘有用的辅助，偶尔有奇效
            ItemID.ScourgeoftheCorruptor, // 腐化者之戟：实战效果不错，特定条件更优
            ItemID.StormTigerStaff,      // 沙漠虎杖：帅，叠召唤后很强
            ItemID.StaffoftheFrostHydra,  // 冰冻九头蛇法杖：聊胜于无的哨兵辅助
            ItemID.PiranhaGun             // 食人鱼枪：输出同期一般，手感较怪
        };

        public override void OnKill(NPC npc)
        {
            // 仅处理圣骑士和地牢史莱姆
            if (npc.type != NPCID.Paladin && npc.type != NPCID.DungeonSlime)
            {
                base.OnKill(npc);
                return;
            }

            // 概率判定：圣骑士 10% (1/10)，地牢史莱姆 1% (1/100)
            bool shouldDrop = npc.type == NPCID.Paladin
                ? Main.rand.NextBool(10)
                : Main.rand.NextBool(100);

            if (!shouldDrop)
            {
                base.OnKill(npc);
                return;
            }

            // 从武器池中均匀随机选择一种掉落
            short itemId = DungeonChestWeapons[Main.rand.Next(DungeonChestWeapons.Length)];

            // 生成掉落物品
            Item.NewItem(npc.GetSource_Loot(), npc.getRect(), itemId);

            base.OnKill(npc);
        }
    }
}