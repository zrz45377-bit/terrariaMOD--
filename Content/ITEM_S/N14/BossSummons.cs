using Microsoft.Xna.Framework;
using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N14
{
    // ============================
    // 世纪之花召唤物
    // ============================
    public class PlanteraSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 32;
            Item.maxStack = 20;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Lime;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.UseSound = SoundID.Item44;
        }

        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(NPCID.Plantera))
            {
                Main.NewText("Plantera has already been summoned!", Color.Orange);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.Plantera);
                Main.NewText("Plantera has awoken!", Color.Pink);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Humanity>(), 3)  // 人性
                .AddIngredient(ItemID.Ectoplasm, 5)                 // 灵气
                .AddIngredient(ItemID.ChlorophyteBar, 5)            // 叶绿锭
                .Register();
        }
    }

    // ============================
    // 骷髅王召唤物
    // ============================
    public class SkeletronSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 20;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Green;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.UseSound = SoundID.Item44;
        }

        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(NPCID.SkeletronHead))
            {
                Main.NewText("Skeletron is already here!", Color.Gray);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.SkeletronHead);
                Main.NewText("Skeletron has awoken!", Color.Gray);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Humanity>(), 2)  // 人性
                .AddIngredient(ItemID.Ectoplasm, 3)                 // 灵气
                .AddIngredient(ItemID.Bone, 30)                     // 骨头
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }

    // ============================
    // 血肉墙召唤物
    // ============================
    public class WallOfFleshSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 22;
            Item.maxStack = 20;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.UseSound = SoundID.Item44;
        }

        public override bool CanUseItem(Player player)
        {
            // 血肉墙保留地狱环境限制（向导玩偶主题）
            if (player.ZoneUnderworldHeight)
            {
                if (NPC.AnyNPCs(NPCID.WallofFlesh))
                {
                    Main.NewText("The Wall of Flesh is already summoned!", Color.Red);
                    return false;
                }
                return true;
            }
            Main.NewText("This item only works in the Underworld...", Color.Red);
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.WallofFlesh);
                Main.NewText("Wall of Flesh has awoken!", Color.Red);
                player.AddBuff(BuffID.Horrified, 36000);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Humanity>(), 3)  // 人性
                .AddIngredient(ItemID.Ectoplasm, 5)                 // 灵气
                .AddIngredient(ItemID.HellstoneBar, 10)             // 狱石锭
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }

    
}