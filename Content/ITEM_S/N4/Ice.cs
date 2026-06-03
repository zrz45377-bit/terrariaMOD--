using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N4
{
    public class Ice : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.aiStyle = 0;           // 完全自定义 AI
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;         // 穿透 3 个敌人
            Projectile.timeLeft = 120;        // 5 秒
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;    // 水中不减速
            Projectile.tileCollide = true;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            // 1. 朝向飞行方向旋转
            Projectile.rotation += 0.20f;

            // 2. 自动追踪（Homing）
            float detectRange = 600f;         // 索敌范围
            float maxSpeed = 20f;             // 最大速度
            float turnFactor = 0.20f;         // 转向灵敏度（0~1，越大越灵敏）

            NPC target = null;
            float closest = detectRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(this))
                {
                    float dist = Projectile.Distance(npc.Center);
                    if (dist < closest)
                    {
                        closest = dist;
                        target = npc;
                    }
                }
            }

            if (target != null)
            {
                Vector2 desiredVelocity = target.Center - Projectile.Center;
                desiredVelocity.Normalize();
                desiredVelocity *= maxSpeed;

                // 平滑转向
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, turnFactor);
            }

            // 3. 冰尘拖尾
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(20, 20),
                    DustID.Ice,
                    Velocity: Projectile.velocity * 0.1f,
                    Scale: 1.5f
                );
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 120); // 霜冻（2秒，月后级别）
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, Scale: 1.5f);
            }
        }
    }
}