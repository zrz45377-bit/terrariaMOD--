using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.Boss.XProjectile
{
    /// <summary>
    /// 【东方弹幕基类】
    /// 普通弹幕只需继承此类，在 SetDefaults 里改贴图/大小/伤害即可。
    /// 已封装：直线运动、旋转对齐、生成/消失渐变、发光拖尾、碰撞音效。
    /// 特殊弹幕重写 AI() 或 PreDraw()。
    /// </summary>
    internal abstract class BaseAmulet : ModProjectile
    {
        // ---------- 可覆盖属性 ----------
        /// <summary>弹幕存在时间（帧），默认 300 = 5秒</summary>
        protected virtual int LifeTime => 300;

        /// <summary>是否自动旋转朝向速度方向（默认 true）</summary>
        protected virtual bool RotateToVelocity => true;

        /// <summary>是否发光（默认 true，带红色光晕）</summary>
        protected virtual bool Glow => true;

        /// <summary>发光颜色（默认淡红）</summary>
        protected virtual Color GlowColor => new Color(255, 80, 80, 180);

        /// <summary>拖尾长度（0 = 无拖尾）</summary>
        protected virtual int TrailLength => 1;

        /// <summary>是否受重力影响（默认 false）</summary>
        protected virtual bool HasGravity => false;

        /// <summary>是否穿透敌人（默认 false，击中就消失）</summary>
        protected virtual bool Penetrate => false;

        /// <summary>穿透数量（Penetrate=true 时有效）</summary>
        protected virtual int PenetrateCount => 1;

        /// <summary>是否贴墙反弹（默认 false）</summary>
        protected virtual bool Bounce => false;

        // ---------- 初始化 ----------
        public override void SetDefaults()
        {
            // 碰撞箱（子类覆盖）
            Projectile.width = 16;
            Projectile.height = 16;

            // 基础属性
            Projectile.timeLeft = LifeTime;
            Projectile.friendly = false;   // 对玩家有害（敌人弹幕）
            Projectile.hostile = true;     // 是敌对弹幕
            Projectile.tileCollide = !Bounce; // 不贴墙反弹就穿墙
            Projectile.ignoreWater = true;

            // 穿透
            Projectile.penetrate = Penetrate ? PenetrateCount : -1;

            // 重力
            Projectile.aiStyle = -1;     // 完全自定义
        }

        // ---------- 主 AI ----------
        public override void AI()
        {
            // 1. 重力（可选）
            //if (HasGravity)
            //{
            //    Projectile.velocity.Y += 0.15f;
            //}

            // 2. 旋转对齐速度（御币/符札朝飞行方向）
            if (RotateToVelocity && Projectile.velocity.Length() > 0.1f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            // 3. 生成/消失 透明渐变（前 15 帧淡入，最后 15 帧淡出）
            if (Projectile.timeLeft > LifeTime - 15)
            {

            }
            else if (Projectile.timeLeft < 15)
            {
                // 快消失：淡出
                float fadeOut = Projectile.timeLeft / 15f;
                Projectile.alpha = (int)(255 * (1f - fadeOut));
            }
            else
            {
                Projectile.alpha = 0; // 完全可见
            }

            // 4. 基础拖尾粒子（可选）
            //if (TrailLength > 0 && Projectile.timeLeft % 3 == 0)
            //{
            //    Dust d = Dust.NewDustPerfect(
            //        Projectile.Center,
            //        DustID.PinkTorch,       // 默认粉色拖尾，子类可改
            //        -Projectile.velocity * 0.1f,
            //        150,
            //        GlowColor,
            //        0.8f
            //    );
            //    d.noGravity = true;
            //}

            // 5. 速度衰减（可选，子类可覆盖）
            // Projectile.velocity *= 0.998f;

            // 6. 调用子类额外 AI
            ExtraAI();
        }

        /// <summary>子类可覆盖此方法添加额外运动逻辑（追踪、摇摆、分裂等）</summary>
        protected virtual void ExtraAI() { }

        // ---------- 碰撞 ----------
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // 击中玩家时：生成红色粒子，弹幕消失（如果不穿透）
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, -Projectile.velocity.X * 0.5f, -Projectile.velocity.Y * 0.5f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Bounce)
            {
                // 反弹逻辑
                if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                    Projectile.velocity.Y = -oldVelocity.Y;
                return false; // 不杀死弹幕
            }

            // 撞墙死亡粒子
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone, oldVelocity.X * 0.3f, oldVelocity.Y * 0.3f);
            }
            return true; // 杀死弹幕
        }

        // ---------- 消失 ----------
        public override void Kill(int timeLeft)
        {
            // 自然消失时的小爆炸粒子
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, speed, 150, GlowColor, 1.0f);
                d.noGravity = true;
            }
        }

        // ---------- 绘制 ----------
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origin = frame.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // 1. 发光拖尾（多层半透明贴图叠加）
            //if (Glow && TrailLength > 0)
            //{
            //    for (int i = TrailLength - 1; i > 0; i--)
            //    {
            //        float progress = i / (float)TrailLength;
            //        Color trailColor = GlowColor * progress * 0.4f;
            //        trailColor.A = 0;

            //        // 注意：简单实现，不存历史位置，用速度反推
            //        Vector2 trailPos = pos - Projectile.velocity * i * 0.5f;
            //        sb.Draw(tex, trailPos, frame, trailColor, Projectile.rotation, origin, Projectile.scale * (0.8f + progress * 0.2f), SpriteEffects.None, 0f);
            //    }
            //}

            // 2. 主贴图
            Color drawColor = Projectile.GetAlpha(lightColor);
            sb.Draw(tex, pos, frame, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            // 3. 外层光晕（Additive 混合）

            return false; // 已手动绘制，阻止默认绘制
        }
    }
}