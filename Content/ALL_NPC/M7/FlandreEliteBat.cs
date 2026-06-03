using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M7
{
    public class FlandreEliteBat : ModNPC
    {
        private const int FrameHeight = 36;
        private const int FrameCount = 4;
        private const float TownNPCSearchRange = 12000f; // 检索范围：75格左右

        // ai[0] : 主攻击循环计时（玩家阶段）
        // ai[1] : 瞬移/弹幕计时（玩家阶段）
        // ai[2] : 召唤/大招计时（Phase3）
        // ai[3] : 狂暴阶段切换标记（0/1）
        // localAI[0] : 0=屠城阶段（先杀NPC），1=猎杀玩家阶段
        // localAI[1] : 屠城攻击冷却

        private enum ElitePhase
        {
            Phase1_Chaser,
            Phase2_Teleport,
            Phase3_Berserk
        }

        private ElitePhase CurrentPhase
        {
            get
            {
                float hpPercent = (float)NPC.life / NPC.lifeMax;
                if (hpPercent <= 0.30f) return ElitePhase.Phase3_Berserk;
                if (hpPercent <= 0.70f) return ElitePhase.Phase2_Teleport;
                return ElitePhase.Phase1_Chaser;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = FrameHeight;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.knockBackResist = 0.05f;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Venom] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.Frostburn] = true;
            NPC.buffImmune[BuffID.Confused] = true;

            // ========== 核心：生成时强制进入屠城模式 ==========
            NPC.localAI[0] = 0f;
            NPC.localAI[1] = 0f;

            ApplyEliteStats();
        }

        private void ApplyEliteStats()
        {
            NPC.lifeMax = 6000;
            NPC.damage = 25;
            NPC.defense = 25;
            NPC.value = 30000f;


            NPC.life = NPC.lifeMax;
        }

        public override void AI()
        {
            // ========== 绝对优先：只要还在屠城阶段，完全无视玩家 ==========
            if (NPC.localAI[0] == 0f)
            {
                HuntTownNPCs();
                Animate();
                return;
            }

            // ========== 以下只有屠城结束后才会执行 ==========
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.velocity.Y += 0.15f;
                NPC.velocity.X *= 0.95f;
                if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                Animate();
                return;
            }

            SpawnEliteAura();

            // 阶段切换提示
            if (NPC.ai[3] == 0f && CurrentPhase == ElitePhase.Phase3_Berserk)
            {
                NPC.ai[3] = 1f;
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                            Main.rand.NextVector2Circular(6f, 6f), 0, Color.Gold, 1.5f);
                        d.noGravity = true;
                    }
                    CombatText.NewText(NPC.getRect(), Color.Gold, "狂暴化！");
                }
            }

            if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
            {
                NPC.velocity.X *= -0.9f;
                NPC.velocity.Y *= -0.7f;
            }

            switch (CurrentPhase)
            {
                case ElitePhase.Phase1_Chaser: Phase1AI(player); break;
                case ElitePhase.Phase2_Teleport: Phase2AI(player); break;
                case ElitePhase.Phase3_Berserk: Phase3AI(player); break;
            }

            EliteChasePlayer(player);
            Animate();
            NPC.spriteDirection = NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
        }

        // ========== 屠城阶段：唯一目标是城镇NPC，玩家不存在 ==========
        private void HuntTownNPCs()
        {
            int targetNPC = -1;
            float minDist = TownNPCSearchRange; // 1200f

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].townNPC && Main.npc[i].life > 0)
                {
                    float dist = NPC.Distance(Main.npc[i].Center);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        targetNPC = i;
                    }
                }
            }

            // 范围内没有存活城镇NPC → 屠城完成，切换为玩家狩猎
            if (targetNPC == -1)
            {
                NPC.localAI[0] = 1f;
                NPC.localAI[1] = 0f;
                NPC.TargetClosest(true);

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(NPC.getRect(), Color.Gold, "NPC 已全灭…轮到你了！");
                    for (int i = 0; i < 30; i++)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                            Main.rand.NextVector2Circular(5f, 5f), 0, Color.Gold, 1.5f);
                        d.noGravity = true;
                    }
                }
                return;
            }

            // 高速飞向城镇NPC
            Vector2 toNPC = Main.npc[targetNPC].Center - NPC.Center;
            toNPC.Normalize();
            float speed = 9.5f;
            float accel = 0.32f;

            NPC.velocity.X += toNPC.X * accel;
            NPC.velocity.Y += toNPC.Y * accel;
            if (NPC.velocity.Length() > speed)
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * speed;

            NPC.spriteDirection = NPC.direction = Main.npc[targetNPC].Center.X > NPC.Center.X ? 1 : -1;

            // 攻击冷却递减
            if (NPC.localAI[1] > 0f) NPC.localAI[1]--;

            // 近距离撕咬
            if (minDist < 55f && NPC.localAI[1] <= 0f)
            {
                NPC.localAI[1] = 40f;
                int hitDir = Main.npc[targetNPC].Center.X > NPC.Center.X ? 1 : -1;

                NPC.HitInfo hitInfo = Main.npc[targetNPC].CalculateHitInfo(
                    damage: NPC.damage * 2,
                    hitDirection: hitDir,
                    crit: false,
                    knockBack: 5f
                );
                Main.npc[targetNPC].StrikeNPC(hitInfo);

                if (Main.netMode == NetmodeID.Server)
                    Main.npc[targetNPC].netUpdate = true;

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(NPC.getRect(), Color.Red, "撕裂！");
                    for (int i = 0; i < 15; i++)
                    {
                        Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                            Main.rand.NextVector2Circular(4f, 4f), 0, Color.Crimson, 1.2f);
                    }
                }

                // 吸血回复
                int heal = NPC.damage / 3;
                if (heal > 0 && NPC.life < NPC.lifeMax)
                {
                    NPC.life = Math.Min(NPC.life + heal, NPC.lifeMax);
                    if (Main.netMode != NetmodeID.Server)
                        CombatText.NewText(NPC.getRect(), Color.Gold, $"+{heal}");
                }
            }
        }

        private void EliteChasePlayer(Player player)
        {
            Vector2 toPlayer = player.Center - NPC.Center;
            float dist = toPlayer.Length();
            toPlayer.Normalize();

            float speed = CurrentPhase switch
            {
                ElitePhase.Phase3_Berserk => 14f,
                ElitePhase.Phase2_Teleport => 10f,
                _ => 7.5f
            };
            float accel = CurrentPhase switch
            {
                ElitePhase.Phase3_Berserk => 0.45f,
                ElitePhase.Phase2_Teleport => 0.28f,
                _ => 0.20f
            };
            float dashSpeed = CurrentPhase switch
            {
                ElitePhase.Phase3_Berserk => 18f,
                ElitePhase.Phase2_Teleport => 13f,
                _ => 10f
            };
            float dashDist = CurrentPhase switch
            {
                ElitePhase.Phase3_Berserk => 400f,
                ElitePhase.Phase2_Teleport => 320f,
                _ => 250f
            };

            float targetSpeed = dist < dashDist ? dashSpeed : speed;
            float currentAccel = dist < dashDist ? accel * 1.8f : accel;

            NPC.velocity.X += toPlayer.X * currentAccel;
            NPC.velocity.Y += toPlayer.Y * currentAccel;

            if (NPC.velocity.Length() > targetSpeed)
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * targetSpeed;
        }

        private void Phase1AI(Player player)
        {
            NPC.ai[0]++;
            if (NPC.ai[0] >= 120f && NPC.Distance(player.Center) < 500f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                Vector2 baseDir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 dir = baseDir.RotatedBy(MathHelper.ToRadians(18 * i));
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * 9f,
                        ProjectileID.Fireball, NPC.damage / 2, 2f, Main.myPlayer);
                }
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDustPerfect(NPC.Center, DustID.Torch,
                        Main.rand.NextVector2Circular(3f, 3f), 0, Color.OrangeRed, 1.2f);
                }
            }
        }

        private void Phase2AI(Player player)
        {
            NPC.ai[0]++;
            NPC.ai[1]++;

            if (NPC.ai[0] >= 180f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                Vector2 offset = new Vector2(
                    Main.rand.NextBool() ? Main.rand.Next(120, 200) : Main.rand.Next(-200, -120),
                    Main.rand.Next(-160, -60)
                );
                NPC.Center = player.Center + offset;
                NPC.netUpdate = true;
                for (int i = 0; i < 25; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                        Main.rand.NextVector2Circular(5f, 5f), 0, Color.Gold, 1.3f);
                    d.noGravity = true;
                }
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                        Main.rand.NextVector2Circular(4f, 4f), 0, Color.Crimson, 1.1f);
                }
            }

            if (NPC.ai[1] >= 90f && NPC.Distance(player.Center) < 450f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1] = 0f;
                Vector2 baseDir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 dir = baseDir.RotatedBy(MathHelper.ToRadians(12 * i));
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * 11f,
                        ProjectileID.CrystalBullet, NPC.damage / 2, 1.5f, Main.myPlayer);
                }
            }
        }

        private void Phase3AI(Player player)
        {
            NPC.ai[0]++;
            NPC.ai[1]++;
            NPC.ai[2]++;

            if (NPC.ai[0] >= 100f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                Vector2 target = player.Center + new Vector2(
                    Main.rand.Next(-150, 150),
                    Main.rand.Next(-140, -40)
                );
                NPC.Center = target;
                NPC.netUpdate = true;
                for (int i = 0; i < 30; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                        Main.rand.NextVector2Circular(6f, 6f), 0, Color.Gold, 1.6f);
                    d.noGravity = true;
                }
            }

            if (NPC.ai[1] >= 70f && NPC.Distance(player.Center) < 550f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1] = 0f;
                Vector2 baseDir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                for (int round = 0; round < 3; round++)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 dir = baseDir.RotatedBy(MathHelper.ToRadians(10 * i + Main.rand.Next(-5, 6)));
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * (12f + round * 2f),
                            ProjectileID.CrystalBullet, NPC.damage / 2, 1f, Main.myPlayer);
                    }
                }
            }

            if (NPC.ai[2] >= 360f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[2] = 0f;
                for (int i = 0; i < 3; i++)
                {
                    int x = (int)(NPC.Center.X + Main.rand.Next(-80, 80));
                    int y = (int)(NPC.Center.Y + Main.rand.Next(-80, 80));
                    int bat = NPC.NewNPC(NPC.GetSource_FromAI(), x, y, NPCID.CaveBat);
                    if (Main.npc[bat].active)
                    {
                        Main.npc[bat].target = NPC.target;
                        Main.npc[bat].velocity = (player.Center - Main.npc[bat].Center).SafeNormalize(Vector2.UnitY) * 6f;
                    }
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                        Main.rand.NextVector2Circular(5f, 5f), 0, Color.DarkRed, 1.4f);
                }
            }

            if (NPC.Distance(player.Center) < 90f && Main.rand.NextBool(15) && !player.dead)
            {
                int steal = NPC.damage / 6;
                NPC.life = Math.Min(NPC.life + steal, NPC.lifeMax);
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(NPC.getRect(), Color.Gold, $"+{steal}");
                    player.Hurt(PlayerDeathReason.LegacyDefault(), NPC.damage / 20, NPC.direction);
                }
            }
        }

        private void Animate()
        {
            NPC.frameCounter++;
            int frameSpeed = CurrentPhase == ElitePhase.Phase3_Berserk ? 3 : 5;
            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += FrameHeight;
                if (NPC.frame.Y >= FrameHeight * FrameCount)
                    NPC.frame.Y = 0;
            }
        }

        private void SpawnEliteAura()
        {
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(22f, 18f),
                    DustID.GoldCoin,
                    NPC.velocity * 0.15f,
                    0,
                    Color.Gold,
                    0.8f
                );
                d.noGravity = true;
            }
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(18f, 14f),
                    DustID.Torch,
                    NPC.velocity * 0.1f,
                    0,
                    Color.OrangeRed,
                    0.7f
                );
                d.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            int heal = hurtInfo.Damage / 4;
            if (heal > 0 && NPC.life < NPC.lifeMax)
            {
                NPC.life = Math.Min(NPC.life + heal, NPC.lifeMax);
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.Server)
                    CombatText.NewText(NPC.getRect(), Color.Gold, $"+{heal}");
            }

            target.AddBuff(BuffID.Bleeding, 300);
            target.AddBuff(BuffID.OnFire, 180);
            if (CurrentPhase >= ElitePhase.Phase2_Teleport)
                target.AddBuff(BuffID.CursedInferno, 120);
            if (CurrentPhase == ElitePhase.Phase3_Berserk)
                target.AddBuff(BuffID.Weak, 180);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                        Main.rand.NextVector2Circular(6f, 6f), 0, Color.Gold, 1.5f);
                    d.noGravity = true;
                }
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDustPerfect(NPC.Center, DustID.Torch,
                        Main.rand.NextVector2Circular(5f, 5f), 0, Color.OrangeRed, 1.3f);
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
            }
        }

        public override void OnKill()
        {
            for (int i = 0; i < 50; i++)
            {
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                    Main.rand.NextVector2Circular(6f, 6f), 0, Color.Gold, 1.5f);
                d.noGravity = true;
            }

            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.ChristmasPudding, Main.rand.Next(2, 6));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.GoldCoin, Main.rand.Next(8, 21));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.SilverCoin, Main.rand.Next(50, 100));

            int[] elitePool = new int[]
            {
                ItemID.CrystalShard,
                ItemID.SoulofNight,
                ItemID.SoulofSight,
                ItemID.RedPotion,
                ItemID.LifeFruit,
                ItemID.BloodMoonStarter
            };

            if (CurrentPhase == ElitePhase.Phase3_Berserk || NPC.downedMoonlord)
            {
                Array.Resize(ref elitePool, elitePool.Length + 3);
                elitePool[elitePool.Length - 3] = ItemID.PlatinumCoin;
                elitePool[elitePool.Length - 2] = ItemID.SoulofMight;
                elitePool[elitePool.Length - 1] = ItemID.SoulofFright;
            }

            int itemToDrop = elitePool[Main.rand.Next(elitePool.Length)];
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, Main.rand.Next(1, 4));

            if (Main.rand.NextBool(4))
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.LifeCrystal, 1);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("血月之下现身的破坏之翼。她会先将目之所及的一切城镇吞噬殆尽，再向玩家露出獠牙。据说她最爱的甜点是布丁。")
            });
        }

        // ========== 生成条件：仅血月 / 南瓜月 ==========
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // 非血月且非南瓜月期间，生成概率强制归零——芙兰只在这两种月夜现身
            if (!Main.bloodMoon && !Main.pumpkinMoon) return 0f;

            float chance = 0f;

            // 玩家位于地表时，生成概率最高（0.8%）
            if (spawnInfo.Player.ZoneOverworldHeight)
                chance = 0.008f;
            // 玩家位于地下岩石层时，生成概率降低（0.3%）
            else if (spawnInfo.Player.ZoneRockLayerHeight)
                chance = 0.003f;
            // 其他层（如洞穴层、地狱层等）不生成

            return chance;
        }
    }
}