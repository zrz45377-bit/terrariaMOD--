using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M6
{
    /// <summary>
    /// 蜗牛货币 —— 以 ItemID.Snail（普通蜗牛）为货币单位
    /// 继承 CustomCurrencySingleCoin，这是 tML 官方推荐的做法
    /// </summary>
    public class SnailCurrency : CustomCurrencySingleCoin
    {
        public static SnailCurrency SnailCurrencySystem;
        public static int SnailCurrencyID;

        /// <param name="coinItemID">货币物品 ID，这里用普通蜗牛</param>
        /// <param name="currencyCap">货币上限（9999 足够）</param>
        /// <param name="currencyTextKey">本地化键，显示在价格旁</param>
        public SnailCurrency(int coinItemID, long currencyCap, string currencyTextKey) : base(coinItemID, currencyCap)
        {
            this.CurrencyTextKey = currencyTextKey;
            CurrencyTextColor = Color.Olive;        // 价格文字颜色
            CurrencyDrawScale = 1f;               // 储蓄栏图标大小（Defender Medal 是 0.8f）
        }
    }
}