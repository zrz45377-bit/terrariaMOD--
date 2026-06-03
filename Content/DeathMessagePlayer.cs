using TEACHER.Content.ALL_NPC.M1;
using TEACHER.Content.ALL_NPC.M2;
using TEACHER.Content.ALL_NPC.M3;
using TEACHER.Content.ALL_NPC.M4;
using TEACHER.Content.ALL_NPC.M5;
using TEACHER.Content.ALL_NPC.M6;
using TEACHER.Content.ALL_NPC.M7;
using TEACHER.Content.Boss;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TEACHER.Content
{
    public class DeathMessagePlayer : ModPlayer
    {
        public override bool PreKill(double damage, int hitDirection, bool pvp,
            ref bool playSound, ref bool genGore,
            ref PlayerDeathReason damageSource)
        {
            if (damageSource.SourceNPCIndex < 0)
                return true;

            NPC killer = Main.npc[damageSource.SourceNPCIndex];
            string msg = null;

            // ========== 红魔蝙蝠 ==========
            if (killer.type == ModContent.NPCType < RemiliaBat > ())
            {
                string[] messages = {
                    $"{Player.name} 被一只蝙蝠单杀了，这丢人程度足以名留青史。",
                    $"{Player.name} 现在明白为什么那位大小姐从不出门了——她的宠物蝙蝠就能搞定一切杂鱼。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 噩梦守卫的阴阳玉 ==========
            else if (killer.type == ModContent.NPCType < Evil > ())
            {
                string[] messages = {
                    $"{Player.name} 被噩梦守卫的阴阳玉碾成了连尘埃都不剩下的虚无。",
                    $"{Player.name} 试图用弹幕对抗噩梦守卫，然后发现自己的攻击和存在一起归于了虚无。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 遗落的军械箱 ==========
            else if (killer.type == ModContent.NPCType<ChestMimic>())
            {
                string[] messages = {
                    $"{Player.name} 被遗落的军械箱吞进了没有尽头的兵器深渊，连骨头都被锻成了铁渣。",
                    $"{Player.name} 满怀期待地打开了箱子，结果箱子也满怀期待地吃掉了{Player.name}。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 火把古神 ==========
            else if (killer.type == ModContent.NPCType<TorchGodsFavor>())
            {
                string[] messages = {
                    $"{Player.name} 点燃了第101支火把，古神的七色净火将{Player.name}从灵魂到肉体焚至连灰烬都不剩。",
                    $"{Player.name} 在绝对黑暗中逗留太久，古神的注视如同超新星爆发，将{Player.name}烧成了宇宙尘埃。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 飞行矿物史莱姆 ==========
            else if (killer.type == ModContent.NPCType < MineralSlimeM3 > ())
            {
                string[] messages = {
                    $"{Player.name} 被飞行矿物史莱姆的结晶弹幕轰成了血肉筛子。",
                    $"{Player.name} 试图采集这只史莱姆，结果反被它采集了生命。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 死欲的半灵 ==========
            else if (killer.type == ModContent.NPCType < YoumuHalfSpiritTrader > ())
            {
                string[] messages = {
                    $"{Player.name} 被饥饿的大小姐当成了今夜的夜宵，连骨头都没剩下。",
                    $"{Player.name} 在半灵消散的血雾中，听到了妖梦拔剑的死亡之音。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 返魂蝶·苍 ==========
            else if (killer.type == ModContent.NPCType < ResurrectionButterflyBlue > ())
            {
                string[] messages = {
                    $"{Player.name} 被返魂蝶的苍蓝磷光引渡，踏上了前往冥界的单程旅途。",
                    $"{Player.name} 试图触碰那抹苍蓝，却发现自己早已失去了可以触碰它的手——以及身体。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 返魂蝶·紫 ==========
            else if (killer.type == ModContent.NPCType < ResurrectionButterflyPurple > ())
            {
                string[] messages = {
                    $"{Player.name} 被返魂蝶的紫色磷火带往了华胥之国，再也没有回来。",
                    $"{Player.name} 沉醉于那妖艳的紫色光芒，再次醒来时，已是白玉楼庭前一株无名的枯樱。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 蠕动夜虫 ==========
            else if (killer.type == ModContent.NPCType < FireflyMerchant > ())
            {
                string[] messages = {
                    $"{Player.name} 被莉格露的狂怒虫群啃噬殆尽，连渣都没剩下。",
                    $"{Player.name} 胆敢抢夺莉格露的蜗牛，结果被夜虫之怒烧成了连灰烬都算不上的虚无。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 绯红狂蝠 ==========
            else if (killer.type == ModContent.NPCType<FlandreEliteBat>())
            {
                string[] messages = {
                    $"{Player.name} 被狂蝠当成了布丁上的樱桃，一口嚼碎，连模具都省了。",
                    $"{Player.name} 的城镇NPC朋友们先走了一步，现在{Player.name}也追上去了——以布丁馅料的形式。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }

            if (msg != null)
                damageSource = PlayerDeathReason.ByCustomReason(msg);

            return true;
        }
    }
}