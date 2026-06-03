using Microsoft.Xna.Framework;
using System;
using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N5
{
    public class Gun : ModItem
    {
        // 0 = 快速模式，1 = 狙击模式
        public int mode = 0;
        int hurts = 50;

        public override void SetDefaults()
        {
            Item.damage = 200*hurts;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 64;
            Item.height = 24;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(gold: 35);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item11;       // 快速模式音效
            Item.autoReuse = true;

            Item.useAmmo = AmmoID.Bullet;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 16f;
        }

        // 根据当前模式实时调整武器属性
        public override void HoldItem(Player player)
        {
            if (mode == 0) // 快速
            {
                Item.useTime = 4;
                Item.useAnimation = 4;
                Item.autoReuse = true;
                Item.UseSound = SoundID.Item11;
                Item.knockBack = 2f;
                Item.shootSpeed = 16f;
            }
            else // 狙击
            {
                Item.useTime = 85;
                Item.useAnimation = 85;
                Item.autoReuse = false;
                Item.UseSound = SoundID.Item40;   // 狙击枪音效
                Item.knockBack = 7f;
                Item.shootSpeed = 1f;
            }
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // 右键 = 切换模式，不射击
            if (player.altFunctionUse == 2)
            {
                mode = mode == 0 ? 1 : 0;

                // 切换音效 + 文字提示
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
                string text = mode == 0 ? "速射模式" : "狙击模式";
                CombatText.NewText(player.getRect(), mode == 0 ? Color.Orange : Color.Cyan, text, true);

                // 切换粒子特效
                for (int i = 0; i < 8; i++)
                {
                    Vector2 speed = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 2f;
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.MagicMirror, speed, 100, mode == 0 ? Color.Orange : Color.Cyan, 1.2f);
                    d.noGravity = true;
                }

                return false;
            }

            return base.CanUseItem(player);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (mode == 1) // 狙击模式
            {
                position = Main.MouseWorld;
                velocity = Vector2.Zero;
                type = ModContent.ProjectileType<Button>();
                // 狙击保持 5000 高伤，不削弱
            }
            else // 快速模式
            {
                damage = Math.Max(1, damage / hurts);
                // type / position / velocity 保持弹药默认
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (mode == 1) // 狙击：手动生成 Button + 瞄准线
            {
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

                Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
                float dist = toMouse.Length();
                Vector2 dir = toMouse.SafeNormalize(Vector2.UnitX);

                for (float i = 0; i < dist; i += 6f)
                {
                    Dust d = Dust.NewDustPerfect(player.MountedCenter + dir * i, DustID.RedTorch, Vector2.Zero, 100, default, 1f);
                    d.noGravity = true;
                }

                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDustPerfect(Main.MouseWorld, DustID.PurpleTorch, dir.RotatedByRandom(MathHelper.PiOver2) * 4f);
                }

                return false;
            }

            // 快速模式：返回 true，让原版根据当前弹药自动生成对应弹幕
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SniperRifle, 1).AddIngredient(ModContent.ItemType<Humanity>(), 3);
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ItemID.FragmentVortex, 10);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }
}