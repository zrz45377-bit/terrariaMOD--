using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content
{
    public class DropGlobalNPC2 : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // ========== 1. 所有小动物 ==========
            // 覆盖：兔兔、松鼠、鸟、蝴蝶、萤火虫、青蛙、蜗牛、蠕虫、金鱼、企鹅、鸭子、老鼠、蝎子、松露虫、七彩草蛉、地狱小动物、金小动物、宝石小动物 等
            if (NPCID.Sets.CountsAsCritter[npc.type])
            {
                npcLoot.Add(ItemDropRule.Common(ItemID.Ectoplasm, 4)); // 25% 掉落灵气
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 2)); // 25% 掉落灵气
            }

            // ========== 2. 所有友方/城镇NPC ==========
            if (IsFriendlyNPC(npc))
            {
                npcLoot.Add(ItemDropRule.Common(ItemID.Ectoplasm, 4)); // 25% 掉落灵气
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1)); // 25% 掉落灵气
            }
        }

        private bool IsFriendlyNPC(NPC npc)
        {
            if (npc.townNPC) return true;
            if (npc.type == NPCID.TravellingMerchant) return true;
            if (npc.type == NPCID.OldMan) return true;
            if (npc.type == NPCID.SkeletonMerchant) return true;
            if (npc.type == NPCID.TownCat || npc.type == NPCID.TownDog || npc.type == NPCID.TownBunny) return true;
            return false;
        }
    }

    // ========== 世纪之花：额外掉落锯刃镐 ==========
    // 设计理念：提前让玩家获得神庙级挖掘工具，但数量严格控制
    public class PlanteraPicksawDrop : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.type != NPCID.Plantera) return;

            // 必定掉落：1 把锯刃镐（固定额外奖励）
            Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Picksaw, 1);

            // 1/8 概率额外再掉一个神庙钥匙（方便多世界玩家）
            if (Main.rand.NextBool(8))
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.TempleKey);
            }

            base.OnKill(npc);
        }
    }

    // ========== 世纪之花触手：仆从微量材料掉落 ==========
    // 设计理念：世纪之花的触手被击破时，有概率泄出少量丛林能量
    public class PlanteraTentacleLoot : GlobalNPC
    {
        // 仆从奖励池（加权）
        private static readonly (short itemId, double weight)[] TentacleDrops = new[]
        {
            (ItemID.ChlorophyteOre, 4.0),      // 叶绿矿（最常见）
            (ItemID.ChlorophyteBar, 1.0),      // 叶绿锭（稀有）
            (ItemID.JungleSpores, 2.0),        // 丛林孢子
        };

        public override void OnKill(NPC npc)
        {
            // 仅对世纪之花触手生效
            if (npc.type != NPCID.PlanterasTentacle) return;

            // 仆从怪只有 25% 概率触发掉落（避免刷起来太慷慨）
            if (!Main.rand.NextBool(4))
            {
                base.OnKill(npc);
                return;
            }

            // 加权随机抽取
            double totalWeight = 0;
            foreach (var drop in TentacleDrops) totalWeight += drop.weight;

            double roll = Main.rand.NextDouble() * totalWeight;
            foreach (var drop in TentacleDrops)
            {
                roll -= drop.weight;
                if (roll <= 0)
                {
                    // 叶绿矿掉 2~4 个，其他材料掉 1~2 个
                    int stack = (drop.itemId == ItemID.ChlorophyteOre) ? Main.rand.Next(2, 5) : Main.rand.Next(1, 3);
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), drop.itemId, stack);
                    break;
                }
            }

            base.OnKill(npc);
        }
    }
}