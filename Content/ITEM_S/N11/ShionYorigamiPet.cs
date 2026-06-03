using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N11
{
    public class ShionYorigamiPet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 33;
            Projectile.height = 42;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!player.active || player.dead
                || !player.HasBuff(ModContent.BuffType < ShionYorigamiBuff > ()))
            {
                Projectile.Kill();
                return;
            }

            // === 特效①：正弦波悬浮，让飘动更自然 ===
            float hoverOffset = (float)Math.Sin(Main.GameUpdateCount * 0.05f) * 6f;

            float speed = 8f;
            float inertia = 40f;
            Vector2 idlePosition = player.Center
                + new Vector2(-40 * player.direction, -40 + hoverOffset);
            Vector2 toIdle = idlePosition - Projectile.Center;
            float dist = toIdle.Length();

            if (dist > 600f)
            {
                Projectile.Center = idlePosition;
                Projectile.velocity *= 0.1f;
            }
            else if (dist > 10f)
            {
                toIdle.Normalize();
                toIdle *= speed;
                Projectile.velocity = (Projectile.velocity * (inertia - 1) + toIdle)
                    / inertia;
            }
            else
            {
                Projectile.velocity *= 0.9f;
            }

            // === 特效②：根据移动速度轻微倾斜（披风摆动感）===
            Projectile.rotation = Projectile.velocity.X * 0.04f;
            Projectile.spriteDirection = player.direction;
            Projectile.timeLeft = 2;

            // === 特效③：蓝色披风拖尾 ===
            if (Main.rand.NextBool(3))
            {
                Vector2 dustPos = Projectile.Bottom + new Vector2(
                    Main.rand.Next(-8, 8),
                    Main.rand.Next(-4, 4)
                );
                int dust = Dust.NewDust(
                    dustPos,
                    4, 4,
                    DustID.BlueTorch,
                    Projectile.velocity.X * 0.1f,
                    Projectile.velocity.Y * 0.1f + 0.3f,
                    100,
                    default,
                    0.7f
                );
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 0.4f;
            }

            // === 特效④：霉运灰雾（偶尔向外扩散）===
            if (Main.rand.NextBool(20))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustSpeed = Main.rand.NextVector2Circular(0.8f, 0.8f);
                    int dust = Dust.NewDust(
                        Projectile.Center,
                        16, 16,
                        DustID.Smoke,
                        dustSpeed.X,
                        dustSpeed.Y,
                        120,
                        new Color(120, 140, 160), // 灰蓝色
                        0.8f
                    );
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 0.2f;
                }
            }

            // === 特效⑤：虚幻金币（贫穷神对财富的渴望，出现即消失）===
            if (Main.rand.NextBool(90))
            {
                Vector2 coinPos = Projectile.Top + new Vector2(Main.rand.Next(-12, 12), -8);
                int dust = Dust.NewDust(
                    coinPos,
                    6, 6,
                    DustID.GoldCoin,
                    0f,
                    -0.5f,
                    150,
                    default,
                    0.9f
                );
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].fadeIn = 0.3f;
            }

            // === 特效⑥：极微弱的蓝色环境光 ===
            Lighting.AddLight(Projectile.Center, 0.08f, 0.12f, 0.20f);
        }
    }
}