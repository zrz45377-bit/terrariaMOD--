using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TEACHER.Content
{
    public class DukeFishronAnglerLoot : GlobalNPC
    {
        // 普通单件奖励池（按原版稀有度加权）
        private static readonly (short itemId, double weight)[] BaseRewards = new[]
        {
            // 功能性配饰（较常见）
            (ItemID.HighTestFishingLine, 4.0),
            (ItemID.AnglerEarring, 4.0),
            (ItemID.TackleBox, 4.0),
            (ItemID.LavaproofTackleBag, 4.0),   // 防熔岩钓钩

            // 信息配饰
            (ItemID.FishermansGuide, 3.33),
            (ItemID.WeatherRadio, 3.33),
            (ItemID.Sextant, 3.33),

            // 其他
            (ItemID.FishingBobber, 4.0),
            (ItemID.FishHook, 1.67),              // 鱼钩
            (ItemID.FishMinecart, 1.67),          // 鲤鱼矿车
            (ItemID.SuperAbsorbantSponge, 1.43),  // 超级吸收绵
            (ItemID.BottomlessBucket, 1.43),      // 无底水桶

            // 稀有
            (ItemID.GoldenBugNet, 1.25),
            (ItemID.FuzzyCarrot, 1.0),            // 绒毛胡萝卜
            (ItemID.GoldenFishingRod, 0.4),       // 金钓竿
            (ItemID.HotlineFishingHook, 1.0),     // 热线钓竿（表格里的"熔线钓钩"）
            (ItemID.FinWings, 1.43),              // 鳍翼
            (ItemID.HoneyAbsorbantSponge, 2.5),   // 蜂蜜吸收绵
            (ItemID.BottomlessHoneyBucket, 2.5),  // 无底蜂蜜桶
        };


        public override void OnKill(NPC npc)
        {
            if (npc.type != NPCID.DukeFishron) return;

            // 组装本次的掉落池
            var pool = new List<(short itemId, double weight)>(BaseRewards);
            // ===== 套装类（原版备注："一次性奖励全部 3 件"）=====
            
            // 1/80 概率直接掉渔夫全套（帽子+背心+裤子）
            if (Main.rand.NextBool(80))
            {
                DropMultiple(npc, ItemID.AnglerHat, ItemID.AnglerVest, ItemID.AnglerPants);
                return;
            }

            // 1/80 概率掉鱼装全套
            if (Main.rand.NextBool(80))
            {
                DropMultiple(npc, ItemID.FishCostumeMask, ItemID.FishCostumeShirt, ItemID.FishCostumeFinskirt);
                return;
            }


            // ===== 普通单件奖励（加权随机） =====
            double totalWeight = 0;
            foreach (var drop in pool) totalWeight += drop.weight;

            double roll = Main.rand.NextDouble() * totalWeight;
            foreach (var drop in pool)
            {
                roll -= drop.weight;
                if (roll <= 0)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), drop.itemId);
                    break;
                }
            }

            base.OnKill(npc);
        }

        // 辅助：在同一位置掉落多件物品（用于套装）
        private void DropMultiple(NPC npc, params int[] items)
        {
            var source = npc.GetSource_Loot();
            Vector2 pos = npc.Center;
            foreach (int id in items)
                Item.NewItem(source, pos, id);
        }
    }
}