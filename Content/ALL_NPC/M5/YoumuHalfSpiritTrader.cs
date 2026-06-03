// YoumuHalfSpiritTrader.cs
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M5
{
    internal class YoumuHalfSpiritTrader : ModNPC
    {
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement(
                    "生与死的界限对她早已模糊——毕竟她只是个被临时分出来的半灵。\n" +
                    "\t\t\t趁着夜色飘到地表，全因那位【华胥的亡灵】大小姐突然喊饿。\n" +
                    "\t\t\t本体去人间之里砍价了，而她负责不惜代价高价收购……\n" +
                    "\t\t\t千万别告诉妖梦大人她加价了，否则这个分身明天可能就再也分不出来了。")
            });
        }
        private const float BUY_PRICE_MULTIPLIER = 1.8f;
        private static readonly Random _rng = new Random();

        private bool _hasMetPlayer = false;
        private int _chatterIndex = 0;

        // ==================== 话痨台词库（兵分两路+阳奉阴违） ====================
        private static readonly List<string> _chatter = new List<string>
        {
            "妖梦大人现在应该正在人间之里砍价吧...而我这边直接高价收，回去她会不会砍我？",
            "大小姐的胃是无底洞，妖梦大人带的钱袋可能不够...所以我这边不惜代价多收点。",
            "半灵分身没有痛觉，但妖梦大人拔剑的时候...疼痛会同步过来的，很可怕。",
            "兵分两路是妖梦大人的主意，她说'你去那边，我去这边，谁买到算谁的'...结果我这边啥都没收到。",
            "如果你看到另一个拿着楼观剑的绿色身影，那就是妖梦大人本人。别告诉她我高价买食材！",
            "大小姐说'只要是能吃的都可以'...上次她连路边的发光蘑菇都啃了，妖梦大人拦都拦不住。",
            "半灵的记忆只有一晚，明天我就会忘记今晚的事...但妖梦大人不会忘，她会记账。",
            "妖梦大人的砍价技术天下第一，但她派我出来...显然没教我砍价。",
            "时间紧迫！大小姐的饥饿是白玉楼最大的自然灾害！",
            "我飘得这么低是因为...半灵能量在消散？不，是因为我在找掉在地上的食物。",
            "如果妖梦大人发现我花了这么多钱，她可能会让我代替食材被做成菜...虽然半灵不好吃。",
            "大小姐的胃连接着异次元，这是妖梦大人亲口说的...在她第108次采购归来之后。",
            "采购时间有限，妖梦大人说天亮前必须回白玉楼复命...否则大小姐会吃人，字面意思。",
            "妖梦大人让我'见机行事'...我觉得高价就是见机行事，对吧？",
            "其实妖梦大人也怕砍价砍到一半被大小姐吃掉...所以她跑得比我还快。"
        };

        public override bool CanChat() => true;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 10;
            NPC.lifeMax = 16384;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            NPC.friendly = true;
            NPC.townNPC = false;       // 无房子 = 无快乐按钮
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = 0;
        }

        public override void AI()
        {
            if (Main.dayTime)
            {
                if (NPC.active && _hasMetPlayer && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    string[] bye = new[]
                    {
                        "天亮了！妖梦大人发信号让我回去汇合了！",
                        "太阳出来了...该去白玉楼交差了，希望妖梦大人那边买够了。",
                        "采购时间结束！如果妖梦大人问我买了什么...我就说是你卖给她的。",
                        "晨光好刺眼...半灵要回归本体了。明天见...如果你还活着的话。"
                    };
                    Main.NewText($"[半灵分身] {bye[_rng.Next(bye.Length)]}", new Color(200, 255, 220));
                }
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            Player target = Main.player[NPC.target];
            if (target.active && !target.dead)
            {
                NPC.direction = target.Center.X > NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
            }

            NPC.ai[0]++;
            NPC.velocity.Y = (float)Math.Sin(NPC.ai[0] * 0.06f) * 0.4f;
            NPC.velocity.X += Main.rand.NextFloat(-0.05f, 0.05f);
            NPC.velocity.X *= 0.95f;

            NPC.alpha = 30 + (int)(Math.Sin(NPC.ai[0] * 0.12f) * 30) + Main.rand.Next(-10, 11);
            if (NPC.alpha < 10) NPC.alpha = 10;
            if (NPC.alpha > 100) NPC.alpha = 100;

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-8, 9)),
                    DustID.GreenTorch,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.8f, -0.2f)),
                    80, new Color(150, 255, 150), 0.7f);
                d.noGravity = true;
            }
            if (Main.rand.NextBool(5))
            {
                Dust d2 = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(Main.rand.Next(-4, 5), Main.rand.Next(-6, 7)),
                    DustID.WhiteTorch,
                    new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.5f, -0.1f)),
                    60, Color.White, 0.5f);
                d2.noGravity = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 6)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type])
                    NPC.frame.Y = 0;
            }
        }

        // ==================== 首次对话（兵分两路版） ====================
        public override string GetChat()
        {
            if (!_hasMetPlayer)
            {
                _hasMetPlayer = true;
                return
                    "啊、被发现了！嘘——别出声！\n" +
                    "我是魂魄妖梦的半灵分身，不是表妹！\n" +
                    "大小姐突然要吃夜宵，妖梦大人亲自去人间之里采购了...\n" +
                    "但她怕一个人买不够，就把我分出来兵分两路！\n" +
                    "所以...你身上有食物吗？我按妖梦大人说的'合理价格'收购...\n" +
                    "好吧其实我偷偷加价了，千万别告诉她！她让我砍价来着。";
            }

            string line = _chatter[_chatterIndex];
            _chatterIndex = (_chatterIndex + 1) % _chatter.Count;

            if (_rng.NextDouble() < 0.35)
                line += "\n" + _chatter[_rng.Next(_chatter.Count)];
            return line;
        }

        // ==================== 只有一个按钮：出售食物 ====================
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "出售食物";
            button2 = "";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (!firstButton) return;

            Player plr = Main.LocalPlayer;
            bool soldAnything = false;
            long totalMoney = 0;
            string firstReaction = null;

            for (int i = 0; i < 50; i++)
            {
                Item item = plr.inventory[i];
                if (item == null || item.stack <= 0) continue;

                if (IsFood(item))
                {
                    long stackValue = (long)(item.value * BUY_PRICE_MULTIPLIER) * item.stack;
                    totalMoney += stackValue;

                    if (firstReaction == null)
                        firstReaction = GetFoodReaction(item.Name);

                    item.TurnToAir();
                    soldAnything = true;
                }
            }

            if (soldAnything)
            {
                GiveMoney(plr, totalMoney);
                string priceText = FormatMoney(totalMoney);
                Main.NewText($"[半灵分身] {firstReaction}", new Color(200, 255, 220));
                Main.NewText($"[半灵分身] 总共 {priceText}！大小姐会满意的~大概。", Color.Gold);

                if (_rng.NextDouble() < 0.4)
                    Main.NewText($"[半灵分身] {_chatter[_rng.Next(_chatter.Count)]}", new Color(200, 255, 220));
            }
            else
            {
                string[] noFood = new[]
                {
                    "背包里连一颗糖都没有吗？！那妖梦大人那边压力就大了...",
                    "空空如也...兵分两路结果我这一路全军覆没，妖梦大人会骂死我的。",
                    "没有食物...没有食物...我要变成大小姐和妖梦大人之间的传话工具了...那更惨。",
                    "你是靠光合作用生存的吗？那你能不能光合作用出一点团子来？"
                };
                Main.NewText($"[半灵分身] {noFood[_rng.Next(noFood.Length)]}", new Color(200, 255, 220));
            }
        }

        private bool IsFood(Item item)
        {
            if (item == null || item.stack <= 0) return false;
            if (ItemID.Sets.IsFood[item.type]) return true;
            if (item.buffType == BuffID.WellFed || item.buffType == BuffID.WellFed2 || item.buffType == BuffID.WellFed3 || item.buffType == BuffID.Tipsy)
                return true;
            return false;
        }

        private string GetFoodReaction(string foodName)
        {
            string l = foodName.ToLowerInvariant();
            if (l.Contains("dango") || l.Contains("团子") || l.Contains("mochi"))
                return "团子！大小姐最喜欢三色团子了~";
            if (l.Contains("noodle") || l.Contains("pho") || l.Contains("spaghetti") || l.Contains("ramen") || l.Contains("面") || l.Contains("粉"))
                return "面条吗...虽然大小姐更想要白玉楼特供热汤面，但这个也行吧。";
            if (l.Contains("sushi") || l.Contains("sashimi") || l.Contains("生鱼"))
                return "哦哦！是寿司！这个大小姐一定会很满意的！妖梦大人可切不出这么细的片。";
            if (l.Contains("pudding") || l.Contains("布丁"))
                return "布丁...半灵不需要吃东西，但大小姐说布丁是'灵魂的食物'。真奇怪，灵魂明明是我啊。";
            if (l.Contains("cake") || l.Contains("cookie") || l.Contains("donut") || l.Contains("甜") || l.Contains("candy") || l.Contains("ice cream") || l.Contains("糖"))
                return "甜食！大小姐的挚爱！你懂行啊！妖梦大人从来不买这些...";
            if (l.Contains("ale") || l.Contains("sake") || l.Contains("wine") || l.Contains("beer") || l.Contains("酒") || l.Contains("啤"))
                return "饮品吗...大小姐说搭配甜食刚刚好！不过妖梦大人看到会生气的。";
            if (l.Contains("fish") || l.Contains("shrimp") || l.Contains("lobster") || l.Contains("salmon") || l.Contains("seafood") || l.Contains("海鲜"))
                return "海鲜！大小姐的口味很高级呢~比妖梦大人烤的焦黑鱼强多了。";
            if (l.Contains("meat") || l.Contains("steak") || l.Contains("bacon") || l.Contains("rib") || l.Contains("burger") || l.Contains("肉"))
                return "肉食！大小姐的体力补充剂！半灵虽然不吃，但大小姐的胃是无底洞。";
            if (l.Contains("fruit") || l.Contains("苹果") || l.Contains("香蕉") || l.Contains("桃") || l.Contains("果"))
                return "水果！健康的选择...但大小姐更想要巧克力覆盖版的。";
            return $"这个「{foodName}」应该合大小姐的口味...大概。反正比妖梦大人做的安全。";
        }

        private void GiveMoney(Player plr, long amount)
        {
            int plat = (int)(amount / 1000000);
            int gold = (int)((amount % 1000000) / 10000);
            int silver = (int)((amount % 10000) / 100);
            int copper = (int)(amount % 100);

            var src = plr.GetSource_GiftOrReward("HalfSpiritTrade");
            if (plat > 0) plr.QuickSpawnItem(src, ItemID.PlatinumCoin, plat);
            if (gold > 0) plr.QuickSpawnItem(src, ItemID.GoldCoin, gold);
            if (silver > 0) plr.QuickSpawnItem(src, ItemID.SilverCoin, silver);
            if (copper > 0) plr.QuickSpawnItem(src, ItemID.CopperCoin, copper);
        }

        private string FormatMoney(long amount)
        {
            int plat = (int)(amount / 1000000);
            int gold = (int)((amount % 1000000) / 10000);
            int silver = (int)((amount % 10000) / 100);
            int copper = (int)(amount % 100);

            var parts = new List<string>();
            if (plat > 0) parts.Add($"{plat}铂");
            if (gold > 0) parts.Add($"{gold}金");
            if (silver > 0) parts.Add($"{silver}银");
            if (copper > 0) parts.Add($"{copper}铜");
            return parts.Count > 0 ? string.Join("", parts) : "0铜";
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 16; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GreenTorch, hit.HitDirection * 2, -2f, 0, new Color(200, 255, 200), 1.2f);
                for (int i = 0; i < 8; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.WhiteTorch, hit.HitDirection * 1, -1f, 0, Color.White, 0.8f);
            }
        }
    }
}