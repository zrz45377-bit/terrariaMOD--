using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.Items
{
    public class YinYangOrb : ModProjectile
    {
        // ========== 弹幕基础属性 ==========
        public override void SetDefaults()
        {
            Projectile.width = 120;          // 碰撞箱宽度
            Projectile.height = 120;         // 碰撞箱高度（和贴图尺寸一致）
            Projectile.aiStyle = -1;        // -1 = 不使用原版AI，完全自己写

            Projectile.friendly = true;     // true = 可以命中敌人（但这里只用来触发穿透，不触发爆炸）
            Projectile.hostile = true;     // false = 不会通过原版机制伤害玩家

            Projectile.penetrate = -1;      // -1 = 无限穿透，命中敌人后不会消失
            Projectile.timeLeft = 20 * 60;  // 20秒 × 60帧 = 1200帧，时间到自动进入 Kill()

            Projectile.tileCollide = true;  // true = 开启地形碰撞，用于反弹
            Projectile.ignoreWater = false; // false = 在水中会受到阻力/减速
        }

        // ========== 每帧逻辑 ==========
        public override void AI()
        {
            // --- 1. 轻微重力 ---
            // 每帧给Y轴速度加一点下落，形成弧线飞行
            Projectile.velocity.Y += 0.12f;
            // 限制最大下落速度，防止越飞越快变成流星
            if (Projectile.velocity.Y > 10f)
                Projectile.velocity.Y = 10f;

            // --- 2. 持续旋转 ---
            // 根据飞行方向决定旋转方向（正转/反转）
            // Math.Sign: 正数返回1，负数返回-1，0返回0
            Projectile.rotation += 0.15f * Math.Sign(Projectile.velocity.X);

            // --- 3. 拖尾粒子 ---
            // NextBool(3) = 每3帧平均触发1次（33%概率）
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6, 6), // 在弹幕中心周围随机位置生成
                    DustID.PurificationPowder,    // 粒子类型：净化粉（白色）
                    -Projectile.velocity * 0.2f,  // 初速度：和弹幕反向，形成拖尾感
                    100,                          // 透明度
                    Color.White,                  // 颜色
                    1.1f                          // 缩放
                );
                d.noGravity = true;               // 粒子不受重力，飘在空中
            }

            // --- 4. 最后3秒预警 ---
            // 180帧 = 3秒，进入倒计时阶段
            if (Projectile.timeLeft < 180)
            {
                // 加速旋转，视觉上提示"要炸了"
                Projectile.rotation += 0.3f;

                // 随机生成红色火花
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.Torch,             // 火把粒子（偏红/橙）
                        Vector2.Zero,             // 初速度为0，原地闪烁
                        100,
                        Color.Red,
                        1.2f
                    );
                    d.noGravity = true;
                }
            }
        }

        // ========== 碰到物块时 ==========
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 这里 oldVelocity 是碰撞前一帧的速度
            // 通过比较碰撞前后速度差，判断撞的是水平面还是垂直面

            // --- 水平碰撞反弹（左右墙壁）---
            // 如果X轴速度变化超过0.1，说明撞到了左右墙
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > 0.1f)
            {
                // 反转X速度，并乘0.85让每次反弹稍微衰减（模拟能量损失）
                Projectile.velocity.X = -oldVelocity.X * 0.85f;
            }

            // --- 垂直碰撞反弹（地面/天花板）---
            // 如果Y轴速度变化超过0.1，说明撞到了上下地面
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.1f)
            {
                Projectile.velocity.Y = -oldVelocity.Y * 0.85f;
            }

            // 反弹音效（玻璃/水晶碰撞声）
            SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            // 反弹时生成白色粒子
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PurificationPowder,
                    Main.rand.NextVector2Circular(2, 2), // 随机小速度向四周散开
                    100,
                    Color.White,
                    1.0f
                );
                d.noGravity = true;
            }

            // 返回 false = 告诉引擎"不要杀死这个弹幕"，让它继续飞行
            return false;
        }

        // ========== 时间到 / 需要销毁时 ==========
        [Obsolete]
        public override void Kill(int timeLeft)
        {
            // 20秒倒计时结束，或者在其他地方调用 Projectile.Kill() 时触发
            Explode();
        }

        // ========== 爆炸核心逻辑 ==========
        private void Explode()
        {
            Vector2 center = Projectile.Center;   // 爆炸中心点

            // --- 4. 爆炸视觉 & 音效 ---
            // 播放爆炸音效
            SoundEngine.PlaySound(SoundID.Item14, center);

            // 白色放射状粒子（爆炸核心）
            for (int i = 0; i < 50; i++)
            {
                // NextVector2CircularEdge = 在圆形边缘随机取点（方向均匀）
                // 速度范围 4~10
                Vector2 speed = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 10f);

                Dust d = Dust.NewDustPerfect(center, DustID.PurificationPowder, speed, 100, Color.White, 2.5f);
                d.noGravity = true; // 不受重力，呈放射状飞散
            }

            // 灰色烟雾粒子（爆炸外围）
            for (int i = 0; i < 25; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(3f, 3f); // 随机方向，速度较慢

                Dust d = Dust.NewDustPerfect(center, DustID.Smoke, speed, 100, Color.Gray, 1.8f);
                d.noGravity = false; // 受重力，会往下飘，像烟雾
            }
        }

        // ========== 自定义绘制 ==========
        public override bool PreDraw(ref Color lightColor)
        {
            // 获取当前弹幕的贴图
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;

            // 计算绘制位置：世界坐标 → 屏幕坐标
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // 贴图中心点，用于正确旋转
            Vector2 origin = tex.Size() / 2f;

            // 执行绘制
            Main.EntitySpriteDraw(
                tex,
                pos,
                null,               // 不裁剪，画整张贴图
                lightColor,         // 接受环境光照颜色
                Projectile.rotation,// 当前旋转角度
                origin,
                Projectile.scale,   // 缩放
                SpriteEffects.None, // 不翻转
                0                   // 层级
            );

            // 返回 false = 阻止原版默认绘制（避免双重绘制）
            return false;
        }
    }
}