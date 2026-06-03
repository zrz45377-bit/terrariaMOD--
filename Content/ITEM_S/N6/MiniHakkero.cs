using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N6
{
    public class MiniHakkero : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 2250;
            Item.DamageType = DamageClass.Magic;
            Item.width = 44;
            Item.height = 44;
            Item.useTime = 1;           // 每帧触发（配合 channel 实现持续发射）
            Item.useAnimation = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 8;
            Item.value = Item.sellPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item15;
            Item.autoReuse = true;
            Item.channel = true;        // 长按持续发射（核心）
            Item.shoot = ModContent.ProjectileType<MyLaserBeam>();
            Item.shootSpeed = 16f;
            Item.noMelee = true;
            Item.mana = 1;              // 持续耗蓝（每 useTime tick 扣一次）
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // 安全 Normalize（避免零向量爆炸）
            if (velocity != Vector2.Zero)
            {
                Vector2 muzzleOffset = Vector2.Normalize(velocity) * 55f;
                if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
                    position += muzzleOffset;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LastPrism)             // 最终棱镜（魔炮原型）
                .AddIngredient(ItemID.StarCloak)           // 星星斗篷（星符收集器）
                .AddIngredient(ItemID.CrystalBall)           // 水晶球（八卦占卜）
                .AddIngredient(ItemID.BookofSkulls)          // 骷髅之书（魔法书基底）
                .AddIngredient(ItemID.LunarBar, 20)
                .AddIngredient(ItemID.FallenStar, 77)      // 77颗星星（魔理沙的幸运数字梗）
                .AddIngredient(ItemID.SoulofSight, 20)
                .Register();
        }
    }
}