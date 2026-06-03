using Microsoft.Xna.Framework;
using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N15
{
    // ==================== 计数器 ====================
    /// <summary>
    /// 挂在玩家身上的蓄力计数器。
    /// </summary>
    public class ShinkiPlayer : ModPlayer
    {
        public int ChargeCount = 0;
    }

    // ==================== 物品本体 ====================
    /// <summary>
    /// 神绮魔导书。
    /// 蓄力中：发射 NebulaBlaze1
    /// 第150次：在玩家周围500像素范围内释放 NebulaArcanum，重置计数
    /// </summary>
    public class ShinkiTome : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 1;
            Item.width = 80;
            Item.height = 80;
            Item.useTime = 5;
            Item.useAnimation = 5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 3;
            Item.value = Item.buyPrice(gold: 15);
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ProjectileID.NebulaBlaze1;  // 默认弹幕，Shoot()里会覆盖
            Item.shootSpeed = 8f;
            Item.noMelee = true;
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
                                   Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            ShinkiPlayer mp = player.GetModPlayer<ShinkiPlayer>();
            mp.ChargeCount++;

            if (mp.ChargeCount >= 150)
            {
                // ===== 第150次：释放 NebulaArcanum =====
                // 在玩家周围500像素随机位置生成12枚星云秘术球
                for (int i = 0; i < 40; i++)
                {
                    // 随机角度，随机半径100~500像素
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(200f, 600f);
                    Vector2 spawnPos = player.Center + new Vector2(dist, 0).RotatedBy(angle);

                    // 朝鼠标方向发射，带20度随机偏移
                    Vector2 toMouse = (Main.MouseWorld - spawnPos).SafeNormalize(Vector2.UnitY);
                    toMouse = toMouse.RotatedByRandom(MathHelper.ToRadians(20f));

                    Projectile.NewProjectile(source, spawnPos, toMouse * 0.1f,
                        ProjectileID.NebulaArcanum, damage, knockback, player.whoAmI);
                }

                mp.ChargeCount = 0;
            }
            else
            {
                // ===== 蓄力中：发射 NebulaBlaze1 =====
                Projectile.NewProjectile(source, position, 
                    new Vector2(velocity.X+ Main.rand.NextFloat(-1f, 1f), velocity.Y+ Main.rand.NextFloat(-1f, 1f)),
                    ProjectileID.NebulaBlaze1, damage, knockback, player.whoAmI);
            }

            // 返回 false：告诉游戏"我已经手动生成弹幕了，不需要再自动生成"
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe()
            .AddIngredient(ModContent.ItemType<Humanity>(), 50)
            .AddIngredient(ItemID.FragmentNebula, 18);     // 星云碎片
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }
}