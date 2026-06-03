using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M5
{
    internal class YoumuHalfSpiritSpawnSystem : ModSystem
    {
        private int _spawnTimer = 0;

        public override void PostUpdateWorld()
        {
            // 白天不生成
            if (Main.dayTime)
            {
                _spawnTimer = 0;
                return;
            }

            // 如果已存在该NPC，重置计时器
            if (NPC.AnyNPCs(ModContent.NPCType < YoumuHalfSpiritTrader > ()))
            {
                _spawnTimer = 0;
                return;
            }

            _spawnTimer++;
            int checkInterval = 1200 + Main.rand.Next(1200);
            if (_spawnTimer < checkInterval) return;
            _spawnTimer = 0;

            Player target = null;
            int count = 0;

            // 修改：只在夜间且身处墓地的活跃玩家中挑选目标
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead || !p.ZoneGraveyard) continue;
                count++;
                if (Main.rand.NextBool(count))
                    target = p;
            }

            if (target == null) return;

            Point tileCenter = target.Center.ToTileCoordinates();
            int spawnX = -1, spawnY = -1;

            // 在目标玩家（墓地内）附近寻找合适的地面生成点
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int tryX = tileCenter.X + Main.rand.Next(-35, 36);
                int tryY = tileCenter.Y + Main.rand.Next(-15, 16);
                tryX = Math.Clamp(tryX, 10, Main.maxTilesX - 10);
                tryY = Math.Clamp(tryY, 10, Main.maxTilesY - 10);

                int groundY = tryY;
                while (groundY < Main.maxTilesY - 10 && !WorldGen.SolidTile(tryX, groundY))
                    groundY++;

                if (groundY > 10 && !WorldGen.SolidTile(tryX, groundY - 1) && !WorldGen.SolidTile(tryX, groundY - 2))
                {
                    spawnX = tryX * 16 + 8;
                    spawnY = (groundY - 1) * 16;
                    break;
                }
            }

            if (spawnX < 0) return;

            var source = target.GetSource_GiftOrReward("HalfSpiritSpawn");
            int idx = NPC.NewNPC(source, spawnX, spawnY, ModContent.NPCType < YoumuHalfSpiritTrader > ());

            if (idx != Main.maxNPCs)
            {
                NPC npc = Main.npc[idx];
                npc.target = target.whoAmI;
                npc.direction = target.Center.X > npc.Center.X ? 1 : -1;
                npc.netUpdate = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    string[] msgs = new[]
                    {
                        "半灵分身悄无声息地飘了过来...",
                        "大小姐的采购员出现了！",
                        "你感觉到一股凉意——半灵分身在附近徘徊。",
                        "夜晚的空气中传来了半灵的低语..."
                    };
                    Main.NewText(msgs[Main.rand.Next(msgs.Length)], new Color(200, 255, 220));
                }
            }
        }
    }
}