using TEACHER.Content.ALL_NPC.M1;
using TEACHER.Content.ALL_NPC.M2;
using TEACHER.Content.ALL_NPC.M3;
using TEACHER.Content.ALL_NPC.M4;
using TEACHER.Content.ALL_NPC.M5;
using TEACHER.Content.ALL_NPC.M6;
using TEACHER.Content.ALL_NPC.M7;
using TEACHER.Content.ALL_NPC.M9;
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

            // ========== 红魔蝙蝠（蕾米莉亚·斯卡雷特的眷属）==========
            if (killer.type == ModContent.NPCType < RemiliaBat > ())
            {
                string[] messages = {
                    $"{Player.name} 被一只蝙蝠单杀了，这丢人程度足以名留青史。",
                    $"{Player.name} 现在明白为什么那位大小姐从不出门了——她的宠物蝙蝠就能搞定一切杂鱼。",
                    $"{Player.name} 试图用弹幕和蝙蝠对射，结果发现自己的弹幕量还不如一只蝙蝠。",
                    $"{Player.name} 被红魔馆的看门蝙蝠当成了入侵者，连门都没摸到就倒下了。",
                    $"{Player.name} 终于理解了\"红魔\"二字的含义——被红色蝙蝠魔性地送回重生点。",
                    $"{Player.name} 的鲜血被蝙蝠吸干后，大小姐表示：这血的味道，杂鱼确认。",
                    $"{Player.name} 想逃跑，但蝙蝠的追踪弹幕比{Player.name}的走位还风骚。",
                    $"{Player.name} 在红魔馆门口连第一关都没过，建议转行去人间之里种田。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 噩梦守卫的阴阳玉（灵梦相关？）==========
            else if (killer.type == ModContent.NPCType < Evil > ())
            {
                string[] messages = {
                    $"{Player.name} 被噩梦守卫的阴阳玉碾成了连尘埃都不剩下的虚无。",
                    $"{Player.name} 试图用弹幕对抗噩梦守卫，然后发现自己的攻击和存在一起归于了虚无。",
                    $"{Player.name} 的阴阳玉储备量连噩梦守卫的零头都不到，被弹幕海彻底淹没。",
                    $"{Player.name} 在绝对劣势的弹幕对决中，终于理解了什么叫\"火力压制\"。",
                    $"{Player.name} 被阴阳玉追着绕了地图三圈，最后体力不支被碾碎。",
                    $"{Player.name} 试图模仿博丽灵梦扔阴阳玉，结果噩梦守卫才是正版。",
                    $"{Player.name} 的HP在阴阳玉的连续轰炸下像雪一样融化了。",
                    $"{Player.name} 被噩梦守卫评价为：连杂鱼弹幕都躲不过的杂鱼中的杂鱼。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 遗落的军械箱（宝箱怪）==========
            else if (killer.type == ModContent.NPCType<ChestMimic>())
            {
                string[] messages = {
                    $"{Player.name} 被遗落的军械箱吞进了没有尽头的兵器深渊，连骨头都被锻成了铁渣。",
                    $"{Player.name} 满怀期待地打开了箱子，结果箱子也满怀期待地吃掉了{Player.name}。",
                    $"{Player.name} 的贪婪终于遭到了报应——这次报应会咬人。",
                    $"{Player.name} 在打开箱子的瞬间，听到了来自深渊的咀嚼声。",
                    $"{Player.name} 以为里面是传奇武器，结果里面只有{Player.name}的遗骨。",
                    $"{Player.name} 的寻宝之旅以被宝箱当成宝藏吞掉而告终。",
                    $"{Player.name} 被军械箱里的兵器从内部刺穿，变成了箱子的新装饰。",
                    $"{Player.name} 终于明白了：免费的宝箱往往是最贵的。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 火把古神（克苏鲁/古神梗）==========
            else if (killer.type == ModContent.NPCType<TorchGodsFavor>())
            {
                string[] messages = {
                    $"{Player.name} 点燃了第101支火把，古神的七色净火将{Player.name}从灵魂到肉体焚至连灰烬都不剩。",
                    $"{Player.name} 在绝对黑暗中逗留太久，古神的注视如同超新星爆发，将{Player.name}烧成了宇宙尘埃。",
                    $"{Player.name} 的光源引来了不该引来的存在——在古神面前，火把和{Player.name}一样渺小。",
                    $"{Player.name} 被七色火焰吞噬时，终于理解了为什么地下不能乱插火把。",
                    $"{Player.name} 的照明变成了致死的信号弹，古神表示很满意这份祭品。",
                    $"{Player.name} 试图用火把驱散黑暗，结果驱散的是自己的存在。",
                    $"{Player.name} 在古神的注视下连惨叫都没发出就变成了基本粒子。",
                    $"{Player.name} 的最后一个念头是：早知道带荧光棒就好了。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 飞行矿物史莱姆（M3区域）==========
            else if (killer.type == ModContent.NPCType < MineralSlimeM3 > ())
            {
                string[] messages = {
                    $"{Player.name} 被飞行矿物史莱姆的结晶弹幕轰成了血肉筛子。",
                    $"{Player.name} 试图采集这只史莱姆，结果反被它采集了生命。",
                    $"{Player.name} 的镐子还没挥下去，就被结晶弹幕打成了蜂窝煤。",
                    $"{Player.name} 低估了会飞的史莱姆，就像低估了所有会飞的凝胶状生物。",
                    $"{Player.name} 被矿物结晶贯穿时，后悔没先做个反射盾。",
                    $"{Player.name} 的矿石收藏梦碎在了这只史莱姆的结晶弹幕里。",
                    $"{Player.name} 被史莱姆当成了移动靶子，练习弹幕射击。",
                    $"{Player.name} 终于明白：有些矿物不是你能采的，有些史莱姆不是你能惹的。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 死欲的半灵（魂魄妖梦）==========
            else if (killer.type == ModContent.NPCType < YoumuHalfSpiritTrader > ())
            {
                string[] messages = {
                    $"{Player.name} 被饥饿的大小姐当成了今夜的夜宵，连骨头都没剩下。",
                    $"{Player.name} 在半灵消散的血雾中，听到了妖梦拔剑的死亡之音。",
                    $"{Player.name} 被楼观剑和白楼剑的交叉斩击切成了均匀的薄片。",
                    $"{Player.name} 试图挑战半人半灵的剑士，结果连刀光都没看清。",
                    $"{Player.name} 的魂魄被半灵吞噬，成为了白玉楼庭院里新的养分。",
                    $"{Player.name} 在妖梦的\"心剑一体\"面前，连拔刀的机会都没有。",
                    $"{Player.name} 被幽幽子大人的饥饿感波及，成为了西行妖的肥料。",
                    $"{Player.name} 的剑术在半灵剑士面前，就像树枝对抗激光剑。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 返魂蝶·苍（西行寺幽幽子）==========
            else if (killer.type == ModContent.NPCType < ResurrectionButterflyBlue > ())
            {
                string[] messages = {
                    $"{Player.name} 被返魂蝶的苍蓝磷光引渡，踏上了前往冥界的单程旅途。",
                    $"{Player.name} 试图触碰那抹苍蓝，却发现自己早已失去了可以触碰它的手——以及身体。",
                    $"{Player.name} 在苍蓝磷火中看到了冥界的风景，然后成为了风景的一部分。",
                    $"{Player.name} 被返魂蝶当成了前往华胥之国的引路明灯，只不过{Player.name}是燃料。",
                    $"{Player.name} 的魂魄被苍蓝磷光剥离，轻盈地飘向了白玉楼的方向。",
                    $"{Player.name} 在幽幽子的\"反魂蝶 -八分咲-\"面前，连八分之一的生存机会都没有。",
                    $"{Player.name} 被苍蓝磷火净化了，从肉体到灵魂都化作了冥界的尘埃。",
                    $"{Player.name} 试图数清返魂蝶的数量，结果数到了自己的寿命尽头。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 返魂蝶·紫（西行寺幽幽子）==========
            else if (killer.type == ModContent.NPCType < ResurrectionButterflyPurple > ())
            {
                string[] messages = {
                    $"{Player.name} 被返魂蝶的紫色磷火带往了华胥之国，再也没有回来。",
                    $"{Player.name} 沉醉于那妖艳的紫色光芒，再次醒来时，已是白玉楼庭前一株无名的枯樱。",
                    $"{Player.name} 在紫色磷火中看到了最美的幻觉，然后永远留在了幻觉里。",
                    $"{Player.name} 被幽幽子大人的弹幕优雅地埋葬，连墓碑都是樱花形状的。",
                    $"{Player.name} 的魂魄被紫色磷火染成了幽幽子最喜欢的颜色。",
                    $"{Player.name} 在\"樱符 -完全墨染的樱花-\"的弹幕中，被墨染成了死亡的颜色。",
                    $"{Player.name} 被返魂蝶带到了冥界的入口，门票是{Player.name}的整个存在。",
                    $"{Player.name} 试图抵抗紫色磷火的诱惑，但妖梦说：没有人类能拒绝大小姐的邀请。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 蠕动夜虫（莉格露·奈特巴格）==========
            else if (killer.type == ModContent.NPCType < FireflyMerchant > ())
            {
                string[] messages = {
                    $"{Player.name} 被莉格露的狂怒虫群啃噬殆尽，连渣都没剩下。",
                    $"{Player.name} 胆敢抢夺莉格露的蜗牛，结果被夜虫之怒烧成了连灰烬都算不上的虚无。",
                    $"{Player.name} 在虫群的嗡鸣声中，体验到了被亿万只昆虫分食的感觉。",
                    $"{Player.name} 被萤火虫的光芒吸引，然后成为了虫群的免费晚餐。",
                    $"{Player.name} 的惨叫被虫群的振翅声淹没，连回声都没留下。",
                    $"{Player.name} 试图用火把驱赶虫群，结果引来了更多愤怒的萤火虫。",
                    $"{Player.name} 被莉格露当成了入侵领地的害虫，受到了虫类的正义制裁。",
                    $"{Player.name} 在夜虫的包围中，终于理解了为什么没人敢在夜晚招惹虫使。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 绯红狂蝠（芙兰朵露·斯卡雷特）==========
            else if (killer.type == ModContent.NPCType<FlandreEliteBat>())
            {
                string[] messages = {
                    $"{Player.name} 被狂蝠当成了布丁上的樱桃，一口嚼碎，连模具都省了。",
                    $"{Player.name} 的城镇NPC朋友们先走了一步，现在{Player.name}也追上去了——以布丁馅料的形式。",
                    $"{Player.name} 被芙兰朵露的狂蝠当成了新的玩具，玩坏后就扔掉了。",
                    $"{Player.name} 在\"U.N. Owen就是她吗？\"的旋律中，被狂蝠撕成了碎片。",
                    $"{Player.name} 试图和二小姐的宠物讲道理，结果狂蝠只懂一种语言：暴力。",
                    $"{Player.name} 被狂蝠的弹幕风暴淹没，连\"禁忌\"的边都没摸到。",
                    $"{Player.name} 的鲜血染红了狂蝠的翅膀，芙兰朵露表示这颜色很配她的裙子。",
                    $"{Player.name} 在地下室门口徘徊太久，二小姐说：既然来了就别走了，永远留下吧。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }
            // ========== 被遗忘的存在（古明地恋）==========
            else if (killer.type == ModContent.NPCType < LostHat > ())
            {
                string[] messages = {
                    $"{Player.name} 忘记了那顶帽子的存在，直到它从背后轻轻摘走了{Player.name}的生命。",
                    $"{Player.name} 试图记住古明地恋的名字，但记忆连同{Player.name}本人一起，被无意识吞噬了。",
                    $"{Player.name} 闭上了第三只眼，然后连自我都一并消失了。",
                    $"{Player.name} 突然想回头看看——可惜意识已经先一步停止了。",
                    $"{Player.name} 被一顶帽子单杀了，这丢人程度比被蝙蝠单杀还离谱。",
                    $"{Player.name} 在无意识的深渊中坠落，连\"自己正在坠落\"这件事都忘记了。",
                    $"{Player.name} 的存在被古明地恋的\"空想具现化\"从世界上轻轻擦除。",
                    $"{Player.name} 试图在潜意识中寻找恋的踪迹，结果连自己的潜意识都迷失了。",
                    $"{Player.name} 被\"无意识\"这个概念的实体化形态彻底抹消，没人会记得{Player.name}曾经存在过。",
                    $"{Player.name} 的死亡本身也被遗忘了，所以{Player.name}其实还活着——在无人知晓的虚无中。"
                };
                msg = messages[Main.rand.Next(messages.Length)];
            }

            if (msg != null)
                damageSource = PlayerDeathReason.ByCustomReason(msg);

            return true;
        }
    }
}