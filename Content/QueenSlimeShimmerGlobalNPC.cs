using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content
{
    public class QueenSlimeShimmerGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 仅史莱姆女王生效，其他 Boss / NPC 不受影响
            if (npc.type == NPCID.QueenSlimeBoss)
            {
                // ========== 新增：掉落 1-3 个明胶水晶 ==========
                int crystalCount = Main.rand.Next(1, 4); // 1~3 个（上限4是排他的）
                Item.NewItem(
                    npc.GetSource_Loot(),   // 掉落来源
                    npc.getRect(),          // 在Boss掉落范围内生成
                    ItemID.QueenSlimeCrystal,  // 明胶水晶（若编译报错请改为 ItemID.QueenSlimeCrystal）
                    crystalCount
                );
                // ================================================

                // 将世界坐标转换为 Tile 坐标
                int centerX = (int)((npc.position.X + npc.width / 2f) / 16f);
                int centerY = (int)((npc.position.Y + npc.height / 2f) / 16f);

                // 在死亡位置生成 3×3 的微光池（可自行改大小）
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int tileX = centerX + x;
                        int tileY = centerY + y;

                        // 放置满格 (255) 微光液体
                        WorldGen.PlaceLiquid(tileX, tileY, (byte)LiquidID.Shimmer, 255);
                    }
                }

                // 如果在服务器上运行，把液体变动同步给所有客户端
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendTileSquare(-1, centerX - 1, centerY - 1, 3);
                }
            }

            base.OnKill(npc);
        }
    }
}