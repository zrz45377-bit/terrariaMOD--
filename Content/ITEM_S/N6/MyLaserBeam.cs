using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N6
{
    public class MyLaserBeam : ModProjectile
    {
        private const float MAX_DISTANCE = 16384f;
        private const float MOVE_DISTANCE = 60f;
        private const int MAX_CHARGE = 60;

        public float Distance
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float Charge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public float RuneRotation
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        public float LaserWidth => MathHelper.Lerp(14f, 72f, Charge / MAX_CHARGE);
        public float DamageMult => MathHelper.Lerp(0.3f, 1.5f, Charge / MAX_CHARGE);
        private float ChargeRatio => MathHelper.Clamp(Charge / MAX_CHARGE, 0f, 1f);

        // ========== 高级流光色板（幽紫→御金→月白→桃粉） ==========
        private static readonly Color[] FlowPalette = new Color[]
        {
            new Color(160, 80, 255),   // 0 幽紫
            new Color(255, 200, 80),   // 1 御金
            new Color(200, 240, 255),  // 2 月白
            new Color(255, 120, 180),  // 3 桃粉
        };

        /// <summary>
        /// 在色板之间平滑流动，t 随时间递增即可
        /// </summary>
        private Color FlowColor(float t)
        {
            t = MathF.Abs(t) % 4f;
            int idx = (int)t;
            float lerp = t - idx;
            Color from = FlowPalette[idx % 4];
            Color to = FlowPalette[(idx + 1) % 4];
            return Color.Lerp(from, to, lerp);
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 unit = Projectile.velocity;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                player.Center + unit * MOVE_DISTANCE,
                player.Center + unit * Distance,
                LaserWidth,
                ref point
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request < Texture2D > (Texture).Value;
            Player player = Main.player[Projectile.owner];
            Vector2 start = player.Center + Projectile.velocity * MOVE_DISTANCE;
            Vector2 unit = Projectile.velocity;
            float rotation = unit.ToRotation() - MathHelper.PiOver2;

            const int segmentHeight = 26;
            const int segmentWidth = 28;
            Vector2 centerOrigin = new Vector2(segmentWidth / 2f, segmentHeight / 2f);
            Vector2 topOrigin = new Vector2(segmentWidth / 2f, 0f);

            float widthScale = LaserWidth / segmentWidth;
            float t = (float)Main.GlobalTimeWrappedHourly;
            float flowT = t * 0.35f; // 整体变色速度（慢一些更高级）

            // 当前时间的主色调
            Color currentFlow = FlowColor(flowT);
            Color nextFlow = FlowColor(flowT + 1f);

            // ================================================================
            // 第一层：外侧光晕（最宽，用主色调极淡晕染）
            // ================================================================
            for (int i = 0; i < 2; i++)
            {
                float outerScale = widthScale * (2.8f - i * 0.9f);
                Color outerGlow = Color.Lerp(currentFlow, nextFlow, i * 0.3f) * (0.06f + i * 0.03f);
                Main.EntitySpriteDraw(
                    texture,
                    start - Main.screenPosition,
                    new Rectangle(0, segmentHeight, segmentWidth, segmentHeight),
                    outerGlow,
                    rotation,
                    topOrigin,
                    new Vector2(outerScale, Distance / segmentHeight),
                    SpriteEffects.None,
                    0
                );
            }

            // ================================================================
            // 第二层：激光主体（保留紫→金长度渐变，再叠加上时间流光）
            // ================================================================

            // 2a. 激光头
            Color headColor = Color.Lerp(new Color(160, 80, 255), currentFlow, 0.4f);
            Main.EntitySpriteDraw(
                texture,
                start - Main.screenPosition,
                new Rectangle(0, 0, segmentWidth, segmentHeight),
                headColor,
                rotation,
                centerOrigin,
                new Vector2(widthScale, 1f),
                SpriteEffects.None,
                0
            );

            // 2b. 激光身体
            for (float d = MOVE_DISTANCE + segmentHeight; d < Distance; d += segmentHeight)
            {
                Vector2 pos = player.Center + unit * d;
                float ratio = d / Distance;

                // 基础：沿长度从紫到金（保留原本好看的东方结构）
                Color baseColor = Color.Lerp(
                    new Color(160, 80, 255),
                    new Color(255, 210, 80),
                    ratio
                );

                // 叠加：随时间缓慢流动的淡彩（只叠加 35%，不会盖住原结构）
                Color flow = FlowColor(flowT + ratio * 0.8f);
                Color segColor = Color.Lerp(baseColor, flow, 0.35f);

                float pulse = 0.88f + 0.12f * MathF.Sin(t * 3f + d * 0.02f);
                Main.EntitySpriteDraw(
                    texture,
                    pos - Main.screenPosition,
                    new Rectangle(0, segmentHeight, segmentWidth, segmentHeight),
                    segColor * pulse,
                    rotation,
                    centerOrigin,
                    new Vector2(widthScale, 1f),
                    SpriteEffects.None,
                    0
                );
            }

            // 2c. 激光尾
            Vector2 endPos = player.Center + unit * Distance;
            Color tailColor = Color.Lerp(new Color(255, 210, 80), FlowColor(flowT + 0.5f), 0.3f);
            Main.EntitySpriteDraw(
                texture,
                endPos - Main.screenPosition,
                new Rectangle(0, segmentHeight * 2, segmentWidth, segmentHeight),
                tailColor,
                rotation,
                centerOrigin,
                new Vector2(widthScale, 1f),
                SpriteEffects.None,
                0
            );

            // ================================================================
            // 第三层：内核白光（带微量当前流光色，让核心也有呼吸感）
            // ================================================================
            Color coreTint = Color.Lerp(Color.White, currentFlow, 0.15f) * (0.35f + ChargeRatio * 0.25f);
            Main.EntitySpriteDraw(
                texture,
                start - Main.screenPosition,
                new Rectangle(0, segmentHeight, segmentWidth, segmentHeight),
                coreTint,
                rotation,
                topOrigin,
                new Vector2(widthScale * 0.3f, Distance / segmentHeight),
                SpriteEffects.None,
                0
            );

            // ================================================================
            // 第四层：枪口法阵（彩虹旋转，但用 FlowColor）
            // ================================================================
            DrawRuneCircle(player.Center + unit * (MOVE_DISTANCE * 0.5f), rotation, flowT);

            // ================================================================
            // 第五层：末端冲击光晕（满蓄力时呼吸光圈）
            // ================================================================
            if (ChargeRatio > 0.8f)
            {
                float breathe = 1f + 0.12f * MathF.Sin(t * 6f);
                float impactScale = widthScale * 2.5f * breathe * ((ChargeRatio - 0.8f) / 0.2f);
                Color impactColor = currentFlow * 0.35f;
                Main.EntitySpriteDraw(
                    texture,
                    endPos - Main.screenPosition,
                    new Rectangle(0, 0, segmentWidth, segmentHeight),
                    impactColor,
                    rotation + t * 1.5f,
                    centerOrigin,
                    new Vector2(impactScale, impactScale),
                    SpriteEffects.None,
                    0
                );
            }

            return false;
        }

        // ================================================================
        // 枪口法阵：同心圆 + 八角符文线（流光色粒子）
        // ================================================================
        private void DrawRuneCircle(Vector2 center, float baseRotation, float flowT)
        {
            if (ChargeRatio < 0.1f) return;

            float alpha = MathHelper.Clamp((ChargeRatio - 0.1f) / 0.9f, 0f, 1f);
            float radius = MathHelper.Lerp(20f, 55f, ChargeRatio);
            int points = 8;

            for (int i = 0; i < points; i++)
            {
                float angle = RuneRotation + MathHelper.TwoPi * i / points;
                Vector2 runePos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

                if (Main.rand.NextBool(3))
                {
                    Color c = FlowColor(flowT + i * 0.12f);
                    Dust rd = Dust.NewDustPerfect(runePos, DustID.GoldCoin, Vector2.Zero, 150, c, 0.6f * alpha);
                    rd.noGravity = true;
                    rd.velocity = Vector2.Zero;
                }

                Vector2 nextPos = center + new Vector2(
                    MathF.Cos(RuneRotation + MathHelper.TwoPi * ((i + 1) % points) / points),
                    MathF.Sin(RuneRotation + MathHelper.TwoPi * ((i + 1) % points) / points)
                ) * radius;

                if (Main.rand.NextBool(4))
                {
                    Vector2 midPoint = Vector2.Lerp(runePos, nextPos, Main.rand.NextFloat());
                    Color c = FlowColor(flowT + i * 0.12f + 0.5f);
                    Dust ld = Dust.NewDustPerfect(midPoint, DustID.GoldCoin, Vector2.Zero, 150, c, 0.5f * alpha);
                    ld.noGravity = true;
                    ld.velocity = Vector2.Zero;
                }
            }

            int innerPoints = 16;
            float innerRadius = radius * 0.45f;
            for (int i = 0; i < innerPoints; i++)
            {
                float angle = -RuneRotation + MathHelper.TwoPi * i / innerPoints;
                Vector2 innerPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * innerRadius;
                if (Main.rand.NextBool(5))
                {
                    Color c = FlowColor(flowT + i * 0.06f);
                    Dust id = Dust.NewDustPerfect(innerPos, DustID.GoldCoin, Vector2.Zero, 150, c, 0.4f * alpha);
                    id.noGravity = true;
                    id.velocity = Vector2.Zero;
                }
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Vector2 mousePos = Main.MouseWorld;

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 diff = mousePos - player.Center;
                diff.Normalize();
                Projectile.velocity = diff;
                Projectile.direction = (Main.MouseWorld.X > player.position.X) ? 1 : -1;
                Projectile.netUpdate = true;
            }

            Projectile.position = player.Center + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;

            int dir = Projectile.direction;
            player.ChangeDir(dir);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = (float)Math.Atan2(
                Projectile.velocity.Y * dir,
                Projectile.velocity.X * dir
            );

            RuneRotation += MathHelper.Lerp(0.02f, 0.08f, ChargeRatio);

            if (player.channel && Charge < MAX_CHARGE)
            {
                Charge++;

                if (Main.rand.NextBool(2))
                {
                    float runeAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float runeR = MathHelper.Lerp(15f, 50f, ChargeRatio);
                    Vector2 starPos = player.Center
                        + Projectile.velocity * (MOVE_DISTANCE * 0.5f)
                        + new Vector2(MathF.Cos(runeAngle), MathF.Sin(runeAngle)) * runeR;
                    Vector2 starVel = Projectile.velocity * Main.rand.NextFloat(0.5f, 2f);

                    float flowT = (float)Main.GlobalTimeWrappedHourly * 0.35f;
                    Color dustColor = FlowColor(flowT + Main.rand.NextFloat(0.3f));
                    Dust d = Dust.NewDustPerfect(starPos, DustID.GoldCoin, starVel, 0, dustColor, 0.9f);
                    d.noGravity = true;
                }
            }

            if (!player.channel && Charge < 15f)
            {
                Projectile.Kill();
                return;
            }

            if (!player.channel)
            {
                Projectile.Kill();
                return;
            }
            if (!player.CheckMana(player.inventory[player.selectedItem].mana, true))
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = (int)(player.inventory[player.selectedItem].damage * DamageMult);

            for (Distance = MOVE_DISTANCE; Distance <= MAX_DISTANCE; Distance += 5f)
            {
                Vector2 checkPos = player.Center + Projectile.velocity * Distance;
                if (!Collision.CanHit(player.Center, 1, 1, checkPos, 1, 1))
                {
                    Distance -= 5f;
                    break;
                }
            }

            // 末端粒子：流光色
            Vector2 dustPos = player.Center + Projectile.velocity * Distance;
            float ft = (float)Main.GlobalTimeWrappedHourly * 0.35f;
            for (int i = 0; i < 3; i++)
            {
                float angle = Projectile.velocity.ToRotation() + Main.rand.NextFloat(-0.5f, 0.5f);
                float speed = Main.rand.NextFloat(2f, 6f);
                Vector2 dustVel = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

                Color dustColor = FlowColor(ft + i * 0.2f);
                Dust dust = Dust.NewDustPerfect(dustPos, DustID.GoldCoin, dustVel, 0, dustColor, 1.5f);
                dust.noGravity = true;
            }

            if (Charge >= MAX_CHARGE && Main.rand.NextBool(10))
            {
                Main.instance.CameraModifiers.Add(
                    new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                        player.Center,
                        Main.rand.NextVector2CircularEdge(1f, 1f),
                        0.08f,
                        4f,
                        6,
                        800f,
                        "MiniHakkero"
                    )
                );
            }

            // 光照：随时间变化的流光色（柔和版，降低亮度避免过曝）
            Color lightColor = FlowColor(ft);
            DelegateMethods.v3_1 = lightColor.ToVector3() * 0.7f;
            Utils.PlotTileLine(
                Projectile.Center,
                Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE),
                LaserWidth,
                DelegateMethods.CastLight
            );
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = Projectile.velocity;
            Utils.PlotTileLine(
                Projectile.Center,
                Projectile.Center + unit * Distance,
                LaserWidth,
                DelegateMethods.CutTiles
            );
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 6;
            float ft = (float)Main.GlobalTimeWrappedHourly * 0.35f;
            for (int i = 0; i < 6; i++)
            {
                float angle = MathHelper.TwoPi * i / 6f;
                Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Main.rand.NextFloat(3f, 6f);
                Color dustColor = FlowColor(ft + i / 6f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldCoin, vel, 0, dustColor, 1.4f);
                d.noGravity = true;
            }
        }
    }
}