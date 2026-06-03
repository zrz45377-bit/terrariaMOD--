using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M3
{
    public class MineralSlimeM3 : ModNPC
    {
        // ========== 全局 AI 模式 ==========
        private int aiMode = -1;

        // ========== 彩虹色相循环（0~1）==========
        private float hueCycle;

        // ========== 图鉴 ==========
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement($"Mods.TEACHER.NPCs.MineralSlimeM3.Bestiary")
            });
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 44;

            NPC.damage = 32;
            NPC.defense = 6;
            NPC.lifeMax = 140;

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.value = 350f;
            NPC.knockBackResist = 0.55f;

            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (aiMode == -1)
                aiMode = Main.rand.Next(4);
        }

        // ========== 彩虹着色：让灰度贴图显示七彩颜色 ==========
        // 返回值直接与贴图相乘，忽略环境光变暗，实现"自发光"感
        public override Color? GetAlpha(Color lightColor)
        {
            return Main.hslToRgb(hueCycle, 1f, 0.75f);
        }

        // ========== 外发光：PostDraw 中用 Additive 叠加多层光晕 ==========
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Vector2 drawPos = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
            Rectangle frame = NPC.frame;
            Vector2 origin = frame.Size() / 2f;
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // 临时切换为 Additive 混合，绘制朦胧光晕
            BlendState oldBlend = spriteBatch.GraphicsDevice.BlendState;
            spriteBatch.GraphicsDevice.BlendState = BlendState.Additive;

            for (int i = 4; i >= 1; i--)
            {
                float scale = NPC.scale * (1f + i * 0.07f);
                // 每层色相略微偏移，产生虹彩边缘
                float h = (hueCycle + i * 0.04f) % 1f;
                Color glow = Main.hslToRgb(h, 1f, 0.8f) * (0.18f / i);

                spriteBatch.Draw(texture, drawPos, frame, glow, NPC.rotation, origin, scale, effects, 0f);
            }

            // 恢复混合状态
            spriteBatch.GraphicsDevice.BlendState = oldBlend;
        }

        // ========== 受击时爆出彩虹粒子 ==========
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0) return;

            for (int i = 0; i < 10; i++)
            {
                float h = (hueCycle + i * 0.1f) % 1f;
                Color c = Main.hslToRgb(h, 1f, 0.7f);
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.RainbowTorch,
                    Main.rand.NextVector2Circular(3f, 3f), 0, c, 1.2f);
                d.noGravity = true;
            }
        }

        // ========== 主 AI 分发器 ==========
        public override void AI()
        {
            if (aiMode == -1) aiMode = Main.rand.Next(4);

            // 每帧推进色相（约 6.5 秒完成一次全色谱循环）
            hueCycle += 0.015f;
            if (hueCycle >= 1f) hueCycle -= 1f;

            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];
            NPC.spriteDirection = (player.Center.X > NPC.Center.X) ? 1 : -1;

            // 所有模式通用的彩虹拖尾粒子
            if (Main.rand.NextBool(4))
            {
                float h = (hueCycle + Main.rand.NextFloat(-0.1f, 0.1f)) % 1f;
                Color c = Main.hslToRgb(h, 1f, 0.7f);
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2.5f, NPC.height / 2.5f),
                    DustID.RainbowTorch,
                    NPC.velocity * 0.15f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    0, c, 0.9f);
                d.noGravity = true;
                d.fadeIn = 0.5f;
            }

            switch (aiMode)
            {
                case 0: AI_Mode0_Float(player); break;
                case 1: AI_Mode1_Throw(player); break;
                case 2: AI_Mode2_Summon(player); break;
                case 3: AI_Mode3_Charge(player); break;
                default: AI_Mode0_Float(player); break;
            }
        }

        // ========== 模式 0：基础漂浮 ==========
        private void AI_Mode0_Float(Player player)
        {
            NPC.ai[0]++;
            NPC.ai[1] += 0.06f;

            Vector2 toPlayer = player.Center - NPC.Center;
            toPlayer.Normalize();
            NPC.velocity += toPlayer * 0.035f;
            NPC.velocity.Y += (float)Math.Sin(NPC.ai[1]) * 0.12f;
            NPC.velocity.X += (float)Math.Cos(NPC.ai[1] * 0.5f) * 0.03f;
            NPC.velocity *= 0.96f;

            if (NPC.collideX) NPC.velocity.X *= -0.75f;
            if (NPC.collideY) NPC.velocity.Y *= -0.75f;
        }

        // ========== 模式 1：投掷石头 ==========
        private void AI_Mode1_Throw(Player player)
        {
            NPC.ai[1] += 0.05f;
            NPC.velocity.Y += (float)Math.Sin(NPC.ai[1]) * 0.08f;

            float dist = NPC.Distance(player.Center);
            Vector2 toPlayer = player.Center - NPC.Center;
            toPlayer.Normalize();

            if (dist < 180f) NPC.velocity -= toPlayer * 0.14f;
            else if (dist > 320f) NPC.velocity += toPlayer * 0.09f;
            NPC.velocity *= 0.94f;

            NPC.ai[0]++;
            if (NPC.ai[0] >= 100f && dist < 400f
                && Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1))
            {
                NPC.ai[0] = 0f;
                Vector2 rockVel = toPlayer * 7.5f;
                rockVel.Y -= 3f;

                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, rockVel,
                    ProjectileID.RockGolemRock, NPC.damage / 2, 1.5f, Main.myPlayer);

                // 投掷时爆出对应色相的粒子
                for (int i = 0; i < 6; i++)
                {
                    float h = (hueCycle + i * 0.15f) % 1f;
                    Dust.NewDust(NPC.Center, 8, 8, DustID.RainbowTorch,
                        rockVel.X * 0.2f + Main.rand.NextFloat(-1f, 1f),
                        rockVel.Y * 0.2f + Main.rand.NextFloat(-1f, 1f),
                        0, Main.hslToRgb(h, 1f, 0.8f), 1.1f);
                }
            }

            if (NPC.collideX) NPC.velocity.X *= -0.7f;
            if (NPC.collideY) NPC.velocity.Y *= -0.7f;
        }

        // ========== 模式 2：召唤史莱姆 ==========
        private void AI_Mode2_Summon(Player player)
        {
            NPC.ai[1] += 0.06f;
            NPC.velocity += new Vector2(
                (float)Math.Cos(NPC.ai[1]) * 0.04f,
                (float)Math.Sin(NPC.ai[1] * 0.7f) * 0.06f);
            NPC.velocity *= 0.95f;

            float dist = NPC.Distance(player.Center);
            Vector2 toPlayer = player.Center - NPC.Center;
            toPlayer.Normalize();
            if (dist > 350f) NPC.velocity += toPlayer * 0.05f;
            else if (dist < 150f) NPC.velocity -= toPlayer * 0.06f;

            NPC.ai[0]++;
            if (NPC.ai[0] >= 160f && dist < 450f)
            {
                NPC.ai[0] = 0f;
                int[] slimes = new int[]
                {
                    NPCID.BlueSlime, NPCID.GreenSlime, NPCID.PurpleSlime,
                    NPCID.RedSlime, NPCID.YellowSlime, NPCID.BlackSlime
                };
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)(NPC.Center.Y + 30f),
                    slimes[Main.rand.Next(slimes.Length)]);

                // 召唤时彩虹凝胶爆
                for (int i = 0; i < 12; i++)
                {
                    float h = (hueCycle + i * 0.08f) % 1f;
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RainbowTorch,
                        Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-2f, 4f),
                        0, Main.hslToRgb(h, 1f, 0.8f), 1.2f);
                }
            }

            if (NPC.collideX) NPC.velocity.X *= -0.7f;
            if (NPC.collideY) NPC.velocity.Y *= -0.7f;
        }

        // ========== 模式 3：冲撞 ==========
        private void AI_Mode3_Charge(Player player)
        {
            NPC.ai[0]++;
            float dist = NPC.Distance(player.Center);

            if (NPC.ai[1] == 0f) // 蓄力
            {
                Vector2 toPlayer = player.Center - NPC.Center;
                toPlayer.Normalize();
                NPC.velocity += toPlayer * 0.04f;
                NPC.velocity *= 0.88f;

                if (NPC.ai[0] >= 90f && dist < 350f)
                {
                    NPC.ai[0] = 0f;
                    NPC.ai[1] = 1f;
                    Vector2 chargeDir = player.Center - NPC.Center;
                    chargeDir.Normalize();
                    NPC.velocity = chargeDir * 9.5f;

                    // 冲锋时七彩尾迹
                    for (int i = 0; i < 12; i++)
                    {
                        float h = (hueCycle + i * 0.08f) % 1f;
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.RainbowTorch,
                            -chargeDir * Main.rand.NextFloat(2f, 5f) + Main.rand.NextVector2Circular(1f, 1f),
                            0, Main.hslToRgb(h, 1f, 0.8f), 1.4f);
                        d.noGravity = true;
                    }
                }
            }
            else if (NPC.ai[1] == 1f) // 冲锋
            {
                if (NPC.ai[0] >= 36f || NPC.collideX || NPC.collideY)
                {
                    NPC.ai[0] = 0f;
                    NPC.ai[1] = 2f;
                    NPC.velocity *= 0.25f;
                }
            }
            else if (NPC.ai[1] == 2f) // 冷却
            {
                NPC.velocity *= 0.85f;
                if (NPC.ai[0] >= 70f)
                {
                    NPC.ai[0] = 0f;
                    NPC.ai[1] = 0f;
                }
            }
        }

        // ========== 4 帧史莱姆动画 ==========
        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter > 8)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                    NPC.frame.Y = 0;
            }
        }

        // ========== 死亡掉落（与之前相同） ==========
        public override void OnKill()
        {
            int itemId = GetMineralDrop();
            int amount = Main.rand.Next(3, 7);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemId, amount);

            if (Main.rand.NextBool(10))
            {
                int[] gems = new int[]
                {
                    ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire,
                    ItemID.Emerald, ItemID.Ruby, ItemID.Diamond, ItemID.Amber
                };
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                    gems[Main.rand.Next(gems.Length)], 1);
            }

            // 死亡时超大彩虹爆
            for (int i = 0; i < 24; i++)
            {
                float h = (hueCycle + i * 0.04f) % 1f;
                Color c = Main.hslToRgb(h, 1f, 0.8f);
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.RainbowTorch,
                    Main.rand.NextVector2Circular(6f, 6f), 0, c, 1.6f);
                d.noGravity = true;
                d.fadeIn = 0.8f;
            }
        }

        private int GetMineralDrop()
        {
            var pool = new List<int>
            {
                ItemID.CopperOre, ItemID.TinOre,
                ItemID.IronOre, ItemID.LeadOre,
                ItemID.SilverOre, ItemID.TungstenOre,
                ItemID.GoldOre, ItemID.PlatinumOre,
                ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire,
                ItemID.Emerald, ItemID.Ruby, ItemID.Diamond, ItemID.Amber,
            };

            if (NPC.downedBoss1)
            {
                pool.AddRange(new int[] { ItemID.DemoniteOre, ItemID.CrimtaneOre });
            }
            if (NPC.downedBoss2)
            {
                pool.Add(ItemID.Hellstone);
            }
            if (Main.hardMode)
            {
                pool.AddRange(new int[]
                {
                    ItemID.CobaltOre, ItemID.PalladiumOre,
                    ItemID.MythrilOre, ItemID.OrichalcumOre,
                    ItemID.AdamantiteOre, ItemID.TitaniumOre,
                });
            }
            if (NPC.downedPlantBoss)
            {
                pool.Add(ItemID.ChlorophyteOre);
            }
            if (NPC.downedGolemBoss)
            {
                pool.Add(ItemID.LunarOre);
            }

            return pool[Main.rand.Next(pool.Count)];
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // 只在地下岩石层，排除地牢和地狱
            if (!spawnInfo.Player.ZoneRockLayerHeight) return 0f;
            if (spawnInfo.Player.ZoneDungeon || spawnInfo.Player.ZoneUnderworldHeight) return 0f;

            // 基础权重：对标洞穴蝙蝠（0.5f）
            float chance = 0.15f;

            // 困难模式后矿物活性化，权重提升（对标巨型蝙蝠 0.8f）
            if (Main.hardMode)
                chance = 0.18f;

            // 血月时地下魔力暴走，权重再翻倍（1.2f，此时比蝙蝠还常见）
            if (Main.bloodMoon)
                chance += 0.14f;

            // 夜晚（地下依然算夜晚）微增
            if (!Main.dayTime)
                chance += 0.11f;

            return chance;
        }
    }
}