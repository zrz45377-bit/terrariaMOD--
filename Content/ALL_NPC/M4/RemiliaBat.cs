using Microsoft.Xna.Framework;
using System;
using TEACHER.Content.ITEM_S.N10;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M4
{
    public class RemiliaBat : ModNPC
    {
        private enum Stage
        {
            PreHardmode,
            Hardmode,
            PostPlantera,
            PostMoonlord
        }

        private Stage CurrentStage
        {
            get
            {
                if (NPC.downedMoonlord) return Stage.PostMoonlord;
                if (NPC.downedPlantBoss) return Stage.PostPlantera;
                if (Main.hardMode) return Stage.Hardmode;
                return Stage.PreHardmode;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 36;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.knockBackResist = 0.25f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Venom] = true;

            ApplyStageStats();
        }

        private void ApplyStageStats()
        {
            int THE_lifemax = 200;
            int The_damage = 5;
            NPC.defense = 15;
            int[] add_number = { 2,8,32 };
            int[] add_number_damage = { 2, 4, 8 };
            switch (CurrentStage)
            {
                case Stage.PostMoonlord:
                    NPC.lifeMax = THE_lifemax * add_number[2];
                    NPC.damage = The_damage * add_number_damage[2];
                    NPC.value = 5000f;
                    break;
                case Stage.PostPlantera:
                    NPC.lifeMax = THE_lifemax * add_number[1];
                    NPC.damage = The_damage * add_number_damage[1];
                    NPC.value = 2000f;
                    break;
                case Stage.Hardmode:
                    NPC.lifeMax = THE_lifemax * add_number[0];
                    NPC.damage = The_damage * add_number_damage[0];
                    NPC.value = 500f;
                    break;
                default:
                    NPC.lifeMax = THE_lifemax;
                    NPC.damage = The_damage;
                    NPC.value = 100f;
                    break;
            }
            NPC.life = NPC.lifeMax;
        }

        public override void AI()
        {
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

            if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
            {
                NPC.velocity.X *= -0.7f;
                NPC.velocity.Y *= -0.5f;
            }

            switch (CurrentStage)
            {
                case Stage.PreHardmode: PreHardmodeAI(player); break;
                case Stage.Hardmode: HardmodeAI(player); break;
                case Stage.PostPlantera: PostPlanteraAI(player); break;
                case Stage.PostMoonlord: PostMoonlordAI(player); break;
            }

            ChasePlayer(player);
            Animate();

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(20f, 15f),
                    DustID.Blood, NPC.velocity * 0.2f, 0, Color.Crimson, 0.9f);
                d.noGravity = true;
            }

            NPC.spriteDirection = NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
        }

        private void ChasePlayer(Player player)
        {
            Vector2 toPlayer = player.Center - NPC.Center;
            float dist = toPlayer.Length();
            toPlayer.Normalize();

            float speed = CurrentStage switch
            {
                Stage.PostMoonlord => 10f,
                Stage.PostPlantera => 7f,
                Stage.Hardmode => 5.5f,
                _ => 4f
            };
            float accel = CurrentStage switch
            {
                Stage.PostMoonlord => 0.35f,
                Stage.PostPlantera => 0.22f,
                Stage.Hardmode => 0.18f,
                _ => 0.12f
            };
            float dashSpeed = CurrentStage switch
            {
                Stage.PostMoonlord => 14f,
                Stage.PostPlantera => 10f,
                Stage.Hardmode => 9f,
                _ => 7f
            };
            float dashDist = CurrentStage switch
            {
                Stage.PostMoonlord => 350f,
                Stage.PostPlantera => 300f,
                Stage.Hardmode => 250f,
                _ => 200f
            };

            float targetSpeed = dist < dashDist ? dashSpeed : speed;
            float currentAccel = dist < dashDist ? accel * 1.5f : accel;

            NPC.velocity.X += toPlayer.X * currentAccel;
            NPC.velocity.Y += toPlayer.Y * currentAccel;

            if (NPC.velocity.Length() > targetSpeed)
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * targetSpeed;
        }

        private void PreHardmodeAI(Player player)
        {
            // 肉前：纯冲撞+吸血，无额外弹幕
        }

        private void HardmodeAI(Player player)
        {
            NPC.ai[0]++;
            if (NPC.ai[0] >= 180f && NPC.Distance(player.Center) < 450f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * 8f,
                    ProjectileID.BloodNautilusShot, NPC.damage / 2, 1f, Main.myPlayer);
            }
        }

        private void PostPlanteraAI(Player player)
        {
            NPC.ai[0]++;
            NPC.ai[1]++;

            if (NPC.ai[0] >= 240f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                Vector2 target = player.Center + new Vector2(Main.rand.Next(-180, 180), Main.rand.Next(-120, -40));
                NPC.Center = target;
                NPC.netUpdate = true;
                for (int i = 0; i < 20; i++)
                    Dust.NewDustPerfect(NPC.Center, DustID.Blood, Main.rand.NextVector2Circular(4f, 4f), 0, Color.Red, 1.2f);
            }

            if (NPC.ai[1] >= 120f && NPC.Distance(player.Center) < 400f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1] = 0f;
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.ToRadians(14 * i));
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * 10f,
                        ProjectileID.BloodNautilusShot, NPC.damage / 2, 1f, Main.myPlayer);
                }
            }
        }

        private void PostMoonlordAI(Player player)
        {
            NPC.ai[0]++;
            NPC.ai[1]++;
            NPC.ai[2]++;

            if (NPC.ai[0] >= 150f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                NPC.Center = player.Center + new Vector2(Main.rand.Next(-200, 200), Main.rand.Next(-150, -50));
                NPC.netUpdate = true;
                for (int i = 0; i < 25; i++)
                    Dust.NewDustPerfect(NPC.Center, DustID.Blood, Main.rand.NextVector2Circular(5f, 5f), 0, Color.Crimson, 1.5f);
            }

            if (NPC.ai[1] >= 90f && NPC.Distance(player.Center) < 500f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1] = 0f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY)
                        .RotatedBy(MathHelper.ToRadians(Main.rand.Next(-12, 12)));
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * 12f,
                        ProjectileID.BloodNautilusShot, NPC.damage / 2, 1f, Main.myPlayer);
                }
            }

            if (NPC.ai[2] >= 480f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[2] = 0f;
                for (int i = 0; i < 2; i++)
                {
                    int x = (int)(NPC.Center.X + Main.rand.Next(-60, 60));
                    int y = (int)(NPC.Center.Y + Main.rand.Next(-60, 60));
                    NPC.NewNPC(NPC.GetSource_FromAI(), x, y, NPCID.CaveBat);
                }
            }

            if (NPC.Distance(player.Center) < 100f && Main.rand.NextBool(20) && !player.dead)
            {
                int steal = NPC.damage / 8;
                NPC.life = Math.Min(NPC.life + steal, NPC.lifeMax);
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(NPC.getRect(), Color.Red, $"+{steal}");
                    player.Hurt(PlayerDeathReason.LegacyDefault(), NPC.damage / 25, NPC.direction);
                }
            }
        }

        private void Animate()
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += 36;
                if (NPC.frame.Y >= 36 * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            int heal = hurtInfo.Damage / 5;
            if (heal > 0 && NPC.life < NPC.lifeMax)
            {
                NPC.life = Math.Min(NPC.life + heal, NPC.lifeMax);
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.Server)
                    CombatText.NewText(NPC.getRect(), Color.Red, $"+{heal}");
            }

            if (CurrentStage >= Stage.Hardmode) target.AddBuff(BuffID.Bleeding, 180);
            if (CurrentStage >= Stage.PostPlantera) target.AddBuff(BuffID.Weak, 180);
            if (CurrentStage >= Stage.PostMoonlord) target.AddBuff(BuffID.CursedInferno, 120);
        }

        // ========== 掉落 ==========
        public override void OnKill()
        {
            // ========== 死亡视觉特效 ==========
            // 蝙蝠死亡时爆开 30 个血雾粒子，营造猩红氛围
            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                    Main.rand.NextVector2Circular(4f, 4f), 0, Color.Crimson, 1.3f);
                d.noGravity = true;
            }

            // ========== 基础掉落 ==========
            // 无论什么阶段，杀蝙蝠必掉 20~50 银币（夜行生物的积蓄）
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.SilverCoin, Main.rand.Next(20, 51));

            // ========== 阶段递进式核心掉落 ==========
            // 设计理念：世界进度越靠后，掉的东西越好。
            // 每个阶段用 NPC.downedXXX 判断，直接读原版字段，1.4.4.9 绝对稳。

            // ── 阶段 11：月球领主后（真·毕业池）──
            if (NPC.downedMoonlord)
            {
                // 神枪·冈格尼尔：1% 概率（1/100），因为这武器太厉害了，不能烂大街
                if (Main.rand.NextBool(100))
                {
                    // 直接掉 MOD 自定义物品：神枪·冈格尼尔（左键戳刺 + 右键投掷）
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ModContent.ItemType<GungnirSpear>(), 1);
                }
                else
                {
                    // 99% 概率：从其他毕业武器池随机抽一件， consolation prize
                    int[] pool = new int[]
                    {
                        ItemID.BloodOrange,
                        ItemID.BloodWater,
                        ItemID.BloodMoonStarter,
                        ItemID.RedPotion,           // 红药水
                        ItemID.SoulofNight,         // 暗夜之魂
                        ItemID.BloodMoonStarter
                    };
                    int itemToDrop = pool[Main.rand.Next(pool.Length)];
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
                }

                // 月后额外打赏：5~15 金币（红魔馆主不差钱）
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.GoldCoin, Main.rand.Next(5, 16));

                return; // 月后掉完直接结束，不走下面任何阶段
            }

            // ── 阶段 10：光之女皇后 ──
            else if (NPC.downedEmpressOfLight)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate,
                    ItemID.SoulofNight,         // 暗夜之魂
                    ItemID.CrimsonKeyMold,
                    ItemID.FleshBlock
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 9：猪龙鱼公爵后 ──
            else if (NPC.downedFishron)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate,
                    ItemID.SoulofNight,         // 暗夜之魂
                    ItemID.CrimsonKeyMold,
                    ItemID.FleshBlock
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 8：石巨人后 ──
            else if (NPC.downedGolemBoss)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate,
                    ItemID.SoulofNight,         // 暗夜之魂
                    ItemID.CrimsonKeyMold,
                    ItemID.FleshBlock
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 7：世纪之花后 ──
            else if (NPC.downedPlantBoss)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate,
                    ItemID.SoulofNight,         // 暗夜之魂
                    ItemID.CrimsonKeyMold,
                    ItemID.FleshBlock
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 6：新三王后（任意机械 Boss）──
            else if (NPC.downedMechBossAny)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate,
                    ItemID.SoulofNight,         // 暗夜之魂
                    ItemID.CrimsonKeyMold
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 5：血肉之墙后（困难模式）──
            else if (Main.hardMode)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate,
                    ItemID.SoulofNight,         // 暗夜之魂
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 4：骷髅王后（地牢解锁）──
            else if (NPC.downedBoss3)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.CrimsonFishingCrate
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 3：世界吞噬者 / 克苏鲁之脑后 ──
            else if (NPC.downedBoss2)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 2：克苏鲁之眼后 ──
            else if (NPC.downedBoss1)
            {
                int[] pool = new int[]
                {
                    ItemID.BloodOrange,
                    ItemID.BloodWater,
                    ItemID.BloodMoonStarter,
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }

            // ── 阶段 1：肉前（啥 Boss 都没打）──
            else
            {
                int[] pool = new int[]
                {
                    ItemID.BatBat,              // 蝙蝠棍（原版稀有）
                    ItemID.RedPotion,           // 红药水
                    ItemID.Batfish,            // 手里剑
                };
                int itemToDrop = pool[Main.rand.Next(pool.Length)];
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, itemToDrop, 1);
            }
        }


        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("栖息于永夜之中的猩红幽灵，以鲜血为食，愈战愈强。据说其真身并非蝙蝠，不过是某位红色恶魔的倒影罢了。")
            });
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (Main.dayTime) return 0f;

            float chance = 0f;

            if (spawnInfo.Player.ZoneOverworldHeight)
                chance = 0.001f;   // 地表很罕见
            else if (spawnInfo.Player.ZoneRockLayerHeight)
                chance = 0.0001f;   // 地下稍微多一点

            // 阶段加成也要克制
            if (NPC.downedMoonlord) chance *= 1.5f;  // 最高 0.045f
            else if (NPC.downedPlantBoss) chance *= 1.3f;  // 最高 0.039f
            else if (Main.hardMode) chance *= 1.15f; // 最高 0.0345f

            return chance;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                        Main.rand.NextVector2Circular(4f, 4f), 0, Color.Crimson, 1.3f);
                    d.noGravity = true;
                }
            }
        }
    }
}