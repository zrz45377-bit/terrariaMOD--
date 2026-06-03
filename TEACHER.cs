using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEACHER.Content.ALL_NPC.M6;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class TEACHER : Mod
	{
        public override void Load()
        {
            // 注册蜗牛货币：普通蜗牛，上限 9999
            SnailCurrency.SnailCurrencySystem = new SnailCurrency(
                ItemID.Snail,
                9999L,
                "Mods.TEACHER.Currency.Snail"
            );
            SnailCurrency.SnailCurrencyID = CustomCurrencyManager.RegisterCurrency(SnailCurrency.SnailCurrencySystem);
        }
    }
}
