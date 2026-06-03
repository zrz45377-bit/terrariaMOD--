using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TEACHER.Content.ITEM_S.N13;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M6
{
    /// <summary>
    /// 莉格露·奈特巴格（Wriggle Nightbug）—— 蠕动夜虫
    /// 东方Project的萤火虫妖怪，夏天出没，喜欢吃蜗牛
    /// aiStyle=-1 完全自定义，手动补全城镇NPC的入住/回家逻辑
    /// </summary>
    [AutoloadHead]
    public class FireflyMerchant : ModNPC
    {
        // 动画：竖排 4 帧
        private const int FrameCount = 4;
        private const int FrameSpeed = 8;
        private int _frameCounter = 0;
        private int _currentFrame = 0;

        // 商店名
        public const string ShopName = "Shop";

        // 1只发光蜗牛 = 5只普通蜗牛
        private const int GlowingExchangeRate = 5;

        // 虫价（普通蜗牛计价）
        private const int TruffleWormPrice = 10;
        private const int EmpressButterflyPrice = 16;

        // 回家/找房相关
        private const int HomeSearchInterval = 300;   // 5秒找一次房
        private const float HomeRange = 20f * 16f;    // 房子周围20格游荡范围
        private const float NightReturnSpeed = 3f;    // 夜晚回家速度

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = FrameCount;

            NPCID.Sets.TownCritter[NPC.type] = true;
            NPCID.Sets.ActsLikeTownNPC[NPC.type] = true;
            NPCID.Sets.AllowDoorInteraction[NPC.type] = false;
        }

        public override void SetDefaults()
        {
            NPC.width = 34;
            NPC.height = 34;
            NPC.aiStyle = -1;           // ← 保持自定义，自己管入住
            NPC.damage = 0;
            NPC.defense = 10;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.3f;
            NPC.friendly = true;
            NPC.townNPC = true;         // ← 关键：标记为城镇NPC，才会被系统统计人口
            NPC.noGravity = true;       // ← 漂浮
            NPC.noTileCollide = false;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement("Mods.TEACHER.NPCs.FireflyMerchant.Bestiary")
            });
        }

        // ========== 核心 AI：漂浮 + 城镇逻辑 ==========
        public override void AI()
        {
            // ===== 1. 自动找房（无家可归时） =====
            if (NPC.homeless && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1]++;
                if (NPC.ai[1] >= HomeSearchInterval)
                {
                    NPC.ai[1] = 0;
                    TryFindHome();
                }
            }

            // ===== 2. 有房子时的行为 =====
            if (!NPC.homeless)
            {
                Vector2 homePos = new Vector2(NPC.homeTileX * 16, NPC.homeTileY * 16);

                // 夜晚：强制回家
                if (!Main.dayTime)
                {
                    Vector2 toHome = homePos - NPC.Center;
                    float dist = toHome.Length();

                    if (dist > 32f) // 离家超过2格就往回飘
                    {
                        toHome.Normalize();
                        NPC.velocity = Vector2.Lerp(NPC.velocity, toHome * NightReturnSpeed, 0.08f);
                    }
                    else
                    {
                        // 到家了，悬停
                        NPC.velocity *= 0.9f;
                        NPC.velocity.Y = (float)Math.Sin(Main.GameUpdateCount * 0.05f) * 0.3f;
                    }
                }
                // 白天：在房子周围漂浮游荡
                else
                {
                    NPC.ai[0]++;

                    // 垂直漂浮
                    NPC.velocity.Y = (float)Math.Sin(NPC.ai[0] * 0.05f) * 0.5f;

                    // 水平游荡：别飘太远
                    if (NPC.ai[0] % 100 == 0)
                    {
                        // 如果离房子太远，往家飘；否则随机逛
                        float distToHome = Math.Abs(NPC.Center.X - homePos.X);
                        if (distToHome > HomeRange)
                        {
                            NPC.direction = NPC.Center.X > homePos.X ? -1 : 1;
                            NPC.velocity.X = NPC.direction * 1.5f;
                        }
                        else
                        {
                            NPC.direction = Main.rand.NextBool() ? 1 : -1;
                            NPC.velocity.X = NPC.direction * (0.3f + Main.rand.NextFloat() * 0.5f);
                        }
                    }
                    else
                    {
                        NPC.velocity.X *= 0.96f;
                    }

                    // 硬性范围限制（防止卡出世界）
                    if (NPC.velocity.X > 2f) NPC.velocity.X = 2f;
                    if (NPC.velocity.X < -2f) NPC.velocity.X = -2f;
                }
            }
            // ===== 3. 无家可归时：跟随玩家漂浮 =====
            else
            {
                NPC.ai[0]++;

                // 垂直正弦漂浮
                NPC.velocity.Y = (float)Math.Sin(NPC.ai[0] * 0.05f) * 0.5f;

                // 水平：缓慢游荡
                if (NPC.ai[0] % 100 == 0)
                {
                    NPC.direction = Main.rand.NextBool() ? 1 : -1;
                    NPC.velocity.X = NPC.direction * (0.3f + Main.rand.NextFloat() * 0.4f);
                }
                else
                {
                    NPC.velocity.X *= 0.95f;
                }

                if (NPC.velocity.X > 1.5f) NPC.velocity.X = 1.5f;
                if (NPC.velocity.X < -1.5f) NPC.velocity.X = -1.5f;
            }

            // 通用：朝向与发光
            NPC.spriteDirection = NPC.direction;
            Lighting.AddLight(NPC.Center, 0.3f, 0.8f, 0.4f);
        }

        /// <summary>
        /// 自动找房：扫描玩家附近的安全区域作为落脚点。
        /// 注意：这不是严格的房屋验证，但足够让 NPC 被系统识别为"已入住"。
        /// 玩家也可以通过房屋菜单手动重新分配。
        /// </summary>
        private void TryFindHome()
        {
            // 收集已被占用的家坐标（避免抢房）
            var occupied = new HashSet<(int x, int y)>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.townNPC && !other.homeless && other.whoAmI != NPC.whoAmI)
                {
                    occupied.Add((other.homeTileX, other.homeTileY));
                }
            }

            // 遍历所有活跃玩家，在其周围找落脚点
            for (int p = 0; p < Main.maxPlayers; p++)
            {
                Player player = Main.player[p];
                if (!player.active || player.dead) continue;

                Point center = player.Center.ToTileCoordinates();

                // 随机扫描周围区域
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    int tryX = center.X + Main.rand.Next(-25, 26);
                    int tryY = center.Y + Main.rand.Next(-15, 11);

                    if (!WorldGen.InWorld(tryX, tryY, 10)) continue;

                    // 简单地形验证：脚下有地板，头顶有空位
                    if (!WorldGen.SolidTile(tryX, tryY + 1)) continue;      // 需要地板
                    if (WorldGen.SolidTile(tryX, tryY)) continue;           // 不能卡在墙里
                    if (WorldGen.SolidTile(tryX, tryY - 1)) continue;       // 头顶至少空一格

                    // 检查是否已被占用
                    if (occupied.Contains((tryX, tryY))) continue;

                    // 找到家了！
                    NPC.homeless = false;
                    NPC.homeTileX = tryX;
                    NPC.homeTileY = tryY;
                    NPC.netUpdate = true;

                    // 入住特效
                    for (int i = 0; i < 16; i++)
                    {
                        Dust d = Dust.NewDustPerfect(
                            new Vector2(tryX * 16 + 8, tryY * 16),
                            DustID.MagicMirror,
                            new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 0f)),
                            0, Color.GreenYellow, 1.2f);
                        d.noGravity = true;
                    }

                    // 通知
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Main.NewText("蠕动夜虫在附近找到了落脚点，决定住下来了！", new Color(180, 255, 150));
                    }
                    return;
                }
            }
        }

        // ========== 竖排 4 帧绘制 ==========
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;

            _frameCounter++;
            if (_frameCounter >= FrameSpeed)
            {
                _frameCounter = 0;
                _currentFrame = (_currentFrame + 1) % FrameCount;
            }

            int frameWidth = texture.Width;
            int frameHeight = texture.Height / FrameCount;
            Rectangle sourceRect = new Rectangle(0, _currentFrame * frameHeight, frameWidth, frameHeight);

            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(texture, NPC.Center - screenPos, sourceRect, drawColor,
                NPC.rotation, origin, NPC.scale, effects, 0f);
            return false;
        }

        // ========== 对话 ==========
        public override string GetChat()
        {
            Player player = Main.LocalPlayer;
            int snailCount = CountItem(player, ItemID.Snail);
            int glowingCount = CountItem(player, ItemID.GlowingSnail);

            if (NPC.homeless && Main.rand.NextBool(3))
                return "我还没有找到住的地方呢...你能给我分个房子吗？或者我可以在附近随便找个角落？";

            if (glowingCount > 0 && Main.rand.NextBool(2))
                return $"你身上有 {glowingCount} 只发光蜗牛？看起来好好吃...可以给我吗？我每只给你 {GlowingExchangeRate} 只普通蜗牛当回礼！";

            if (snailCount > 0)
            {
                string[] hungryLines = new[]
                {
                    $"你带了 {snailCount} 只蜗牛？咔嚓咔嚓...蜗牛，最棒了！",
                    "蜗牛的软肉配上蘑菇地的泥土香...啊，你在看吗？要、要一起来吃吗？",
                    "普通蜗牛虽然不如发光蜗牛有嚼劲，但数量够多也能吃饱呢！"
                };
                return hungryLines[Main.rand.Next(hungryLines.Length)];
            }

            if (Main.rand.NextBool(4) && NPC.downedPlantBoss)
                return "夏天的夜晚，神圣地里会有漂亮的虫子飞出来...不过对我来说，还是蜗牛比较好吃。";

            string[] lines = new[]
            {
                "我是蠕动夜虫，夏天的萤火虫妖怪！你身上有带吃的吗？",
                "蠕动夜虫——就是能在夜里悄悄爬来爬去的意思哦，很帅吧？",
                "我的商店里卖很稀有的虫子，但只收普通蜗牛当伙食费！",
                "发光蜗牛最好吃了！蘑菇地的微光会让它们的肉质更紧实...你要尝尝吗？",
                "这些虫子都是我抓来卖的，但蜗牛是我留着自己吃的，所以你要用蜗牛来换！",
                "夏天的夜晚很长，没有蜗牛吃的话，萤火虫的光芒也会变暗淡呢...",
                "1、2、3...你猜我一次能吃多少只蜗牛？答案是——有多少吃多少！"
            };
            return lines[Main.rand.Next(lines.Length)];
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string> { "蠕动夜虫", "莉格露", "夜虫", "Wriggle" };
        }

        // 入住条件：困难模式后，有空房时自动生成
        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return Main.hardMode;
        }

        // ========== 按钮 ==========
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "商店";
            button2 = "喂我吃发光蜗牛";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = ShopName;
            }
            else
            {
                EatGlowingSnails(Main.LocalPlayer);
            }
        }

        // ========== 商店 ==========
        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, ShopName);

            // ========== 太阳虫头盔（装备）==========
            // 不可合成，但可以用 30 只蜗牛从莉格露这里换
            npcShop.Add(new Item(ModContent.ItemType < SunWormHelmet > ())
            {
                shopCustomPrice = 50,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            // ========== 泰拉瑞亚原有虫类（全部只收蜗牛）==========

            // ── 蝴蝶 ──
            npcShop.Add(new Item(ItemID.JuliaButterfly)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.MonarchButterfly)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.PurpleEmperorButterfly)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.RedAdmiralButterfly)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.SulphurButterfly)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.TreeNymphButterfly)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.UlyssesButterfly)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.ZebraSwallowtailButterfly)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.GoldButterfly)
            {
                shopCustomPrice = 15,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            // ── 萤火虫 & 闪电虫 ──
            npcShop.Add(new Item(ItemID.Firefly)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.LightningBug)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            // ── 蜗牛 & 蠕虫 ──
            // 发光蜗牛更稀有、更好吃，所以比普通蜗牛贵
            npcShop.Add(new Item(ItemID.GlowingSnail)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.Worm)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.GoldWorm)
            {
                shopCustomPrice = 15,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.EnchantedNightcrawler)
            {
                shopCustomPrice = 9,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            // ── 蚱蜢 ──
            npcShop.Add(new Item(ItemID.Grasshopper)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.GoldGrasshopper)
            {
                shopCustomPrice = 10,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            // ── 蝎子 & 其他小虫 ──
            npcShop.Add(new Item(ItemID.Scorpion)
            {
                shopCustomPrice = 2,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.BlackScorpion)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.Grubby)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });
            npcShop.Add(new Item(ItemID.Sluggy)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            // ========== 原有顶级商品（保留蜗牛货币）==========
            npcShop.Add(new Item(ItemID.TruffleWorm)
            {
                shopCustomPrice = TruffleWormPrice,        // 10 只蜗牛
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            npcShop.Add(new Item(ItemID.EmpressButterfly)
            {
                shopCustomPrice = EmpressButterflyPrice,   // 16 只蜗牛
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            npcShop.Add(new Item(ItemID.Abeemination)
            {
                shopCustomPrice = 5,   // 16 只蜗牛
                shopSpecialCurrency = SnailCurrency.SnailCurrencyID
            });

            npcShop.Register();
        }

        // ========== 吃发光蜗牛（兑换） ==========
        private void EatGlowingSnails(Player player)
        {
            int glowingCount = CountItem(player, ItemID.GlowingSnail);

            if (glowingCount <= 0)
            {
                Main.npcChatText = "没有发光蜗牛吗...发光蜗牛在地下蘑菇地才有，比普通蜗牛好吃三倍呢！";
                return;
            }

            int normalToGive = glowingCount * GlowingExchangeRate;
            RemoveItems(player, ItemID.GlowingSnail, glowingCount);
            player.QuickSpawnItem(NPC.GetSource_GiftOrReward(), ItemID.Snail, normalToGive);

            for (int i = 0; i < 8; i++)
            {
                Vector2 dustPos = NPC.Center + new Vector2(Main.rand.Next(-12, 12), -10);
                Dust d = Dust.NewDustPerfect(dustPos, DustID.Grass,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -1.5f), 0, Color.GreenYellow, 1.0f);
                d.noGravity = true;
            }

            Main.npcChatText = $"嘎吱嘎吱...{glowingCount} 只发光蜗牛真美味！这是吃剩的，一共 {normalToGive} 只，攒够了就能在我的商店里换虫子啦！";
        }

        // ========== 背包工具 ==========
        private int CountItem(Player player, int itemType)
        {
            int count = 0;
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item.type == itemType && !item.favorited)
                    count += item.stack;
            }
            return count;
        }

        private void RemoveItems(Player player, int itemType, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < 58 && remaining > 0; i++)
            {
                Item item = player.inventory[i];
                if (item.type == itemType && !item.favorited)
                {
                    int remove = Math.Min(remaining, item.stack);
                    item.stack -= remove;
                    remaining -= remove;
                    if (item.stack <= 0)
                        item.TurnToAir();
                }
            }
        }
    }
}