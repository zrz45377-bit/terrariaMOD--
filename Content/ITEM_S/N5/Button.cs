using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N5
{
    public class Button : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.hostile = false;

            // 核心：不会移动、不会掉落
            Projectile.tileCollide = false;      // 不碰方块
            Projectile.ignoreWater = true;       // 忽略水阻力
            Projectile.timeLeft = 20;            // 60帧 = 1秒后消失

            // 如果需要完全静止不动
            Projectile.aiStyle = -1;             // 禁用原版AI，自己控制
            Projectile.penetrate = -1;           // 不因为命中怪物而消失
        }

        public override void AI()
        {
            // 完全禁止任何速度变化，保持生成时的位置
            Projectile.velocity = Vector2.Zero;

            // 可选：添加淡入淡出或缩放效果
            float progress = Projectile.timeLeft / 60f;
            Projectile.alpha = (int)(255 * (1f - progress)); // 逐渐消失

            // 可选：生成粒子效果
            if (Projectile.timeLeft % 5 == 0)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // 消失时的特效
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
            }
        }
    }
}