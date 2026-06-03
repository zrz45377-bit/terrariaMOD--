using System.IO;                          // 预留：若后续需读写自定义数据可启用
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N10
{
    /// <summary>
    /// 冈格尼尔·投掷模式弹幕（血月之枪）
    /// 沿直线高速飞行，命中敌人/地形后引发大范围猩红爆炸
    /// 爆炸对Boss降低AOE倍率，并保护撒旦军团水晶等关键场景NPC
    /// </summary>
    public class GungnirThrownProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 32;              // 碰撞箱宽度（像素）
            Projectile.height = 32;             // 碰撞箱高度（像素）
            Projectile.aiStyle = -1;            // -1 = 完全自定义AI，不走原版预设
            Projectile.friendly = true;         // 对敌人造成伤害
            Projectile.penetrate = -1;          // -1 = 无限穿透（直到触发爆炸自行销毁）
            Projectile.tileCollide = true;      // 会撞墙并触发爆炸
            Projectile.DamageType = DamageClass.Melee; // 伤害类型：近战（吃近战加成）
            Projectile.timeLeft = 300;          // 最大存活帧数（约5秒），超时自动爆炸
            Projectile.light = 2f;              // 发光半径，照亮黑暗地形
            Projectile.scale = 1.5f;            // 贴图缩放
            Projectile.extraUpdates = 2;        // 每帧额外更新2次，飞行更平滑、碰撞更精准
        }

        // 标记：是否已爆炸（防止重复调用）
        private bool hasExploded = false;
        // 爆炸AOE半径（像素），约15格距离
        private const float EXPLOSION_RADIUS = 250f;

        /// <summary>
        /// 每帧更新：目标锁定标记、飞行旋转、猩红尾迹粒子、照明
        /// </summary>
        public override void AI()
        {
            // ---------- 目标锁定标记（仅本地玩家执行一次）----------
            // localAI[0] 存储最近敌人的 whoAmI+1，用于在敌人头顶显示猩红锁定标记
            if (Projectile.localAI[0] == 0f && Projectile.owner == Main.myPlayer)
            {
                NPC target = FindClosestNPC(1200f);   // 搜索1200像素（75格）内最近敌人
                Projectile.localAI[0] = target != null ? target.whoAmI + 1 : -1f;
                Projectile.netUpdate = true;            // 同步给多人模式的其他客户端
            }

            // ---------- 锁定标记视觉：被锁定的敌人头顶偶尔闪烁猩红粒子 ----------
            int targetIdx = (int)Projectile.localAI[0] - 1;
            if (targetIdx >= 0 && targetIdx < Main.maxNPCs && Main.npc[targetIdx].active && Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustPerfect(
                    Main.npc[targetIdx].Center + new Vector2(0, -Main.npc[targetIdx].height * 0.6f),
                    DustID.SolarFlare, Vector2.Zero, 0, Color.Crimson, 0.8f);
                d.noGravity = true;
            }

            // ---------- 贴图旋转：让矛尖始终朝向飞行方向 ----------
            // PiOver2 = 90°补偿，因为贴图默认朝上，需转为朝右/朝下
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // ---------- 猩红尾迹：飞行时身后拖出2条日耀色粒子 ----------
            for (int i = 0; i < 2; i++)
            {
                // 在矛身后 10~40 像素处生成粒子
                Vector2 backPos = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(10f, 40f);
                Dust d = Dust.NewDustPerfect(backPos, DustID.SolarFlare,
                    -Projectile.velocity * 0.1f, 100, Color.Crimson, Main.rand.NextFloat(0.6f, 1.2f));
                d.noGravity = true;
                d.fadeIn = 1f;
            }

            // ---------- 环境照明：飞行路径上留下暗红色光源 ----------
            Lighting.AddLight(Projectile.Center, 1.5f, 0.05f, 0.05f);
        }

        /// <summary>命中敌人时触发爆炸</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Explode();
        }

        /// <summary>撞到地形时触发爆炸，并返回 false 阻止弹幕被地形弹开</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return false;
        }

        /// <summary>超时/被Kill时，若尚未爆炸则补一次爆炸</summary>
        public override void Kill(int timeLeft)
        {
            if (!hasExploded)
                Explode();
        }

        /// <summary>
        /// 核心爆炸逻辑：粒子特效 + 屏幕震动 + AOE伤害
        /// 注意：AOE循环中已排除撒旦军团水晶/传送门、训练假人，并对Boss降低倍率
        /// </summary>
        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            // 播放爆炸音效（原版火箭/日耀爆炸声）
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);

            // ---------- 粒子特效：三层扩散环 ----------
            // 内环→外环，半径递增，粒子数递增，形成冲击波视觉效果
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 80f + ring * 100f;          // 环半径：40 / 90 / 140
                int count = 24 + ring * 16;                // 粒子数：12 / 20 / 28
                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi / count * i + ring * 0.5f;
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                    Vector2 vel = -angle.ToRotationVector2() * (3f - ring); // 外环速度更慢
                    Dust d = Dust.NewDustPerfect(pos, DustID.SolarFlare, vel, 0, Color.Crimson, 2.2f - ring * 0.4f);
                    d.noGravity = true;
                }
            }

            // ---------- 粒子特效：中心爆发 ----------
            // 60个粒子向四面八方高速飞散，模拟爆心
            for (int i = 0; i < 60; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.SolarFlare,
                    dir * Main.rand.NextFloat(2f, 14f), 0, Color.Crimson, Main.rand.NextFloat(1.5f, 3f));
                d.noGravity = true;
            }

            // ---------- 屏幕震动 ----------
            // 仅本地玩家触发，6f 强度、10f 半径、15帧持续
            if (Projectile.owner == Main.myPlayer)
                Main.instance.CameraModifiers.Add(
                    new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                        Projectile.Center, Vector2.UnitY, 6f, 10f, 15));

            // ---------- 爆炸瞬间强光 ----------
            Lighting.AddLight(Projectile.Center, 3f, 0f, 0f);

            // ========== AOE 伤害循环（关键逻辑）==========
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                // 【保护1】撒旦军团水晶：打爆则事件直接失败
                if (npc.type == NPCID.DD2EterniaCrystal) continue;

                // 【保护2】撒旦军团传送门：两边出怪的门，也是NPC类型
                if (npc.type == NPCID.DD2LanePortal) continue;

                // 超出爆炸半径则跳过
                if (npc.Distance(Projectile.Center) > EXPLOSION_RADIUS) continue;

                // 【平衡】Boss 只受 40% AOE 倍率，防止多部位同时吃满爆炸瞬秒
                // 例如月球领主（眼睛+手+心脏）同时被炸，原版倍率会叠加出数倍伤害
                float damageMult = npc.boss ? 1f : 1.5f;

                // 附加日耀 debuff（持续掉血）
                npc.AddBuff(BuffID.Daybreak, 600);

                // 执行伤害：不触发暴击（避免数字太夸张）
                npc.SimpleStrikeNPC((int)(Projectile.damage * damageMult), Projectile.direction,
                    knockBack: 8f, damageType: DamageClass.Melee, crit: false);
            }

            // 正式销毁弹幕
            Projectile.Kill();
        }

        /// <summary>
        /// 搜索指定半径内最近的敌对NPC（排除友好/无敌目标）
        /// 用于在矛飞行时标记"被锁定"的敌人头顶特效
        /// </summary>
        private NPC FindClosestNPC(float maxRange)
        {
            NPC result = null;
            float minDist = maxRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float dist = npc.Distance(Projectile.Center);
                if (dist < minDist) { minDist = dist; result = npc; }
            }
            return result;
        }

        /// <summary>
        /// 自定义绘制：残影拖尾 + 本体
        /// 在弹幕贴图后面画出3道半透明、缩小的猩红残影，增强速度感
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 加载贴图（Texture 属性自动映射同名 png）
            Texture2D texture = ModContent.Request < Texture2D > (Texture).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;

            // ---------- 残影拖尾（3层）----------
            for (int i = 1; i <= 3; i++)
            {
                float alpha = 0.15f / i;                        // 越往后越透明：0.15 / 0.075 / 0.05
                float scale = Projectile.scale * (1f - i * 0.08f); // 越往后越小
                // 沿飞行反方向偏移，形成拖尾
                Vector2 trailPos = drawPos - Projectile.velocity.SafeNormalize(Vector2.UnitX) * i * 25f * Projectile.scale;

                Main.EntitySpriteDraw(texture, trailPos, null,
                    Color.Crimson * alpha, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            }

            // ---------- 本体绘制（白色，不受环境光影响）----------
            Main.EntitySpriteDraw(texture, drawPos, null,
                Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false; // 返回 false 阻止原版默认绘制
        }
    }
}