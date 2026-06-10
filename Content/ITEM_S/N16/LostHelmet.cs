using TEACHER.Content.ALL_NPC.M9;
using TEACHER.Content.ITEM_S.N0;
using TEACHER.Content.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N16
{
    [AutoloadEquip(EquipType.Head)]
    public class LostHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            // ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.defense = 40;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 3);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<KoishiHatPlayer>().koishiHatEquipped = true;
            player.AddBuff(BuffID.NightOwl, 2);
            player.AddBuff(BuffID.Shine, 2);
        }

        public override void AddRecipes()
        {
            
            // ========== 召唤物 → 头盔（反向拆解）==========
            Recipe reverse = CreateRecipe();
            reverse.AddIngredient(ModContent.ItemType<XLostHat>(), 1);
            reverse.AddIngredient(ModContent.ItemType<Humanity>(), 10);
            reverse.Register();

            // ========== 材料 → 头盔 ==========
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Ectoplasm, 55);
            recipe.AddIngredient(ItemID.ShadowScale, 50);
            recipe.AddIngredient(ItemID.SoulofNight, 35);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.Register();
        }
    }
}