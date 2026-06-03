using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TEACHER.Content
{
    // ========== 邪教领主：额外掉落夜明锭 ==========
    // 设计理念：提前让玩家接触月球级材料，但数量严格控制
    public class CultistLunarBarDrop : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.type != NPCID.CultistBoss) return;

            // 必定掉落：2~4 个夜明锭（固定额外奖励）
            int barCount = Main.rand.Next(30, 50);
            Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.LunarBar, barCount);

            // 1/12 概率额外再掉一个远古操纵机（方便多基地玩家）
            if (Main.rand.NextBool(12))
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.LunarCraftingStation);
            }

            base.OnKill(npc);
        }
    }

    // ========== 远古幻影妖：仆从微量材料掉落 ==========
    // 设计理念：邪教领主召唤的幻影妖被击破时，有概率泄出少量月球能量
    public class AncientVisionLoot : GlobalNPC
    {
        // 仆从奖励池（加权）
        private static readonly (short itemId, double weight)[] VisionDrops = new[]
        {
            (ItemID.LunarOre, 4.0),       // 夜明矿（最常见）
            (ItemID.LunarBar, 1.0),       // 夜明锭（稀有）
            (ItemID.FallenStar, 2.0),     // 坠落之星
        };

        public override void OnKill(NPC npc)
        {
            // 仅对远古幻影妖生效（邪教领主召唤的眼睛状仆从）
            if (npc.type != NPCID.AncientCultistSquidhead) return;

            // 仆从怪只有 25% 概率触发掉落（避免刷起来太慷慨）
            if (!Main.rand.NextBool(2))
            {
                base.OnKill(npc);
                return;
            }

            // 加权随机抽取
            double totalWeight = 0;
            foreach (var drop in VisionDrops) totalWeight += drop.weight;

            double roll = Main.rand.NextDouble() * totalWeight;
            foreach (var drop in VisionDrops)
            {
                roll -= drop.weight;
                if (roll <= 0)
                {
                    // 夜明矿掉 2~4 个，其他材料掉 1 个
                    int stack = (drop.itemId == ItemID.LunarOre) ? Main.rand.Next(2, 5) : 1;
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), drop.itemId, stack);
                    break;
                }
            }

            base.OnKill(npc);
        }
    }
}