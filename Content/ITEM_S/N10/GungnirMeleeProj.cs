using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N10
{
    public class GungnirMeleeProj : ModProjectile
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.scale = 1.4f;
            Projectile.hide = true;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        private float MovementFactor
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private bool IsRetracting
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value ? 1f : 0f;
        }

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead || owner.noItems || owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            Vector2 playerCenter = owner.RotatedRelativePoint(owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] == 0f)
            {
                Vector2 toMouse = Main.MouseWorld - playerCenter;
                if (toMouse.LengthSquared() > 1f)
                    Projectile.velocity = toMouse.SafeNormalize(Vector2.UnitX);
                else
                    Projectile.velocity = Vector2.UnitX;

                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }

            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = owner.itemAnimation;

            if (owner.itemAnimation == 0)
            {
                Projectile.Kill();
                return;
            }

            if (!owner.frozen)
            {
                if (MovementFactor == 0f)
                {
                    MovementFactor = 3f;
                    Projectile.netUpdate = true;
                }

                int retractThreshold = Math.Max(owner.itemAnimationMax / 3, 1);

                if (owner.itemAnimation < retractThreshold)
                {
                    if (!IsRetracting)
                    {
                        IsRetracting = true;
                        Projectile.netUpdate = true;
                    }
                    MovementFactor = Math.Max(MovementFactor - 8f, 0f);
                }
                else
                {
                    MovementFactor += 8f;
                }
            }

            Projectile.Center = playerCenter + Projectile.velocity * MovementFactor;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);

            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            Lighting.AddLight(Projectile.Center, 0.8f, 0.1f, 0.1f);
            Projectile.netUpdate = true;
        }

        // ========== 【新增】吸血 ==========
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int healAmount = damageDone / 10;   // 10% 吸血
            if (healAmount > 0)
            {
                Player owner = Main.player[Projectile.owner];
                owner.statLife += healAmount;
                if (owner.statLife > owner.statLifeMax2)
                    owner.statLife = owner.statLifeMax2;
                owner.HealEffect(healAmount);
            }
        }
        // =================================

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() / 2f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.Red * 0.35f,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.05f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}