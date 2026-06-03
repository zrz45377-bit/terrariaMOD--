using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N3
{
    public class IbukiGourd : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 3, 0, 0);
            Item.rare = ItemRarityID.Pink;          // 史莱姆皇后 / 困难模式早期
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 防御降低
            player.statDefense -= 15;

            // 暴击率 +20%
            player.GetCritChance(DamageClass.Generic) += 20;

            // 近战伤害 +100%（史莱姆皇后级别的狂战士加成）
            player.GetDamage(DamageClass.Melee) += 1.0f;

            // 其他伤害大幅降低
            player.GetDamage(DamageClass.Ranged) -= 0.60f;
            player.GetDamage(DamageClass.Magic) -= 0.60f;
            player.GetDamage(DamageClass.Summon) -= 0.60f;

            // 速度变得难以控制
            player.moveSpeed += 0.3f;
            player.maxRunSpeed += 1.0f;
            player.accRunSpeed += 1.0f;
            player.runAcceleration += 0.05f;

            // 醉酒摇晃：幅度更小，频率更低
            if (Main.rand.NextBool(6))
            {
                player.velocity.X += Main.rand.NextFloat(-0.6f, 0.6f);
            }

            // 限制最大速度
            if (player.velocity.X > 10f)
                player.velocity.X = 10f;
            if (player.velocity.X < -10f)
                player.velocity.X = -10f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HallowedBar, 12)        // 神圣锭（史莱姆皇后掉落）
                .AddIngredient(ItemID.Gel, 50)              // 凝胶（史莱姆主题）
                .AddIngredient(ItemID.CrystalShard, 15)     // 水晶碎块（神圣之地）
                .AddIngredient(ItemID.Ale, 10)              // 麦芽酒（酒葫芦设定）
                .AddTile(TileID.MythrilAnvil)               // 秘银砧/山铜砧
                .Register();
        }
    }
}