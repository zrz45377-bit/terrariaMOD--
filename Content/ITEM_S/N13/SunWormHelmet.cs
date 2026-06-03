using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N13
{
    [AutoloadEquip(EquipType.Head)]  // ← 关键！自动注册 _Head.png 为头部装备
    public class SunWormHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 神圣头盔是全覆盖头部，不显示头发（默认就是不显示，这里可以不写）
            // 如果想显示帽子发型可以取消注释下面这行：
            // ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.defense = 20;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<SunWormPlayer>().sunWormHelmet = true;

            player.nightVision = true;
            player.AddBuff(BuffID.Shine, 2);
            player.AddBuff(BuffID.NightOwl, 2);
            player.AddBuff(BuffID.Spelunker, 2);
            player.AddBuff(BuffID.Hunter, 2);
        }

        public override void AddRecipes() { }
    }
}