using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;          // ← 新增：用 TextureAssets
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N12.HitboxConfig_G
{
    /// <summary>
    /// 全局开关
    /// </summary>
    public static class HitboxConfig
    {
        public static bool ShowHitboxes = false;
        // 不再需要自己管理 Pixel 纹理！
    }

    /// <summary>
    /// 按键切换：按 H 开启/关闭（仅本地玩家）
    /// </summary>
    public class HitboxInput : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.gameMenu) return;

            if (Main.keyState.IsKeyDown(Keys.H) && !Main.oldKeyState.IsKeyDown(Keys.H))
            {
                HitboxConfig.ShowHitboxes = !HitboxConfig.ShowHitboxes;
                Main.NewText($"[判定点] {(HitboxConfig.ShowHitboxes ? "开启" : "关闭")}", Color.Yellow);
            }
        }
    }

    /// <summary>
    /// 给所有 NPC（敌人）画红色判定点
    /// </summary>
    public class HitboxNPC : GlobalNPC
    {
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!HitboxConfig.ShowHitboxes) return;

            Rectangle box = npc.Hitbox;
            Vector2 topLeft = box.TopLeft() - Main.screenPosition;
            int w = box.Width;
            int h = box.Height;

            // 用原版内置的 1x1 白色纹理，Color 参数负责染色
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, w, h), Color.Red * 0.15f);

            Color border = Color.Red * 0.9f;
            spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, w, 2), border);
            spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y + h - 2, w, 2), border);
            spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, 2, h), border);
            spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X + w - 2, (int)topLeft.Y, 2, h), border);
        }
    }

    /// <summary>
    /// 给所有 Projectile（弹幕/射弹）画黄色判定点
    /// </summary>
    public class HitboxProjectile : GlobalProjectile
    {
        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            if (!HitboxConfig.ShowHitboxes) return;

            Rectangle box = projectile.Hitbox;
            Vector2 topLeft = box.TopLeft() - Main.screenPosition;
            int w = box.Width;
            int h = box.Height;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color border = Color.Yellow * 0.9f;

            // GlobalProjectile.PostDraw 没有 spriteBatch 参数，用 Main.spriteBatch
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, w, 1), border);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y + h - 1, w, 1), border);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, 1, h), border);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X + w - 1, (int)topLeft.Y, 1, h), border);
        }
    }

    /// <summary>
    /// 给玩家画青色判定点
    /// </summary>
    public class HitboxPlayerLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Torso);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => HitboxConfig.ShowHitboxes;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Rectangle box = player.Hitbox;
            Vector2 topLeft = box.TopLeft() - Main.screenPosition;
            int w = box.Width;
            int h = box.Height;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color fill = Color.Cyan * 0.15f;
            Color border = Color.Cyan * 0.9f;

            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, w, h), fill);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, w, 2), border);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y + h - 2, w, 2), border);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, 2, h), border);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)topLeft.X + w - 2, (int)topLeft.Y, 2, h), border);
        }
    }
}