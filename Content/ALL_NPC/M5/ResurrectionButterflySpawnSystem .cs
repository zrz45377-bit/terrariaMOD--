// ResurrectionButterflySpawnSystem.cs（生成系统，无改动）
using Microsoft.Xna.Framework;
using TEACHER.Content.ALL_NPC.M5;
using Terraria;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M5
{
    internal class ResurrectionButterflySpawnSystem : ModSystem
    {
        public override void PostUpdateWorld()
        {
            if (Main.dayTime) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC spirit = Main.npc[i];
                if (!spirit.active || spirit.type != ModContent.NPCType < YoumuHalfSpiritTrader > ()) continue;

                int blueCount = 0;
                int purpleCount = 0;
                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    NPC other = Main.npc[j];
                    if (!other.active) continue;
                    if (other.Distance(spirit.Center) > 150f) continue;

                    if (other.type == ModContent.NPCType < ResurrectionButterflyBlue > ()) blueCount++;
                    if (other.type == ModContent.NPCType < ResurrectionButterflyPurple > ()) purpleCount++;
                }

                var source = spirit.GetSource_FromAI();

                if (blueCount < 3 && Main.rand.NextBool(80))
                {
                    Vector2 spawnPos = spirit.Center + Main.rand.NextVector2Circular(35, 25);
                    NPC.NewNPC(source, (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType < ResurrectionButterflyBlue > ());
                }
                if (purpleCount < 3 && Main.rand.NextBool(80))
                {
                    Vector2 spawnPos = spirit.Center + Main.rand.NextVector2Circular(35, 25);
                    NPC.NewNPC(source, (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType < ResurrectionButterflyPurple > ());
                }
            }
        }
    }
}