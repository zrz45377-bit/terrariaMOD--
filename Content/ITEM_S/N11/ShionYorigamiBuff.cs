using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N11
{
    public class ShionYorigamiBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 刷怪率已移到 GlobalNPC.EditSpawnRate，这里只管宠物
            int projType = ModContent.ProjectileType < ShionYorigamiPet > ();
            if (player.whoAmI == Main.myPlayer
                && player.ownedProjectileCounts[projType] <= 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_Buff(buffIndex),
                    player.Center,
                    Vector2.Zero,
                    projType,
                    0,
                    0f,
                    player.whoAmI
                );
            }
        }
    }
}