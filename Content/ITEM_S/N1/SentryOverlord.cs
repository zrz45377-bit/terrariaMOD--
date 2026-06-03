using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N1
{
    public class SentryOverlord : ModItem
    {


        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;      // 世纪之花级别
            Item.value = Item.sellPrice(0, 2, 0, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 世纪之花前水平：仆从+3 哨兵+2
            player.maxTurrets += 8;
            player.maxMinions += 9;

            // 召唤伤害大幅提升（世纪之花前版本：+50%）
            player.GetDamage(DamageClass.Summon) += 0.50f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.ChlorophyteBar, 8)
                .AddIngredient(ItemID.JungleSpores, 12)
                .AddIngredient(ItemID.Vine, 8)
                .AddIngredient(ItemID.Stinger, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}