// ResurrectionButterflyBlue.cs
using Microsoft.Xna.Framework;
using System;
using TEACHER.Content.ALL_NPC.M5;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M5
{
    internal class ResurrectionButterflyBlue : ModNPC
    {
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement(
                    "从幽幽子大小姐的符卡「反魂蝶」中逃逸的一抹苍蓝残影。\r\n\t\t\t本应在无尽弹幕中消逝的磷光，却因半灵分身携带的浓郁冥界气息而滞留现世，成为了规则的漏洞。\r\n\t\t\t它的翅翼褪去了所有生者的色彩，只剩无名的死灰，唯有在靠近半灵时才会被染成幽冷的苍蓝——那是冥河的颜色。\r\n\t\t\t据说触碰过它的人，会短暂看见白玉楼庭院中永不凋谢的樱花——然后永远忘记自己为何流泪，也忘记自己是谁。")
            });
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3; // 竖排4帧飞行动画
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
            NPC.color = new Color(120, 180, 255);
        }

        public override void AI()
        {
            // 寻找半灵分身
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
                NPC.ai[0] += 0.025f;
                float orbitX = (float)Math.Cos(NPC.ai[0] + NPC.whoAmI * 0.7f) * 45f;
                float orbitY = (float)Math.Sin(NPC.ai[0] * 1.4f + NPC.whoAmI * 0.7f) * 25f;

                Vector2 goal = halfSpirit.Center + new Vector2(orbitX, orbitY);
                Vector2 dir = goal - NPC.Center;
                float dist = dir.Length();

                if (dist > 8f)
                {
                    dir.Normalize();
                    NPC.velocity = Vector2.Lerp(NPC.velocity, dir * 2.5f, 0.06f);
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

            // 苍蓝粒子
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(4, 4),
                    DustID.GemSapphire,
                    -NPC.velocity * 0.3f,
                    80,
                    new Color(120, 180, 255),
                    0.6f
                );
                d.noGravity = true;
            }
        }

        // ==================== 4帧飞行动画 ====================
        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5) // 每5帧切一次， flutter 感
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
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemSapphire,
                        hit.HitDirection * 2, -2f, 0, new Color(120, 180, 255), 0.9f);
                }
            }
        }
    }
}