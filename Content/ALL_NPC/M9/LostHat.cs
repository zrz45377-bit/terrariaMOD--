using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M9
{
    [AutoloadBossHead]
    public class LostHat : ModNPC
    {
        // ========== 状态枚举 ==========
        private enum State
        {
            Unconscious,    // 无意识游荡（隐形）
            Manifest,       // 显形预兆
            FakeManifest,   // 虚假预兆（陷阱）
            Strike,         // 一击
            ReturnStrike,   // 折返补刀
            ShadowClone,    // 分裂幻影
            Fade,           // 消散隐匿
            Awakening,      // 第三眼觉醒过渡
            Phase2Orbit,    // 二阶：绕行凝视
            Phase2Burst     // 二阶：连续突袭
        }

        // ========== AI参数槽 ==========
        // NPC.ai[0] = (float)currentState
        // NPC.ai[1] = stateTimer
        // NPC.ai[2] = globalTimer
        // NPC.ai[3] = 杂用（幻影ID、折返计数等）

        // ========== 核心变量 ==========
        private State CurrentState
        {
            get => (State)(int)NPC.ai[0];
            set => NPC.ai[0] = (float)(int)value;
        }
        private ref float StateTimer => ref NPC.ai[1];
        private ref float GlobalTimer => ref NPC.ai[2];
        private ref float AuxData => ref NPC.ai[3];

        private Vector2 strikeDirection;
        private Vector2 strikeOrigin;
        private List<Vector2> phantomTrails = new List<Vector2>();
        private List<Vector2> decoyPositions = new List<Vector2>();

        private bool thirdEyeOpen = false;
        private bool awakeningPlayed = false;
        private int despawnTimer = 0;
        private int burstCount = 0;
        private float orbitAngle = 0f;

        // 折返补刀
        private bool hasReturnedStrike = false;
        private int returnStrikeDelay = 0;

        // 心跳节律
        private float heartbeatPhase = 0f;

        // ========== 配置参数 ==========
        private const int MANIFEST_DURATION = 50;
        private const int FAKE_MANIFEST_DURATION = 40;
        private const int STRIKE_DURATION = 22;
        private const int RETURN_STRIKE_DURATION = 18;
        private const int FADE_DURATION = 28;
        private const int AWAKENING_DURATION = 120;
        private const int PHASE2_ORBIT_DURATION = 90;
        private const int PHASE2_BURST_MAX = 4;

        private const float STRIKE_SPEED_P1 = 36f;
        private const float STRIKE_SPEED_P2 = 55f;
        private const float RETURN_SPEED = 42f;

        private const float MAX_DISTANCE = 2600f;
        private const float TELEPORT_DISTANCE = 1900f;
        private const float IDEAL_ORBIT = 380f;
        private const float PHASE2_ORBIT_RADIUS = 260f;

        // ========== 颜色主题 ==========
        private static Color EyeColor => new Color(255, 60, 160);
        private static Color TrailColor => new Color(180, 40, 100);
        private static Color WarnColor => new Color(255, 120, 200);

        // ===================================================================
        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 75;
            NPC.lifeMax = 14000;
            NPC.scale = 1.1f;
            NPC.value = Item.buyPrice(0, 80, 0, 0);
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.defense = 18;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath7;
            NPC.npcSlots = 12;
            NPC.alpha = 255;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            Music = MusicLoader.GetMusicSlot(Mod, "Content/Sounds/Music/BossVice");
            SceneEffectPriority = SceneEffectPriority.BossLow;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement(
                    "没有人能说清楚那顶帽子第一次出现是在哪里。\n" +
                    "只知道——当你转过身，它就已经在那里了。\n" +
                    "没有脚步声。没有来路。\n\n" +
                    "研究者们试图记录它的行动规律，\n" +
                    "但所有的笔记在第二天早晨都变成了空白。\n" +
                    "连 我曾经见过它 这件事本身，也会被遗忘。\n\n" +
                    "第三只眼睁开之前，它只是在等。\n" +
                    "没有人知道它在等什么。"
                )
            });
        }

        // ===================================================================
        // ========== 主AI ==========
        // ===================================================================
        public override void AI()
        {
            GlobalTimer++;
            StateTimer++;
            heartbeatPhase += thirdEyeOpen ? 0.18f : 0.08f;

            Player target = GetTargetPlayer();
            if (target == null) { HandleDespawn(); return; }

            float distToPlayer = Vector2.Distance(NPC.Center, target.Center);

            // 超距消失
            if (distToPlayer > MAX_DISTANCE)
            {
                despawnTimer++;
                if (despawnTimer > 200) { NPC.active = false; return; }
            }
            else despawnTimer = 0;

            // 过远强制瞬移（非冲刺类状态）
            if (distToPlayer > TELEPORT_DISTANCE &&
                CurrentState != State.Strike &&
                CurrentState != State.ReturnStrike &&
                CurrentState != State.ShadowClone &&
                CurrentState != State.Awakening)
            {
                ForceTeleportToPlayer(target, 320f);
                return;
            }

            // 防贴脸
            if (distToPlayer < 70f && CurrentState == State.Unconscious)
            {
                NPC.Center += Vector2.Normalize(NPC.Center - target.Center) * 6f;
            }

            // 第三眼觉醒检测
            if (!thirdEyeOpen && NPC.life < NPC.lifeMax * 0.35f && CurrentState != State.Awakening)
            {
                SwitchState(State.Awakening);
                return;
            }

            switch (CurrentState)
            {
                case State.Unconscious: HandleUnconscious(target, distToPlayer); break;
                case State.Manifest: HandleManifest(target); break;
                case State.FakeManifest: HandleFakeManifest(target); break;
                case State.Strike: HandleStrike(target); break;
                case State.ReturnStrike: HandleReturnStrike(target); break;
                case State.ShadowClone: HandleShadowClone(target); break;
                case State.Fade: HandleFade(target); break;
                case State.Awakening: HandleAwakening(target); break;
                case State.Phase2Orbit: HandlePhase2Orbit(target); break;
                case State.Phase2Burst: HandlePhase2Burst(target); break;
            }

            UpdateTrails();
            UpdateVisuals();
        }

        // ===================================================================
        // ========== 状态：无意识游荡 ==========
        // ===================================================================
        private void HandleUnconscious(Player target, float currentDist)
        {
            NPC.alpha = 255;
            NPC.velocity *= 0.90f;
            decoyPositions.Clear();

            // 轨道保持
            float orbitError = currentDist - IDEAL_ORBIT;
            if (Math.Abs(orbitError) > 80f)
            {
                Vector2 dir = Vector2.Normalize(orbitError > 0 ? target.Center - NPC.Center : NPC.Center - target.Center);
                NPC.Center += dir * (Math.Abs(orbitError) > 200f ? 4f : 2.5f);
            }

            // 漂移抖动
            if (GlobalTimer % 55 == 0)
                NPC.velocity += Main.rand.NextVector2Circular(2.5f, 2.5f);

            // 幻影尾迹
            if (GlobalTimer % 5 == 0)
            {
                phantomTrails.Insert(0, NPC.Center);
                if (phantomTrails.Count > 10) phantomTrails.RemoveAt(phantomTrails.Count - 1);
            }

            // 幽灵残影（欺骗视线）
            if (GlobalTimer % 80 == 0 && Main.rand.NextBool(2))
            {
                SpawnGhostDecoy(target);
            }

            // 触发显形
            float triggerDist = thirdEyeOpen ? 180f : 130f;
            int triggerRand = thirdEyeOpen ? 70 : 120;
            bool shouldManifest = currentDist < triggerDist || Main.rand.NextBool(triggerRand);
            if (NPC.life < NPC.lifeMax * 0.6f) triggerRand = thirdEyeOpen ? 45 : 80;

            if (shouldManifest)
            {
                // 低血量概率触发假预兆
                bool doFake = NPC.life < NPC.lifeMax * 0.55f && Main.rand.NextBool(3);

                // 高概率触发幻影分裂
                bool doClone = thirdEyeOpen && NPC.life < NPC.lifeMax * 0.3f && Main.rand.NextBool(2);

                if (doClone)
                {
                    NPC.Center = FindBlindSpot(target);
                    SwitchState(State.ShadowClone);
                }
                else
                {
                    NPC.Center = FindBlindSpot(target);
                    SwitchState(doFake ? State.FakeManifest : State.Manifest);
                }
            }
        }

        // ===================================================================
        // ========== 状态：真·显形预兆 ==========
        // ===================================================================
        private void HandleManifest(Player target)
        {
            float progress = StateTimer / (float)MANIFEST_DURATION;
            NPC.alpha = (int)(255 * (1f - progress * 0.75f));
            NPC.velocity *= 0.85f;

            strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
            NPC.spriteDirection = strikeDirection.X > 0 ? 1 : -1;

            // 脉冲缩放
            float pulse = 1f + (float)Math.Sin(StateTimer * 0.55f) * 0.18f;
            NPC.scale = 1.1f * pulse;

            // 警告粒子：沿冲刺线
            if ((int)StateTimer % 3 == 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    float t = i / 12f;
                    Vector2 linePos = Vector2.Lerp(NPC.Center, target.Center, t * progress);
                    var d = Dust.NewDustPerfect(linePos, DustID.PinkTorch, Vector2.Zero, 100, WarnColor, 0.9f);
                    Main.dust[d.dustIndex].noGravity = true;
                    Main.dust[d.dustIndex].fadeIn = 0.4f;
                }
            }

            // 收拢粒子：从外向内
            if ((int)StateTimer % 5 == 0)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 fromFar = NPC.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 120f;
                Vector2 velocity = Vector2.Normalize(NPC.Center - fromFar) * 5f;
                var d = Dust.NewDustPerfect(fromFar, DustID.GemRuby, velocity, 0, EyeColor, 1.3f);
                Main.dust[d.dustIndex].noGravity = true;
            }

            // 心跳音效
            if ((int)StateTimer == 15 || (int)StateTimer == 30)
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f, Pitch = -0.3f }, NPC.position);

            if (StateTimer >= MANIFEST_DURATION)
            {
                hasReturnedStrike = false;
                strikeOrigin = NPC.Center;
                SwitchState(State.Strike);
            }
        }

        // ===================================================================
        // ========== 状态：假预兆（陷阱）==========
        // ===================================================================
        private void HandleFakeManifest(Player target)
        {
            float progress = StateTimer / (float)FAKE_MANIFEST_DURATION;
            NPC.alpha = (int)(255 * (1f - progress * 0.65f));
            NPC.velocity *= 0.85f;

            // 虚假的预兆方向（偏移真实方向）
            Vector2 fakeDir = Vector2.Normalize(target.Center - NPC.Center);
            fakeDir = fakeDir.RotatedBy(MathHelper.ToRadians(Main.rand.Next(30, 70) * (Main.rand.NextBool() ? 1 : -1)));
            NPC.spriteDirection = fakeDir.X > 0 ? 1 : -1;

            // 同样画警告线，但方向是假的
            if ((int)StateTimer % 4 == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    float t = i / 8f;
                    Vector2 linePos = NPC.Center + fakeDir * (t * 200f * progress);
                    var d = Dust.NewDustPerfect(linePos, DustID.PinkTorch, Vector2.Zero, 150, WarnColor * 0.7f, 0.7f);
                    Main.dust[d.dustIndex].noGravity = true;
                }
            }

            if (StateTimer >= FAKE_MANIFEST_DURATION)
            {
                // 真身从盲点突袭
                strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
                strikeOrigin = NPC.Center;
                hasReturnedStrike = false;
                NPC.alpha = 255;
                SwitchState(State.Strike);
            }
        }

        // ===================================================================
        // ========== 状态：一击 ==========
        // ===================================================================
        private void HandleStrike(Player target)
        {
            float speed = thirdEyeOpen ? STRIKE_SPEED_P2 : STRIKE_SPEED_P1;

            if ((int)StateTimer == 1)
            {
                NPC.velocity = strikeDirection * speed;
                NPC.alpha = 30;

                // 出击粒子爆发
                for (int i = 0; i < 35; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(6f, 16f);
                    var d = Dust.NewDust(NPC.Center, 1, 1, DustID.PinkTorch, vel.X, vel.Y, 0, default, 2.2f);
                    Main.dust[d].noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = thirdEyeOpen ? 0.4f : 0f }, NPC.position);
            }

            // 第三眼开启后持续追踪（更强的转向）
            if (thirdEyeOpen && (int)StateTimer % 3 == 0)
            {
                Vector2 newDir = Vector2.Normalize(target.Center - NPC.Center);
                float trackStrength = NPC.life < NPC.lifeMax * 0.2f ? 0.22f : 0.14f;
                strikeDirection = Vector2.Lerp(strikeDirection, newDir, trackStrength).SafeNormalize(Vector2.UnitX);
                NPC.velocity = strikeDirection * speed;
            }

            // 残影
            if ((int)GlobalTimer % 2 == 0)
            {
                var d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemRuby, 0, 0, 0, EyeColor, 1.3f);
                Main.dust[d].noGravity = true;
            }

            bool hitWall = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            bool timeUp = StateTimer >= STRIKE_DURATION;

            if (timeUp || hitWall)
            {
                if (hitWall)
                {
                    Collision.HitTiles(NPC.position, NPC.velocity, NPC.width, NPC.height);
                    SpawnImpactDust(25, DustID.Stone);
                    SoundEngine.PlaySound(SoundID.Item10, NPC.position);
                }

                // 判断是否触发折返
                float missThreshold = 180f;
                bool missed = Vector2.Distance(NPC.Center, target.Center) > missThreshold;

                if (missed && !hasReturnedStrike)
                {
                    NPC.velocity *= 0.15f;
                    strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
                    SwitchState(State.ReturnStrike);
                }
                else
                {
                    NPC.velocity *= 0.12f;
                    SwitchState(State.Fade);
                }
            }
        }

        // ===================================================================
        // ========== 状态：折返补刀 ==========
        // ===================================================================
        private void HandleReturnStrike(Player target)
        {
            if ((int)StateTimer == 1)
            {
                hasReturnedStrike = true;
                strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
                NPC.velocity = strikeDirection * RETURN_SPEED;
                NPC.alpha = 20;

                // 折返特效：更密集更快的粒子
                for (int i = 0; i < 25; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 vel = strikeDirection * Main.rand.NextFloat(4f, 12f) + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 3f;
                    var d = Dust.NewDust(NPC.Center, 1, 1, DustID.GemRuby, vel.X, vel.Y, 0, EyeColor, 1.8f);
                    Main.dust[d].noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.6f, Volume = 0.8f }, NPC.position);
            }

            // 折返追踪更强
            if ((int)StateTimer % 2 == 0)
            {
                Vector2 newDir = Vector2.Normalize(target.Center - NPC.Center);
                strikeDirection = Vector2.Lerp(strikeDirection, newDir, 0.2f).SafeNormalize(Vector2.UnitX);
                NPC.velocity = strikeDirection * RETURN_SPEED;
            }

            if ((int)GlobalTimer % 2 == 0)
            {
                var d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, 0, 0, 0, WarnColor, 1.5f);
                Main.dust[d].noGravity = true;
            }

            bool hitWall = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            if (StateTimer >= RETURN_STRIKE_DURATION || hitWall)
            {
                if (hitWall)
                {
                    SpawnImpactDust(20, DustID.Stone);
                    SoundEngine.PlaySound(SoundID.Item10, NPC.position);
                }
                NPC.velocity *= 0.1f;
                SwitchState(State.Fade);
            }
        }

        // ===================================================================
        // ========== 状态：幻影分裂 ==========
        // ===================================================================
        private void HandleShadowClone(Player target)
        {
            if ((int)StateTimer == 1)
            {
                NPC.alpha = 80;
                // 召唤2个幻影NPC（如果ModContent.NPCType<LostHatClone>()已注册则用之，否则用视觉欺骗）
                SpawnVisualClones(target, 2);
                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.5f, Volume = 0.6f }, NPC.position);
            }

            // 幻影显形期间本体缓慢靠近盲点
            if (StateTimer < 30f)
            {
                Vector2 blindSpot = target.Center + new Vector2(
                    target.velocity.SafeNormalize(Vector2.Zero).X > 0 ? -200f : 200f, -120f);
                NPC.Center = Vector2.Lerp(NPC.Center, blindSpot, 0.08f);
                NPC.alpha = (int)MathHelper.Lerp(80f, 30f, StateTimer / 30f);
            }

            if (StateTimer >= 45f)
            {
                strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
                strikeOrigin = NPC.Center;
                hasReturnedStrike = false;
                SwitchState(State.Strike);
            }
        }

        // ===================================================================
        // ========== 状态：消散隐匿 ==========
        // ===================================================================
        private void HandleFade(Player target)
        {
            float progress = StateTimer / (float)FADE_DURATION;
            NPC.alpha = (int)(255 * progress);
            NPC.velocity *= 0.88f;
            NPC.scale = 1.1f * (1f - progress * 0.25f);

            if ((int)StateTimer % 4 == 0)
            {
                var d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, 0, 0, 0, default, 1.1f);
                Main.dust[d].noGravity = true;
            }

            if (StateTimer >= FADE_DURATION)
            {
                NPC.scale = 1.1f;
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float orbitRadius = thirdEyeOpen ? IDEAL_ORBIT * 0.8f : IDEAL_ORBIT;
                NPC.Center = FindSafePosition(target.Center + new Vector2(
                    (float)Math.Cos(angle) * orbitRadius,
                    (float)Math.Sin(angle) * orbitRadius * 0.65f));

                SwitchState(thirdEyeOpen ? State.Phase2Orbit : State.Unconscious);
            }
        }

        // ===================================================================
        // ========== 状态：第三眼觉醒过渡 ==========
        // ===================================================================
        private void HandleAwakening(Player target)
        {
            NPC.velocity *= 0.95f;

            if ((int)StateTimer == 1)
            {
                awakeningPlayed = false;
                NPC.alpha = 0;

                // 停止移动，强制出现在玩家正上方
                NPC.Center = target.Center - new Vector2(0, 250f);
            }

            float prog = StateTimer / (float)AWAKENING_DURATION;

            // 大范围粒子内缩
            if ((int)StateTimer % 2 == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 farPos = NPC.Center + Main.rand.NextVector2Circular(700f, 500f);
                    Vector2 vel = Vector2.Normalize(NPC.Center - farPos) * Main.rand.NextFloat(4f, 9f);
                    var d = Dust.NewDustPerfect(farPos, DustID.GemRuby, vel, 0, EyeColor, 2.5f * prog + 0.5f);
                    Main.dust[d.dustIndex].noGravity = true;
                }
            }

            // 脉冲光照 + 缩放律动
            float pulse = (float)Math.Sin(StateTimer * 0.25f);
            NPC.scale = 1.1f + pulse * 0.4f * prog;
            Lighting.AddLight(NPC.Center, (1.2f + pulse * 0.5f) * prog, 0.2f * prog, 0.4f * prog);

            // 音效节点
            if (!awakeningPlayed && StateTimer >= 40)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.position);
                awakeningPlayed = true;
            }

            if (StateTimer >= 60 && (int)StateTimer % 20 == 0)
                SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.5f + prog * 0.5f }, NPC.position);

            // 觉醒完成
            if (StateTimer >= AWAKENING_DURATION)
            {
                thirdEyeOpen = true;
                NPC.damage = 95;
                NPC.defense = 25;

                // 觉醒爆发粒子
                for (int i = 0; i < 120; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float r = Main.rand.NextFloat(3f, 18f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r);
                    var d = Dust.NewDust(NPC.Center, 1, 1, DustID.GemRuby, vel.X, vel.Y, 0, EyeColor, 3f);
                    Main.dust[d].noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.8f }, NPC.position);
                burstCount = 0;
                orbitAngle = 0f;
                SwitchState(State.Phase2Orbit);
            }
        }

        // ===================================================================
        // ========== 二阶状态：绕行凝视 ==========
        // ===================================================================
        private void HandlePhase2Orbit(Player target)
        {
            // 快速公转
            orbitAngle += 0.065f;
            Vector2 desiredPos = target.Center + new Vector2(
                (float)Math.Cos(orbitAngle) * PHASE2_ORBIT_RADIUS,
                (float)Math.Sin(orbitAngle) * PHASE2_ORBIT_RADIUS * 0.6f);

            NPC.velocity = (desiredPos - NPC.Center) * 0.18f;
            float progress = StateTimer / (float)PHASE2_ORBIT_DURATION;
            NPC.alpha = (int)MathHelper.Lerp(200f, 0f, progress);

            NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;

            // 绕行时散射小粒子
            if ((int)GlobalTimer % 3 == 0)
            {
                var d = Dust.NewDustPerfect(NPC.Center, DustID.PinkTorch,
                    NPC.velocity * -0.3f + Main.rand.NextVector2Circular(1f, 1f),
                    100, EyeColor * 0.6f, 0.8f);
                Main.dust[d.dustIndex].noGravity = true;
            }

            if (StateTimer >= PHASE2_ORBIT_DURATION)
            {
                burstCount = 0;
                strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
                strikeOrigin = NPC.Center;
                hasReturnedStrike = false;
                SwitchState(State.Phase2Burst);
            }
        }

        // ===================================================================
        // ========== 二阶状态：连续突袭 ==========
        // ===================================================================
        private void HandlePhase2Burst(Player target)
        {
            float speed = STRIKE_SPEED_P2 + burstCount * 4f;

            if ((int)StateTimer == 1)
            {
                NPC.alpha = 0;
                NPC.velocity = strikeDirection * speed;

                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = strikeDirection * Main.rand.NextFloat(5f, 14f);
                    vel += Main.rand.NextVector2Circular(3f, 3f);
                    var d = Dust.NewDust(NPC.Center, 1, 1, DustID.GemRuby, vel.X, vel.Y, 0, EyeColor, 2f);
                    Main.dust[d].noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.2f + burstCount * 0.15f }, NPC.position);
            }

            // 每次突袭都强追踪
            if ((int)StateTimer % 2 == 0)
            {
                Vector2 newDir = Vector2.Normalize(target.Center - NPC.Center);
                strikeDirection = Vector2.Lerp(strikeDirection, newDir, 0.18f).SafeNormalize(Vector2.UnitX);
                NPC.velocity = strikeDirection * speed;
            }

            if ((int)GlobalTimer % 2 == 0)
            {
                var d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemRuby, 0, 0, 0, EyeColor, 1.5f);
                Main.dust[d].noGravity = true;
            }

            bool hitWall = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            if (StateTimer >= 20 || hitWall)
            {
                if (hitWall) SpawnImpactDust(18, DustID.Stone);
                NPC.velocity *= 0.1f;
                burstCount++;

                if (burstCount >= PHASE2_BURST_MAX)
                {
                    // 连续突袭结束，回到绕行
                    orbitAngle = (float)Math.Atan2(NPC.Center.Y - target.Center.Y, NPC.Center.X - target.Center.X);
                    SwitchState(State.Phase2Orbit);
                }
                else
                {
                    // 短暂停顿后再次突袭，重新指向玩家
                    strikeDirection = Vector2.Normalize(target.Center - NPC.Center);
                    strikeOrigin = NPC.Center;
                    SwitchState(State.Phase2Burst);
                }
            }
        }

        // ===================================================================
        // ========== 工具方法 ==========
        // ===================================================================

        private void ForceTeleportToPlayer(Player target, float radius)
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            NPC.Center = FindSafePosition(target.Center + new Vector2(
                (float)Math.Cos(angle) * radius,
                (float)Math.Sin(angle) * radius * 0.7f));
            NPC.velocity = Vector2.Zero;

            CurrentState = State.Unconscious;
            StateTimer = 0;
            GlobalTimer = 0;
            NPC.alpha = 255;
            phantomTrails.Clear();

            SpawnImpactDust(20, DustID.MagicMirror);
            SoundEngine.PlaySound(SoundID.Item8, NPC.position);
        }

        private void SpawnGhostDecoy(Player target)
        {
            // 在玩家视野中随机生成虚假幽灵位置（纯粒子，无伤害）
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 decoyPos = target.Center + new Vector2(
                (float)Math.Cos(angle) * Main.rand.NextFloat(200f, 500f),
                (float)Math.Sin(angle) * Main.rand.NextFloat(150f, 350f));

            for (int i = 0; i < 15; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(1.5f, 1.5f);
                var d = Dust.NewDustPerfect(decoyPos, DustID.Ghost, vel, 200, new Color(200, 100, 180) * 0.4f, 1.2f);
                Main.dust[d.dustIndex].noGravity = true;
                Main.dust[d.dustIndex].fadeIn = 1f;
            }
        }

        private void SpawnVisualClones(Player target, int count)
        {
            for (int c = 0; c < count; c++)
            {
                float angle = MathHelper.TwoPi / count * c + Main.rand.NextFloat(0.3f);
                Vector2 clonePos = target.Center + new Vector2(
                    (float)Math.Cos(angle) * 280f,
                    (float)Math.Sin(angle) * 180f);

                // 幻影粒子群，模拟另一个本体出现
                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                    var d = Dust.NewDustPerfect(clonePos, DustID.PinkTorch, vel, 100, WarnColor, 1.6f);
                    Main.dust[d.dustIndex].noGravity = true;
                }
            }
        }

        private void SpawnImpactDust(int count, int dustType)
        {
            for (int i = 0; i < count; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType,
                    -NPC.velocity.X * 0.25f, -NPC.velocity.Y * 0.25f);
            }
        }

        private Vector2 FindBlindSpot(Player target)
        {
            // 优先出现在玩家移动反方向（真正的盲点）
            Vector2 moveDir = target.velocity.SafeNormalize(Vector2.Zero);
            Vector2 behind = target.Center - moveDir * 160f;
            Vector2 above = target.Center + new Vector2(Main.rand.NextBool() ? -140f : 140f, -170f);
            Vector2 sideLeft = target.Center + new Vector2(-220f, Main.rand.NextFloat(-60f, 60f));
            Vector2 sideRight = target.Center + new Vector2(220f, Main.rand.NextFloat(-60f, 60f));

            Vector2[] candidates = { behind, above, sideLeft, sideRight };

            // 选最远的（减少玩家提前看到的概率）
            Vector2 best = candidates[0];
            float bestDist = Vector2.Distance(NPC.Center, candidates[0]);
            foreach (var c in candidates)
            {
                float d = Vector2.Distance(NPC.Center, c);
                if (d > bestDist) { bestDist = d; best = c; }
            }
            return FindSafePosition(best);
        }

        private Vector2 FindSafePosition(Vector2 desiredPos)
        {
            Point tilePos = desiredPos.ToTileCoordinates();
            for (int i = 0; i < 50; i++)
            {
                if (WorldGen.TileEmpty(tilePos.X, tilePos.Y - i) &&
                    WorldGen.TileEmpty(tilePos.X + 2, tilePos.Y - i))
                    return new Vector2(tilePos.X * 16 + 16, (tilePos.Y - i) * 16 - 20);
            }
            return desiredPos;
        }

        private Player GetTargetPlayer()
        {
            NPC.TargetClosest(true);
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead) return null;
            return target;
        }

        private void HandleDespawn()
        {
            NPC.velocity *= 0.95f;
            if ((int)GlobalTimer % 300 == 0) NPC.active = false;
        }

        private void SwitchState(State newState)
        {
            CurrentState = newState;
            StateTimer = 0;
        }

        private void UpdateTrails()
        {
            if (phantomTrails.Count > 0 && CurrentState == State.Unconscious)
            {
                if ((int)GlobalTimer % 12 == 0)
                {
                    foreach (var pos in phantomTrails)
                    {
                        if (Main.rand.NextBool(4))
                        {
                            var d = Dust.NewDustPerfect(pos, DustID.Ghost, Vector2.Zero, 200, TrailColor * 0.25f, 0.5f);
                            Main.dust[d.dustIndex].noGravity = true;
                        }
                    }
                }
            }
        }

        private void UpdateVisuals()
        {
            if (thirdEyeOpen)
            {
                // 心跳律动光照
                float heartbeat = (float)Math.Sin(heartbeatPhase);
                float heartbeatStrong = (float)Math.Pow(Math.Max(0, heartbeat), 2.5f);
                float intensity = 0.6f + heartbeatStrong * 0.9f;

                Lighting.AddLight(NPC.Center, intensity * 1.0f, intensity * 0.25f, intensity * 0.5f);

                // 极低血量时光照范围扩大
                if (NPC.life < NPC.lifeMax * 0.15f)
                    Lighting.AddLight(NPC.Center, intensity * 1.5f, intensity * 0.4f, intensity * 0.7f);
            }

            // 冲刺状态强化光照
            if (CurrentState == State.Strike || CurrentState == State.ReturnStrike || CurrentState == State.Phase2Burst)
            {
                Lighting.AddLight(NPC.Center, 1.2f, 0.3f, 0.6f);
            }
        }

        // ===================================================================
        // ========== 自定义绘制 ==========
        // ===================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 第三眼：在本体上方绘制旋转的眼睛纹理
            if (thirdEyeOpen && CurrentState != State.Unconscious)
            {
                Texture2D eyeTexture = TextureAssets.Npc[NPCID.Creeper].Value;
                Vector2 eyeOffset = new Vector2(0, -28f + (float)Math.Sin(GlobalTimer * 0.12f) * 5f);
                Vector2 eyePos = NPC.Center - screenPos + eyeOffset;

                float eyeScale = 1.4f + (float)Math.Sin(GlobalTimer * 0.22f) * 0.2f;
                float eyeAlpha = 1f - NPC.alpha / 255f;

                // 外发光（多层叠绘）
                for (int layer = 3; layer >= 1; layer--)
                {
                    float glowScale = eyeScale + layer * 0.15f;
                    Color glowColor = EyeColor * (0.12f * (4 - layer)) * eyeAlpha;
                    spriteBatch.Draw(eyeTexture, eyePos, null, glowColor,
                        GlobalTimer * 0.06f, eyeTexture.Size() / 2f, glowScale, SpriteEffects.None, 0f);
                }

                // 眼核
                spriteBatch.Draw(eyeTexture, eyePos, null, EyeColor * eyeAlpha,
                    GlobalTimer * 0.06f, eyeTexture.Size() / 2f, eyeScale, SpriteEffects.None, 0f);
            }

            // 折返补刀时绘制残影
            if (CurrentState == State.ReturnStrike)
            {
                Texture2D npcTex = TextureAssets.Npc[NPC.type].Value;
                for (int i = 1; i <= 4; i++)
                {
                    Vector2 trailPos = NPC.Center - NPC.velocity * i * 0.5f - screenPos;
                    float trailAlpha = (5 - i) / 5f * 0.4f * (1f - NPC.alpha / 255f);
                    spriteBatch.Draw(npcTex, trailPos, NPC.frame, EyeColor * trailAlpha,
                        NPC.rotation, npcTex.Size() / 2f, NPC.scale * (1f - i * 0.06f), SpriteEffects.None, 0f);
                }
            }

            return true;
        }

        // ===================================================================
        // ========== 死亡 ==========
        // ===================================================================
        public override void OnKill()
        {
            // 爆裂粒子
            for (int i = 0; i < 100; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ghost,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f), 0, TrailColor, 1.8f);
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                var d = Dust.NewDustPerfect(NPC.Center, DustID.MagicMirror, vel, 0, EyeColor, 2.5f);
                Main.dust[d.dustIndex].noGravity = true;
            }

            // 第三眼死亡时额外效果
            if (thirdEyeOpen)
            {
                for (int i = 0; i < 60; i++)
                {
                    float angle = MathHelper.TwoPi / 60f * i;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 6f;
                    var d = Dust.NewDustPerfect(NPC.Center, DustID.GemRuby, vel, 0, EyeColor, 3f);
                    Main.dust[d.dustIndex].noGravity = true;
                }
                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 1f, Volume = 1.2f }, NPC.position);
            }

            DropLoot();
        }

        private void DropLoot()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.PlatinumCoin, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GoldCoin, Main.rand.Next(5, 15));

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SoulofNight, Main.rand.Next(6, 13));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SoulofLight, Main.rand.Next(3, 7));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Ectoplasm, Main.rand.Next(4, 9));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ShadowScale, Main.rand.Next(5, 11));

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.PinkGel, Main.rand.Next(10, 21));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Feather, Main.rand.Next(5, 11));

            if (Main.rand.NextBool(4)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.PsychoKnife, 1, false, -1);
            if (Main.rand.NextBool(7)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.LifeformAnalyzer, 1, false, -1);
            if (Main.rand.NextBool(9)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreenCap, 1, false, -1);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicMirror, 1, false, -1);
            if (Main.rand.NextBool(13)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.PocketMirror, 1, false, -1);
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RodofDiscord, 1, false, -1);

            if (thirdEyeOpen)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SoulofSight, Main.rand.Next(2, 5));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.BlackLens, Main.rand.Next(1, 3));
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.InvisibilityPotion, Main.rand.Next(3, 7));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RecallPotion, Main.rand.Next(3, 7));
            }
        }
    }
}