using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N0
{
    public class Humanity : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 物品显示名称

            // ========== 垂直动画设置 ==========
            // 贴图尺寸 18×112，按 7 帧垂直排列，每帧 18×16（112 ÷ 16 = 7）
            // 每 6 游戏刻(ticks)切换一帧，数值越小动画越快
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 4));

            // 灵魂物品特效：无重力浮动、图标脉冲发光、像灵魂一样上下飘动
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            ItemID.Sets.ItemIconPulse[Type] = true;
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;          // 单帧宽度（与贴图宽一致）
            Item.height = 16;         // 单帧高度（必须和贴图每帧高度一致）
            Item.maxStack = 999;      // 最大堆叠数
            Item.value = Item.sellPrice(0, 0, 10, 0); // 卖出价格 10 银
            Item.rare = ItemRarityID.Blue; // 稀有度：蓝色
        }
    }
}