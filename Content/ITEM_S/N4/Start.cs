using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N4
{
    public class Start : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;                    // 月后级别伤害
            Item.DamageType = DamageClass.Melee;
            Item.width = 63;
            Item.height = 64;
            Item.useTime = 12;                    // 攻速较快
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7;
            Item.value = Item.buyPrice(gold: 90); // 月后价值
            Item.rare = ItemRarityID.Red;         // 红色 = 月后稀有度
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;

            // 发射 BakaIce 弹幕
            Item.shoot = ModContent.ProjectileType<Ice>();
            Item.shootSpeed = 16f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 从剑尖射出，稍微分散角度
            float spread = 0.15f; // 约 8.5 度
            Vector2 spawnPos = position + Vector2.Normalize(velocity) * 40f;

            for (int i = -2; i <= 2; i++)
            {
                Vector2 perturbedSpeed = velocity.RotatedBy(spread * i);
                Projectile.NewProjectile(source, spawnPos, perturbedSpeed, type, damage/4, knockback, player.whoAmI);
            }
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LunarBar, 12);          // 夜明锭
            recipe.AddIngredient(ItemID.FrostCore, 2);            // 寒霜核（冰雪主题）
            recipe.AddIngredient(ItemID.IceBlock, 100);           // 冰雪块
            recipe.AddTile(TileID.LunarCraftingStation);          // 远古操纵机
            recipe.Register();
        }
    }
}