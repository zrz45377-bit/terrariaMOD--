// ResurrectionButterflyPurple.cs
using Microsoft.Xna.Framework;
using System;
using TEACHER.Content.ALL_NPC.M5;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M5
{
    internal class ResurrectionButterflyPurple : ModNPC
    {
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement(
                    "从幽幽子大小姐的符卡「反魂蝶」中逃逸的一缕紫幻幽火。\r\n\t\t\t与苍蓝蝶成双成对，环绕着半灵分身翩翩起舞，仿佛在为下一个迷失的亡魂铺设黄泉路。\r\n\t\t\t灰色的翅翼本身并无颜色，直到它开始贪婪地吸食半灵溢出的灵力，才泛起妖艳而致命的紫芒。\r\n\t\t\t白玉楼的仆从们深知，紫蝶出现的地方，意味着大小姐的夜宵正在路上——或者，有人即将成为那道夜宵。")
            });
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3; // 竖排4帧
        }

        public override void SetDefaults()
        {
            NPC.width = 16;
            NPC.height = 16;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.friendly = true;
            NPC.knockBackResist = 0f;
            NPC.alpha = 80;
            NPC.color = new Color(220, 100, 255);
        }

        public override void AI()
        {
            NPC halfSpirit = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                if (npc.type != ModContent.NPCType < YoumuHalfSpiritTrader > ()) continue;

                float dist = NPC.Distance(npc.Center);
                if (dist < minDist)
                {
                    minDist = dist;
                    halfSpirit = npc;
                }
            }

            if (halfSpirit != null)
            {
                NPC.ai[0] += 0.03f;
                float orbitX = (float)Math.Cos(NPC.ai[0] + NPC.whoAmI * 0.9f + MathHelper.Pi) * 40f;
                float orbitY = (float)Math.Sin(NPC.ai[0] * 1.2f + NPC.whoAmI * 0.9f) * 30f;

                Vector2 goal = halfSpirit.Center + new Vector2(orbitX, orbitY);
                Vector2 dir = goal - NPC.Center;
                float dist = dir.Length();

                if (dist > 8f)
                {
                    dir.Normalize();
                    NPC.velocity = Vector2.Lerp(NPC.velocity, dir * 2.2f, 0.05f);
                }
                else
                {
                    NPC.velocity *= 0.92f;
                }

                NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
                NPC.rotation = NPC.velocity.X * 0.08f;
            }
            else
            {
                NPC.alpha += 3;
                NPC.velocity.Y -= 0.04f;
                NPC.velocity *= 0.95f;
                if (NPC.alpha >= 255) NPC.active = false;
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(4, 4),
                    DustID.GemAmethyst,
                    -NPC.velocity * 0.3f,
                    80,
                    new Color(220, 100, 255),
                    0.6f
                );
                d.noGravity = true;
            }
        }

        // ==================== 4帧飞行动画 ====================
        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type])
                    NPC.frame.Y = 0;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemAmethyst,
                        hit.HitDirection * 2, -2f, 0, new Color(220, 100, 255), 0.9f);
                }
            }
        }
    }
}