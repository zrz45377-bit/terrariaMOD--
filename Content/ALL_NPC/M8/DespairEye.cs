using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using TEACHER.Content.ITEM_S.N12;
using TEACHER.Content.Systems;

namespace TEACHER.Content.ALL_NPC.M8
{
    [AutoloadBossHead]
    public class DespairEye : ModNPC
    {
        // ──────────────────────────────────────────────
        //  贴图帧定义
        //  0-2 : 一/二/三阶段（有虹膜）
        //  3-5 : 四/五/六阶段（虹膜剥落）
        // ──────────────────────────────────────────────
        private const int FrameCount = 6;
        private const int Phase123FrameStart = 0;
        private const int Phase123FrameEnd = 2;
        private const int Phase456FrameStart = 3;
        private const int Phase456FrameEnd = 5;

        // ──────────────────────────────────────────────
        //  ai[] 用途
        //  ai[0] : 主状态（见下方 State 常量）
        //  ai[1] : 状态计时器（帧）
        //  ai[2] : 子计数器
        //  ai[3] : 已解锁的最高阶段（0-5）
        // ──────────────────────────────────────────────
        private ref float AI_State => ref NPC.ai[0];
        private ref float AI_Timer => ref NPC.ai[1];
        private ref float AI_Counter => ref NPC.ai[2];
        private ref float AI_Phase => ref NPC.ai[3];

        // localAI[0] : 环绕角度（不需要网络同步）
        // localAI[1] : 2/3阶段交替子状态（0=Spazmatism式, 1=Retinazer式）
        private ref float OrbitAngle => ref NPC.localAI[0];
        private ref float AlternateMode => ref NPC.localAI[1];

        // 主状态常量
        private const int STATE_HOVER = 0;
        private const int STATE_PHASE1 = 1;
        private const int STATE_PHASE23 = 2;
        private const int STATE_PHASE4 = 3;
        private const int STATE_PHASE5 = 4;
        private const int STATE_PHASE6 = 5;
        private const int STATE_TRANSFORM = 6;

        // 血量阈值（3000总血）
        private float HP_P2 => NPC.lifeMax * 0.95f; // 2850
        private float HP_P3 => NPC.lifeMax * 0.75f; // 2250
        private float HP_P4 => NPC.lifeMax * 0.50f; // 1500
        private float HP_P5 => NPC.lifeMax * 0.20f; // 600
        private float HP_P6 => NPC.lifeMax * 0.05f; // BUG修复：原为0.5f，应为0.05f → 150

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = FrameCount;
            NPCID.Sets.MPAllowedEnemies[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
            NPCID.Sets.SpecificDebuffImmunity[NPC.type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 100;
            NPC.height = 110;
            NPC.aiStyle = -1;
            NPC.damage = 25;
            NPC.defense = 14;
            NPC.lifeMax = 4500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.value = Item.buyPrice(gold: 8);
            NPC.timeLeft = 300;

            if (!Main.dedServ)
                Music = MusicID.Boss1;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("克苏鲁之眼的兄弟，抑或它褪去理智后的残骸。当虹膜剥落，剩下的只有纯粹的饥饿与绝望。")
            });
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            if (Main.expertMode)
                NPC.damage = (int)(NPC.damage * 0.85f);
        }

        // ══════════════════════════════════════════════
        //  主 AI 入口
        // ══════════════════════════════════════════════
        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.velocity.Y -= 0.04f;
                NPC.velocity.X *= 0.95f;
                if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                return;
            }

            CheckPhaseTransition(player);

            switch ((int)AI_State)
            {
                case STATE_HOVER: HoverAI(player); break;
                case STATE_PHASE1: Phase1AI(player); break;
                case STATE_PHASE23: Phase23AI(player); break;
                case STATE_PHASE4: Phase4AI(player); break;
                case STATE_PHASE5: Phase5AI(player); break;
                case STATE_PHASE6: Phase6AI(player); break;
                case STATE_TRANSFORM: TransformAI(); break;
            }

            Animate();
        }

        // ══════════════════════════════════════════════
        //  阶段切换检查
        // ══════════════════════════════════════════════
        private void CheckPhaseTransition(Player player)
        {
            int targetPhase = (int)AI_Phase;

            if (NPC.life <= HP_P6 && AI_Phase < 5) targetPhase = 5;
            else if (NPC.life <= HP_P5 && AI_Phase < 4) targetPhase = 4;
            else if (NPC.life <= HP_P4 && AI_Phase < 3) targetPhase = 3;
            else if (NPC.life <= HP_P3 && AI_Phase < 2) targetPhase = 2;
            else if (NPC.life <= HP_P2 && AI_Phase < 1) targetPhase = 1;

            if (targetPhase > (int)AI_Phase)
            {
                AI_Phase = targetPhase;
                AI_State = STATE_TRANSFORM;
                AI_Timer = 0f;
                AI_Counter = 0f;
                NPC.netUpdate = true;

                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    string msg = targetPhase switch
                    {
                        1 => "绝望之眼感到了威胁……",
                        5 => "绝望之眼，最后的绝望……",
                        _ => ""
                    };
                    if (msg != "")
                        Main.NewText(msg, Color.Crimson);

                    for (int i = 0; i < 50; i++)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                            Main.rand.NextVector2Circular(8f, 8f), 0, Color.DarkRed, 2f);
                        d.noGravity = true;
                    }
                }

                if (targetPhase >= 3)
                {
                    NPC.defense = 0;
                    NPC.damage = 30;
                }
            }
        }

        // ══════════════════════════════════════════════
        //  STATE_TRANSFORM
        // ══════════════════════════════════════════════
        private void TransformAI()
        {
            AI_Timer++;
            NPC.velocity *= 0.88f;
            NPC.rotation += 0.22f;

            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                        Main.rand.NextVector2Circular(5f, 5f), 0, Color.DarkRed, 1.4f);
                    d.noGravity = true;
                }
            }

            if (AI_Timer >= 80f)
            {
                AI_Timer = 0f;
                AI_Counter = 0f;
                AI_State = (int)AI_Phase switch
                {
                    0 => STATE_PHASE1,
                    1 => STATE_PHASE23,
                    2 => STATE_PHASE23,
                    3 => STATE_PHASE4,
                    4 => STATE_PHASE5,
                    5 => STATE_PHASE6,
                    _ => STATE_HOVER
                };
            }
        }

        // ══════════════════════════════════════════════
        //  STATE_HOVER：衔接用短暂悬浮
        // ══════════════════════════════════════════════
        private void HoverAI(Player player)
        {
            AI_Timer++;
            MoveTowards(player.Center + new Vector2(0, -260f), 0.18f, 7f);
            RotateTowards(player.Center, 0.1f);

            if (AI_Timer >= 60f)
            {
                AI_Timer = 0f;
                AI_Counter = 0f;
                AI_State = (int)AI_Phase switch
                {
                    0 => STATE_PHASE1,
                    1 or 2 => STATE_PHASE23,
                    3 => STATE_PHASE4,
                    4 => STATE_PHASE5,
                    5 => STATE_PHASE6,
                    _ => STATE_PHASE1
                };
            }
        }

        // ══════════════════════════════════════════════
        //  STATE_PHASE1：召唤仆从 + 每秒回5血 + 绕飞
        // ══════════════════════════════════════════════
        private void Phase1AI(Player player)
        {
            AI_Timer++;

            // 每秒回5HP
            if (AI_Timer % 60 == 0 && NPC.life < NPC.lifeMax)
            {
                NPC.life = Math.Min(NPC.life + 5, NPC.lifeMax);
                NPC.HealEffect(5);
            }

            // BUG修复：原为1000帧，改回300帧（5秒）
            if (AI_Timer % 300 == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = Main.expertMode ? 5 : 3;
                for (int i = 0; i < count; i++)
                {
                    Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(150f, 150f);
                    int servant = NPC.NewNPC(NPC.GetSource_FromAI(),
                        (int)spawnPos.X, (int)spawnPos.Y, NPCID.ServantofCthulhu);
                    if (Main.npc[servant].active)
                        Main.npc[servant].velocity =
                            (player.Center - Main.npc[servant].Center).SafeNormalize(Vector2.UnitY) * 4f;
                }
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            }

            OrbitAroundPlayer(player, radius: 300f, orbitSpeed: 0.035f, maxMoveSpeed: 7f);
            RotateTowards(player.Center, 0.1f);
        }

        // ══════════════════════════════════════════════
        //  STATE_PHASE23：火球 ↔ 激光，每10秒交替
        // ══════════════════════════════════════════════
        private void Phase23AI(Player player)
        {
            AI_Timer++;

            if (AI_Timer % 600 == 0)
            {
                AlternateMode = AlternateMode == 0 ? 1 : 0;
                AI_Counter = 0f;
                SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            }

            if (AlternateMode == 0)
                Phase2_SpazmatismStyle(player);
            else
                Phase3_RetinazerStyle(player);
        }

        // ── Spazmatism一阶段：每2秒射一波火球 ──
        private void Phase2_SpazmatismStyle(Player player)
        {
            Vector2 targetPos = player.Center + new Vector2(NPC.direction * -200f, -280f);
            MoveTowards(targetPos, 0.2f, 7f);
            RotateTowards(player.Center, 0.12f);

            // BUG修复：原为% 360 == 60（600帧内只触发1次），改为% 120 == 60（每2秒一波）
            if (AI_Timer % 120 == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = Main.expertMode ? 4 : 3;
                for (int i = 0; i < count; i++)
                {
                    float spread = MathHelper.ToRadians(15f * (i - count / 2f));
                    Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedBy(spread);
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(), NPC.Center,
                        dir * 8f,
                        ProjectileID.CursedFlameHostile,
                        NPC.damage / 2, 2f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
            }
        }

        // ── Retinazer一阶段：每20帧射一发激光 ──
        private void Phase3_RetinazerStyle(Player player)
        {
            Vector2 targetPos = player.Center + new Vector2(0f, -300f);
            MoveTowards(targetPos, 0.18f, 6f);
            RotateTowards(player.Center, 0.15f);

            // BUG修复：原为% 100（每1.67秒1发），改为% 20（每0.33秒1发，有压迫感）
            if (AI_Timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(), NPC.Center,
                    dir * 9.6f,
                    ProjectileID.EyeLaser,
                    NPC.damage / 2, 1f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
            }
        }

        // ══════════════════════════════════════════════
        //  STATE_PHASE4：克苏鲁之眼二阶段冲锋
        // ══════════════════════════════════════════════
        private void Phase4AI(Player player)
        {
            AI_Timer++;

            int dashPrepare = 30;
            int dashDuration = 45;
            int totalCycle = dashPrepare + dashDuration;
            int cycleTimer = (int)AI_Timer % totalCycle;

            if (cycleTimer < dashPrepare)
            {
                MoveTowards(player.Center + new Vector2(0, -200f), 0.15f, 5f);
                RotateTowards(player.Center, 0.2f);
            }
            else if (cycleTimer == dashPrepare)
            {
                Vector2 dashDir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                NPC.velocity = dashDir * 16f;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                for (int i = 0; i < 15; i++)
                    Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                        Main.rand.NextVector2Circular(5f, 5f), 0, Color.Crimson, 1.4f);
            }
            else
            {
                RotateTowardsVelocity(0.3f);
                NPC.velocity *= 0.97f;
            }
        }

        // ══════════════════════════════════════════════
        //  STATE_PHASE5：Spazmatism二阶段，追逐+扇形火球
        // ══════════════════════════════════════════════
        private void Phase5AI(Player player)
        {
            AI_Timer++;

            // BUG修复①：加速度从0.01f→0.35f，最大速度从0.3-0.4f→9-11f
            Vector2 toPlayer = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
            NPC.velocity += toPlayer * 0.35f;
            float maxSpeed = Main.expertMode ? 11f : 9f;
            if (NPC.velocity.Length() > maxSpeed)
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * maxSpeed;

            RotateTowardsVelocity(0.2f);

            // BUG修复②：射击间隔从% 3→% 25，防止每帧狂喷导致弹幕爆炸
            if (AI_Timer % 25 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float baseAngle = (player.Center - NPC.Center).ToRotation();
                int pellets = 3;
                float spread = MathHelper.ToRadians(50f);
                for (int i = 0; i < pellets; i++)
                {
                    float angle = baseAngle - spread / 2f + spread * (i / (float)(pellets - 1));
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 7f;
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.CursedFlameHostile,
                        NPC.damage / 2, 2f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item34, NPC.Center);
            }
        }

        // ══════════════════════════════════════════════
        //  STATE_PHASE6：Retinazer二阶段死亡激光
        // ══════════════════════════════════════════════
        private void Phase6AI(Player player)
        {
            AI_Timer++;

            int chargeTime = 60;
            int shootTime = 120;
            int cooldown = 60;
            int cycleFull = chargeTime + shootTime + cooldown;
            int t = (int)AI_Timer % cycleFull;

            if (t < chargeTime)
            {
                MoveTowards(player.Center + new Vector2(0, -200f), 0.5f, 10f);
                RotateTowards(player.Center, 0.25f);

                if (Main.netMode != NetmodeID.Server && t % 6 == 0)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.MagicMirror,
                        Main.rand.NextVector2Circular(4f, 4f), 0, Color.DeepSkyBlue, 1.6f);
                    d.noGravity = true;
                }
            }
            else if (t == chargeTime)
            {
                SoundEngine.PlaySound(SoundID.Item33, NPC.Center);
            }
            else if (t < chargeTime + shootTime)
            {
                NPC.velocity *= 0.92f;
                RotateTowards(player.Center, 0.3f);

                if (t % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(), NPC.Center,
                        dir * 15f,
                        ProjectileID.EyeLaser,
                        NPC.damage / 2, 1f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                }
            }
            else
            {
                Vector2 away = (NPC.Center - player.Center).SafeNormalize(Vector2.UnitY);
                NPC.velocity += away * 0.3f;
                if (NPC.velocity.Length() > 6f)
                    NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * 6f;
                NPC.velocity *= 0.97f;
            }
        }

        // ══════════════════════════════════════════════
        //  移动辅助
        // ══════════════════════════════════════════════
        private void MoveTowards(Vector2 targetPos, float accel, float maxSpeed)
        {
            Vector2 toTarget = targetPos - NPC.Center;
            NPC.velocity += toTarget.SafeNormalize(Vector2.Zero) * accel;
            if (NPC.velocity.Length() > maxSpeed)
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;
            NPC.velocity *= 0.97f;
        }

        private void OrbitAroundPlayer(Player player, float radius, float orbitSpeed, float maxMoveSpeed)
        {
            OrbitAngle += orbitSpeed;
            Vector2 orbitTarget = player.Center + new Vector2(
                (float)Math.Cos(OrbitAngle) * radius,
                (float)Math.Sin(OrbitAngle) * radius * 0.45f - 120f);
            MoveTowards(orbitTarget, 0.35f, maxMoveSpeed);
        }

        // ══════════════════════════════════════════════
        //  旋转辅助
        // ══════════════════════════════════════════════
        private float AngleTowards(float current, float target, float maxChange)
        {
            float diff = MathHelper.WrapAngle(target - current);
            if (Math.Abs(diff) <= maxChange) return target;
            return current + Math.Sign(diff) * maxChange;
        }

        private void RotateTowards(Vector2 targetPos, float maxRotateSpeed)
        {
            Vector2 toTarget = targetPos - NPC.Center;
            if (toTarget == Vector2.Zero) return;
            float targetRotation = toTarget.ToRotation() - MathHelper.PiOver2;
            NPC.rotation = AngleTowards(NPC.rotation, targetRotation, maxRotateSpeed);
        }

        private void RotateTowardsVelocity(float maxRotateSpeed)
        {
            if (NPC.velocity == Vector2.Zero) return;
            float targetRotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
            NPC.rotation = AngleTowards(NPC.rotation, targetRotation, maxRotateSpeed);
        }

        // ══════════════════════════════════════════════
        //  动画
        // ══════════════════════════════════════════════
        private void Animate()
        {
            bool latePhase = AI_Phase >= 3;
            NPC.frameCounter++;
            int frameSpeed = latePhase ? 4 : 6;
            int frameHeight = TextureAssets.Npc[NPC.type].Value.Height / FrameCount;

            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                int cur = NPC.frame.Y / frameHeight;
                int start = latePhase ? Phase456FrameStart : Phase123FrameStart;
                int end = latePhase ? Phase456FrameEnd : Phase123FrameEnd;

                cur++;
                if (cur < start || cur > end) cur = start;
                NPC.frame.Y = cur * frameHeight;
            }
        }

        // ══════════════════════════════════════════════
        //  受击 / 死亡 / 掉落
        // ══════════════════════════════════════════════
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Bleeding, 180);
            if (AI_Phase >= 3)
                target.AddBuff(BuffID.Weak, 240);
            if (AI_Phase >= 5)
                target.AddBuff(BuffID.Darkness, 300);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                        Main.rand.NextVector2Circular(8f, 8f), 0, Color.Crimson, 1.8f);
                    d.noGravity = true;
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
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.DemoniteOre, Main.rand.Next(40, 71));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.CorruptSeeds, Main.rand.Next(3, 7));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.Lens, Main.rand.Next(3, 6));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.GoldCoin, Main.rand.Next(5, 10));
            if (Main.rand.NextBool(25))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.OpticStaff, 1);
            }

            if (!DownedBossSystem.downedDespairEye)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ModContent.ItemType<DugOutEye>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.OpticStaff, 1);
                DownedBossSystem.downedDespairEye = true;

                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("绝望之眼的眼球滚落到了地上……", Color.Crimson);
            }
            else if (Main.rand.NextBool(50))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ModContent.ItemType<DugOutEye>(), 1);
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (Main.dayTime) return 0f;
            if (!spawnInfo.Player.ZoneOverworldHeight) return 0f;
            if (!NPC.downedBoss1) return 0f;
            return 0.0001f;
        }
    }
}