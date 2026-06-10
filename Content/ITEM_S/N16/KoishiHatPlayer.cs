using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N16
{
    public class KoishiHatPlayer : ModPlayer
    {
        public bool koishiHatEquipped = false;

        public override void ResetEffects()
        {
            koishiHatEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (!koishiHatEquipped)
                return;

            // ========== 无意识核心效果 ==========

            // 大幅降低敌怪仇恨范围（符合"被遗忘"设定）
            Player.aggro -= 400;

            // 移速加成（轻盈/飘忽感）
            Player.moveSpeed += 0.25f;
            Player.runAcceleration += 0.05f;

            // 持续产生无意识粒子
            if (Main.GameUpdateCount % 8 == 0)
            {
                Vector2 pos = Player.Center + Main.rand.NextVector2Circular(32f, 48f);
                Dust d = Dust.NewDustPerfect(pos, DustID.PinkTorch, Vector2.Zero, 0, Color.HotPink, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0.5f;
            }

            // 低血量时触发"第三只眼睁开"效果（参考 Boss 的 thirdEyeOpen 机制）
            if (Player.statLife < Player.statLifeMax2 * 0.35f)
            {
                Player.AddBuff(BuffID.Heartreach, 2);   // 心形拾取范围扩大
                Player.AddBuff(BuffID.Panic, 2);        // 恐慌！额外移速
                Player.AddBuff(BuffID.Hunter, 2);       // 猎人药水（感知）
            }


        }
    }
}