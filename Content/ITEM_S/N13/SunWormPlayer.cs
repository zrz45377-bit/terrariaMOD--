using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N13
{
    public class SunWormPlayer : ModPlayer
    {
        public bool sunWormHelmet;

        public override void ResetEffects()
        {
            sunWormHelmet = false;
        }

        public override void PostUpdate()
        {
            if (!sunWormHelmet) return;

            // 官方 Lighting 类确认存在的属性：全局亮度乘数
            // 默认值约 1.0f，往上调整个世界会变亮
            Lighting.GlobalBrightness = 1.5f;

            // 玩家自身超大范围强光源（R, G, B 都拉满）
            Lighting.AddLight(Player.Center, 2.0f, 2.0f, 2.0f);

            // 以下原版机制绝对稳定，组合起来就是最强照明
            Player.nightVision = true;              // 消除黑暗区域亮度衰减
            Player.AddBuff(BuffID.Shine, 2);        // 自身发光
            Player.AddBuff(BuffID.NightOwl, 2);     // 夜间视觉增强
            Player.AddBuff(BuffID.Spelunker, 2);    // 矿石高亮
            Player.AddBuff(BuffID.Hunter, 2);       // 敌人高亮
        }
    }
}