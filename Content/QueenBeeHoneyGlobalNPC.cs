using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content
{
    public class QueenBeeHoneyGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 仅蜂后生效，其他 Boss / NPC 不受影响
            if (npc.type == NPCID.QueenBee)
            {
                // 将世界坐标转换为 Tile 坐标
                int centerX = (int)((npc.position.X + npc.width / 2f) / 16f);
                int centerY = (int)((npc.position.Y + npc.height / 2f) / 16f);

                // 在死亡位置生成 4×4 的蜂蜜池（蜂后体积大，比史莱姆女王稍宽）
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        int tileX = centerX + x;
                        int tileY = centerY + y;

                        // 放置满格 (255) 蜂蜜液体
                        WorldGen.PlaceLiquid(tileX, tileY, (byte)LiquidID.Honey, 255);
                    }
                }

                // 如果在服务器上运行，把液体变动同步给所有客户端
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendTileSquare(-1, centerX - 2, centerY - 2, 5);
                }

                // 蜂蜜色粒子爆开（可选，增加视觉效果）
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        npc.Center,
                        DustID.Honey,
                        Main.rand.NextVector2Circular(4f, 4f),
                        0,
                        new Color(255, 180, 0),
                        1.5f
                    );
                    d.noGravity = true;
                }

                // 额外掉落几个蜂蜜瓶（可选）
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.BottledHoney, Main.rand.Next(3, 8));
            }

            base.OnKill(npc);
        }
    }
}