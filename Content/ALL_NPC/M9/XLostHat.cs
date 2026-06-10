using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TEACHER.Content.ITEM_S.N16;  // ← 引用头盔所在命名空间

namespace TEACHER.Content.ALL_NPC.M9
{
    public class XLostHat : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item44;
        }

        public override bool CanUseItem(Player player)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<LostHat>());
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Roar, player.position);
                int type = ModContent.NPCType<LostHat>();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.SpawnOnPlayer(player.whoAmI, type);
                }
                else
                {
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            // ========== 头盔 → 召唤物 ==========
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<LostHelmet>(), 1);
            recipe.AddTile(TileID.DemonAltar);                // 恶魔/血腥祭坛
            recipe.Register();

           
        }
    }
}