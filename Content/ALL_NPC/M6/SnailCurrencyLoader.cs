using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M6
{
    public class SnailCurrencyLoader : ModSystem
    {
        public override void Load()
        {
            SnailCurrency.SnailCurrencyID = CustomCurrencyManager.RegisterCurrency(
                SnailCurrency.SnailCurrencySystem = new SnailCurrency(
                    ItemID.Snail,
                    9999L,
                    "Mods.TEACHER.Currency.Snail"  // 本地化键，显示货币名
                )
            );
        }

        public override void Unload()
        {
            SnailCurrency.SnailCurrencySystem = null;
            SnailCurrency.SnailCurrencyID = 0;
        }
    }
}
