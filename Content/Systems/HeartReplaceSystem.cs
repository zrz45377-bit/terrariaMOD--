using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace TEACHER.Content.Systems
{
    public class HeartReplaceSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            try
            {
                string way = "TEACHER";
                // 基础红心（选人界面 + 没吃生命果的角色）
                TextureAssets.Heart = ModContent.Request<Texture2D>(
                    way + "/Content/Textures/CustomHeart",
                    AssetRequestMode.ImmediateLoad
                );

                // 金心（吃过生命果后的角色 HUD）
                TextureAssets.Heart2 = ModContent.Request<Texture2D>(
                    way + "/Content/Textures/CustomHeart2",
                    AssetRequestMode.ImmediateLoad
                );

                // ========== 新增：魔力星 ==========
                TextureAssets.Mana = ModContent.Request<Texture2D>(
                    way + "/Content/Textures/CustomMana",
                    AssetRequestMode.ImmediateLoad
                );
            }
            catch (Exception e){ 
                
            }
        }
    }
}