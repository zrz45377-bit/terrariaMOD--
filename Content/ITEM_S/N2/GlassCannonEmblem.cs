using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N2
{
    public class GlassCannonEmblem : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.value = Item.sellPrice(1, 0, 0, 0);  // 1 铂金币，灾厄后售价
            Item.rare = ItemRarityID.Purple;           // 紫色（灾厄后/毕业级）
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {

            // 3. 暴击率 +100%（满暴击）
            player.GetCritChance(DamageClass.Generic) += 900;

            // 4. 魔法伤害 +2000%（Additive +20.0，灾厄级膨胀）
            player.GetDamage(DamageClass.Magic) += 20.0f;
            // 近战伤害 +100%（史莱姆皇后级别的狂战士加成）
            player.GetDamage(DamageClass.Melee) += 10.0f;

            // 其他伤害大幅降低
            player.GetDamage(DamageClass.Ranged) += 5.60f;
            player.GetDamage(DamageClass.Magic) += 5.60f;
            player.GetDamage(DamageClass.Summon) += 5.60f;

        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Humanity>(), 199)  // 人性
                .AddIngredient(ItemID.LunarBar, 1999)          // 夜明锭×999（灾厄后材料消耗）
                .AddIngredient(ItemID.SorcererEmblem, 1)      // 巫师徽章
                .AddTile(TileID.LunarCraftingStation)          // 远古操纵机
                .Register();
        }
    }
}