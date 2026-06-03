using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M2
{
    [AutoloadBossHead]
    public class TorchGodsFavor : ModNPC
    {
        // 7种颜色主题循环
        private int ThemeCount => 7;
        private int CurrentTheme => (int)AttackIndex; // 0~6

        // AI槽位
        float Timer { get => NPC.ai[0]; set => NPC.ai[0] = value; }
        float AttackIndex { get => NPC.ai[1]; set => NPC.ai[1] = value; }
        float AttackTimer { get => NPC.ai[2]; set => NPC.ai[2] = value; }
        float ThemeSwitchCD { get => NPC.ai[3]; set => NPC.ai[3] = value; }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1; // 单帧贴图
            NPCID.Sets.MPAllowedEnemies[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 100;
            NPC.height = 100;
            NPC.damage = 55;
            NPC.defense = 18;
            NPC.lifeMax = 90000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.value = Item.buyPrice(gold: 12);
            NPC.npcSlots = 15f;
            Music = MusicID.Boss2;
        }

        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];
            if (!player.active || player.dead)
            {
                NPC.velocity.Y -= 0.25f;
                if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                return;
            }

            Timer++;

            // 悬浮跟随
            Hover(player);

            // 背景氛围随主题变色
            UpdateBackgroundTheme();

            // 攻击调度：每180帧（3秒）切换一次主题
            AttackTimer++;
            int attackDuration = 180;
            if (AttackTimer >= attackDuration)
            {
                AttackTimer = 0;
                AttackIndex = (AttackIndex + 1) % ThemeCount;

                // 切换时爆发对应颜色粒子
                SpawnThemeBurst(GetThemeColor(CurrentTheme), 40);
                SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // 执行当前主题攻击
            switch (CurrentTheme)
            {
                case 0: Attack_Red(player); break;
                case 1: Attack_Yellow(player); break;
                case 2: Attack_Green(player); break;
                case 3: Attack_Pink(player); break;
                case 4: Attack_LightBlue(player); break;
                case 5: Attack_Cyan(player); break;
                case 6: Attack_Purple(player); break;
            }
        }

        // ══════════════════════════════════════════════
        //  移动
        // ══════════════════════════════════════════════
        private void Hover(Player player)
        {
            Vector2 target = player.Center + new Vector2(0, -220);
            float maxSpeed = 6f + (CurrentTheme * 0.5f); // 越后期越快
            NPC.velocity = Vector2.Lerp(NPC.velocity,
                (target - NPC.Center).SafeNormalize(Vector2.Zero) * maxSpeed, 0.06f);
        }

        // ══════════════════════════════════════════════
        //  背景氛围：光照 + 环境粒子
        // ══════════════════════════════════════════════
        private void UpdateBackgroundTheme()
        {
            Color c = GetThemeColor(CurrentTheme);
            // Boss中心强光（照亮周围）
            Lighting.AddLight(NPC.Center, c.R / 255f * 1.5f, c.G / 255f * 1.5f, c.B / 255f * 1.5f);

            // 周围飘散同色粒子
            if (Timer % 4 == 0)
            {
                Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(180f, 180f);
                int d = Dust.NewDust(pos, 6, 6, DustID.Torch);
                Main.dust[d].color = c;
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = Main.rand.NextFloat(1.2f, 2.2f);
                Main.dust[d].fadeIn = 0.5f;
            }
        }

        private Color GetThemeColor(int theme)
        {
            return theme switch
            {
                0 => Color.Red,
                1 => Color.Gold,
                2 => Color.LimeGreen,
                3 => Color.HotPink,
                4 => Color.DeepSkyBlue,
                5 => Color.Aquamarine,
                _ => Color.MediumPurple,
            };
        }

        private void SpawnThemeBurst(Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Main.dust[d].color = color;
                Main.dust[d].scale = Main.rand.NextFloat(2f, 3.5f);
                Main.dust[d].noGravity = true;
            }
        }

        // ══════════════════════════════════════════════
        //  主题0：红色 - 火焰与鲜血
        // ══════════════════════════════════════════════
        private void Attack_Red(Player player)
        {
            if (AttackTimer % 12 != 0) return;

            // 3向散射：火球 + 血弹 + 大火球
            for (int i = -1; i <= 1; i++)
            {
                float angle = (player.Center - NPC.Center).ToRotation() + i * 0.25f;
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 8f;

                int projType = i switch
                {
                    -1 => ProjectileID.BallofFire,
                    0 => ProjectileID.Fireball,
                    _ => ProjectileID.BloodNautilusShot, // 红色血弹，如果没有请换 ProjectileID.BloodArrow
                };

                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    projType, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  主题1：黄色 - 光束与圣金
        // ══════════════════════════════════════════════
        private void Attack_Yellow(Player player)
        {
            // 激光束直射（EyeBeam）
            if (AttackTimer == 30 || AttackTimer == 90)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.EyeBeam, NPC.damage / 2, 2f, Main.myPlayer);
            }

            // 扇形金雨
            if (AttackTimer % 15 == 0 && AttackTimer < 120)
            {
                for (int i = -2; i <= 2; i++)
                {
                    float angle = (player.Center - NPC.Center).ToRotation() + i * 0.18f;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 9f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.GoldenShowerHostile, NPC.damage / 2, 2f, Main.myPlayer);
                }
            }

            // Betsy火球收尾
            if (AttackTimer == 150)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 10f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.DD2BetsyFireball, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  主题2：绿色 - 诅咒与剧毒
        // ══════════════════════════════════════════════
        private void Attack_Green(Player player)
        {
            // 诅咒火焰追踪弹
            if (AttackTimer % 10 == 0 && AttackTimer < 100)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 7f;
                vel = vel.RotatedBy(Main.rand.NextFloat(-0.3f, 0.3f));
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.CursedFlameHostile, NPC.damage / 2, 2f, Main.myPlayer);
            }

            // 毒种散射（Plantera风格）
            if (AttackTimer % 20 == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = MathHelper.TwoPi * i / 5f + (AttackTimer * 0.05f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 6f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.PoisonSeedPlantera, NPC.damage / 2, 2f, Main.myPlayer);
                }
            }

            // 松针（PineNeedle）快速连射
            if (AttackTimer % 8 == 0 && AttackTimer > 60 && AttackTimer < 140)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 11f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.PineNeedleHostile, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  主题3：粉色 - 星云与史莱姆
        // ══════════════════════════════════════════════
        private void Attack_Pink(Player player)
        {
            // 星云弹追踪
            if (AttackTimer % 12 == 0 && AttackTimer < 120)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.NebulaBolt, NPC.damage / 2, 2f, Main.myPlayer);
            }

            // 史莱姆尖刺（粉色）
            if (AttackTimer % 18 == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = (player.Center - NPC.Center).ToRotation() + (i - 1) * 0.4f;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 9f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.QueenSlimeMinionBlueSpike, NPC.damage / 2, 2f, Main.myPlayer);
                }
            }

            // 星云球（大范围）
            if (AttackTimer == 90)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi * i / 6f;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 5f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.NebulaSphere, NPC.damage / 2, 2f, Main.myPlayer);
                }
            }

            // Plantera种子（粉色拖尾）
            if (AttackTimer % 25 == 0)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 7f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.SeedPlantera, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  主题4：淡蓝 - 冰霜与激光
        // ══════════════════════════════════════════════
        private void Attack_LightBlue(Player player)
        {
            // 冰霜碎片散射
            if (AttackTimer % 10 == 0 && AttackTimer < 100)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = MathHelper.TwoPi * i / 4f + AttackTimer * 0.08f;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 7f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.FrostShard, NPC.damage / 2, 2f, Main.myPlayer);
                }
            }

            // 火星炮塔激光直射（MartianTurretBolt）
            if (AttackTimer == 40 || AttackTimer == 100 || AttackTimer == 160)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 14f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.MartianTurretBolt, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  主题5：青色 - 幻影与幽灵
        // ══════════════════════════════════════════════
        private void Attack_Cyan(Player player)
        {
            // 幻影眼追踪（PhantasmalEye）
            if (AttackTimer % 20 == 0 && AttackTimer < 140)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 9f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.PhantasmalEye, NPC.damage / 2, 2f, Main.myPlayer);
            }

            // 幻影箭快速连射（PhantasmalBolt）
            if (AttackTimer % 6 == 0 && AttackTimer > 30 && AttackTimer < 150)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
                vel = vel.RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f));
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.PhantasmalBolt, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  主题6：紫色 - 拜月与诅咒
        // ══════════════════════════════════════════════
        private void Attack_Purple(Player player)
        {
            // 拜月教克隆火球（CultistBossFireBallClone）
            if (AttackTimer % 15 == 0 && AttackTimer < 120)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.CultistBossFireBallClone, NPC.damage / 2, 2f, Main.myPlayer);
            }

            // 远古厄运追踪弹（AncientDoomProjectile）
            if (AttackTimer == 60 || AttackTimer == 120)
            {
                for (int i = -1; i <= 1; i++)
                {
                    float angle = (player.Center - NPC.Center).ToRotation() + i * 0.5f;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 6f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ProjectileID.AncientDoomProjectile, NPC.damage / 2, 2f, Main.myPlayer);
                }
            }

            // 沙漠神灯诅咒弹（DesertDjinnCurse）
            if (AttackTimer % 25 == 0)
            {
                Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 7f;
                vel = vel.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f));
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ProjectileID.DesertDjinnCurse, NPC.damage / 2, 2f, Main.myPlayer);
            }
        }

        // ══════════════════════════════════════════════
        //  掉落
        // ══════════════════════════════════════════════
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.TorchGodsFavor, 1));
            npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 8, 15));
        }


        // ========== 图鉴注册 ==========
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement(
                    "自地心初燃之时便守护火源秩序的古神。凡人以千百支火把将地下化为白昼，此举被其视为对黑暗本身的亵渎。当第101支火把点亮，古神便会凝聚形体，以七色净火重塑地底的平衡。"
                    )
            });
        }
    }
}