using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M2
{
    public class TorchGodsFavor_item : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 20;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Green;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.UseSound = SoundID.Item44;
        }

        public override bool CanUseItem(Player player)
        {
            if (!player.ZoneDirtLayerHeight && !player.ZoneRockLayerHeight)
            {
                Main.NewText("必须在地下深处才能唤醒古神……", new Color(255, 80, 0));
                return false;
            }

            if (NPC.AnyNPCs(ModContent.NPCType<TorchGodsFavor>()))
            {
                Main.NewText("火把古神已经苏醒了！", new Color(255, 80, 0));
                return false;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<TorchGodsFavor>());
            }
            else
            {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<TorchGodsFavor>());
            }

            for (int i = 0; i < 20; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f;
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 4f;
                Dust d = Dust.NewDustPerfect(player.Center, DustID.Torch, vel, 0, Color.OrangeRed, 2f);
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            return true;
        }

        public override void AddRecipes()
        {
            // ── 配方A：集齐10种环境火把，每种20个 ──
            CreateRecipe()
                .AddIngredient(ItemID.IceTorch, 20)         // 冰雪火把
                .AddIngredient(ItemID.DesertTorch, 20)      // 沙漠火把
                .AddIngredient(ItemID.JungleTorch, 20)      // 丛林火把
                .AddIngredient(ItemID.CorruptTorch, 20)     // 腐化火把
                .AddIngredient(ItemID.CrimsonTorch, 20)     // 猩红火把
                .AddIngredient(ItemID.HallowedTorch, 20)    // 神圣火把
                .AddIngredient(ItemID.DemonTorch, 20)       // 恶魔火把（地狱）
                .AddIngredient(ItemID.CursedTorch, 20)      // 诅咒火把
                .AddIngredient(ItemID.IchorTorch, 20)       // 灵液火把
                .AddIngredient(ItemID.BoneTorch, 20)        // 骨头火把
                .AddTile(TileID.MythrilAnvil)               // 秘银砧
                .Register();

            // ── 配方B：已有火把神的恩宠时，直接复制一份 ──
            // 相当于用原版恩赐作为"图纸"，加上材料复制出召唤物
            CreateRecipe()
                .AddIngredient(ItemID.TorchGodsFavor, 1)    // 火把神的恩宠（不消耗，作为图纸）
                .AddIngredient(ItemID.Torch, 101)           // 普通火把
                .AddIngredient(ItemID.SoulofLight, 5)       // 光明之魂
                .AddIngredient(ItemID.SoulofNight, 5)       // 黑暗之魂
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}