using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N9
{
    [AutoloadEquip(EquipType.Wings)]
    public class StrangeHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(18000, 9f, 2.5f);
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 24;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.accessory = true;
            Item.defense = 0;

        }

        public override void UpdateEquip(Player player)
        {
            // ========== 移动加速 ==========
            player.runAcceleration += 1.2f;
            player.accRunSpeed += 4f;
            player.maxRunSpeed += 4f;
            player.moveSpeed += 0.35f;
            player.noFallDmg = true;

            // ========== 飞行逻辑 ==========
            if (!player.mount.Active)
            {
                bool onGround = player.velocity.Y == 0 && player.slideDir == 0
                && !player.justJumped;
                bool inAir = !onGround || player.wingTime > 0;

                // 地面起飞
                if (onGround && player.controlJump && player.releaseJump)
                {
                    player.velocity.Y = -6f;
                    player.wingTime = 18000;
                    player.jump = 0; // 防止普通跳跃叠加
                }

                if (inAir)
                {
                    // 按住跳跃：维持飞行 + 上升
                    if (player.controlJump)
                    {
                        player.wingTime = 18000;

                        player.velocity.Y -= 0.6f;
                        if (player.velocity.Y < -9f) player.velocity.Y = -9f;

                        // 空中水平加速
                        if (player.controlLeft) player.velocity.X -= 0.4f;
                        if (player.controlRight) player.velocity.X += 0.4f;

                        if (player.velocity.X > 12f) player.velocity.X = 12f;
                        if (player.velocity.X < -12f) player.velocity.X = -12f;

                        // 风の粒子（飞行时）
                        if (Main.rand.NextBool(3))
                        {
                            Dust d = Dust.NewDustPerfect(
                                player.Center + new Vector2(Main.rand.Next(-10, 10), 14),
                                DustID.Cloud,
                                new Vector2(-player.velocity.X * 0.15f, 0.8f),
                                150, Color.White, Main.rand.NextFloat(0.5f, 1.0f));
                            d.noGravity = true;
                        }
                    }
                    // 松开跳跃：缓降
                    else if (player.velocity.Y > 0)
                    {
                        player.velocity.Y *= 0.99f;
                    }
                }
            }

            // ========== 常驻粒子：风の尾迹 ==========
            if (Main.rand.NextBool(12))
            {
                Dust d = Dust.NewDustPerfect(
                    player.Center + new Vector2(Main.rand.Next(-14, 14), Main.rand.Next(-20, 5)),
                    DustID.Cloud,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -0.4f),
                    180, Color.WhiteSmoke, Main.rand.NextFloat(0.3f, 0.7f));
                d.noGravity = true;
            }

            // ========== 快门声 + 闪光（文文相机） ==========
            if (Main.rand.NextBool(600) && player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Camera, player.Center);

                // 白色闪光粒子
                for (int i = 0; i < 8; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        player.Center + new Vector2(0, -24),
                        DustID.Electric,
                        Main.rand.NextVector2Circular(3f, 3f),
                        0, Color.White, 1.0f);
                    d.noGravity = true;
                }
            }
        }

        public override void AddRecipes()
        {
            // 配方预留
        }
    }
}