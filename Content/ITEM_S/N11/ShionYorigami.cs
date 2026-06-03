using Microsoft.Xna.Framework;
using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N11
{
    public class ShionYorigami : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.value = Item.sellPrice(copper: 1);   // 只值1铜币
            Item.rare = ItemRarityID.Red;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item2;
            Item.buffType = ModContent.BuffType < ShionYorigamiBuff > ();
            Item.shoot = ModContent.ProjectileType < ShionYorigamiPet > ();
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Humanity>(), 3)  // 人性
                .AddIngredient(ItemID.CopperCoin, 1)      // 贫穷神只值1铜币
                .AddIngredient(ItemID.Silk, 15)           // 破布缝成护符
                .AddIngredient(ItemID.RottenChunk, 5)     // 腐肉，代表不幸与衰败
                .AddIngredient(ItemID.SoulofNight, 5)     
                .AddTile(TileID.Loom)                     // 在织布机缝制
                .Register();

            // 腐化/猩红双版本（自动根据世界选择，或者两个都注册让玩家自己选）
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Humanity>(), 3)  // 人性
                .AddIngredient(ItemID.CopperCoin, 1)
                .AddIngredient(ItemID.Silk, 15)
                .AddIngredient(ItemID.Vertebrae, 5)       // 猩红版本
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}