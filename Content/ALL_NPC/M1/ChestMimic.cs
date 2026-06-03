using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ALL_NPC.M1
{
    // 宝箱怪 NPC：伪装成宝箱的敌人，死亡后按世界进度掉落随机武器
    // 设计核心：累积池——每打败一个 Boss，就把它的战利品加入掉落池，越后期池子越大
    public class ChestMimic : ModNPC
    {
        // ========== 图鉴注册 ==========
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement(
                    "从某个覆灭古国的废墟中苏醒的活体兵器库。\r\n\t\t\t它并不渴求金银珠宝，只贪婪地吞噬着世间一切武器与战具。\r\n\t\t\t越是经历过惨烈厮杀的冒险者，越能唤醒它深处沉睡的\"记忆\"——"
                    )
            });
        }
        public override void SetStaticDefaults()
        {
            // 只有一帧动画（静止贴图），不需要多帧精灵表
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            // 碰撞箱大小（和贴图一致）
            NPC.width = 44;
            NPC.height = 44;

            // 战斗属性：普通敌人水平，不是无敌木桩
            NPC.damage = 80;          // 接触伤害
            NPC.defense = 20;         // 防御（能打死的）
            NPC.lifeMax = 600;        // 血量（约等于原版宝箱怪）

            // 音效：金属受击声，更像箱子
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;

            // 金币价值（击杀时掉落的默认金币，额外在 OnKill 里再加）
            NPC.value = 5000f;

            // 击退抗性：有点重，不会被一下打飞
            NPC.knockBackResist = 0.3f;

            // AI 风格 -1 = 完全自定义，不走原版预设
            NPC.aiStyle = -1;
        }

        // ========== 自定义 AI：伪装静止 → 发现玩家 → 跳跃扑击 ==========
        // 行为类似原版宝箱怪：玩家靠近前一动不动，靠近后突然跳起来咬人
        public override void AI()
        {
            // 寻找最近玩家作为目标
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            // 被击中后重置跳跃冷却（被打会立刻反击）
            if (NPC.justHit) NPC.ai[0] = 0f;

            // ai[0] 用作跳跃计时器，每帧 +1
            NPC.ai[0]++;

            // 面向玩家（决定贴图朝向）
            NPC.spriteDirection = (player.Center.X > NPC.Center.X) ? 1 : -1;

            // 检测是否站在地面上（脚底 4 像素范围内有实体方块）
            bool onGround = Collision.SolidCollision(
                new Vector2(NPC.position.X, NPC.position.Y + NPC.height - 4f),
                NPC.width, 8);

            if (onGround)
            {
                // 站在地上时垂直速度归零（防止穿地）
                NPC.velocity.Y = 0f;

                // 跳跃条件：冷却 >= 90 帧（1.5秒）且玩家在 400 像素内
                if (NPC.ai[0] >= 90f && NPC.Distance(player.Center) < 400f)
                {
                    NPC.ai[0] = 0f;                           // 重置冷却
                    NPC.velocity.Y = -9f;                     // 起跳（向上）
                    NPC.velocity.X = 5f * NPC.spriteDirection;  // 向玩家方向冲刺
                }
                else
                {
                    // 不满足跳跃条件时慢慢减速（看起来像在地上摩擦）
                    NPC.velocity.X *= 0.85f;
                }
            }
            else
            {
                // 空中受重力影响
                NPC.velocity.Y += 0.45f;
                // 限制最大下落速度，防止穿墙
                if (NPC.velocity.Y > 12f) NPC.velocity.Y = 12f;
            }
        }

        // ========== 死亡掉落：按世界进度从累积池里抽一件武器 ==========
        // 这是核心机制——池子随着 Boss 击杀记录越来越大
        public override void OnKill()
        {
            // 1. 根据当前世界进度，获取一件随机武器 ID
            int itemId = GetProgressiveItem();

            // 2. 生成物品到死亡位置，带一点随机初速度（像爆出来一样）
            Item.NewItem(
                NPC.GetSource_Loot(),           // 掉落来源（用于图鉴追踪）
                NPC.Center,                     // 生成位置：尸体中心
                itemId,                         // 物品 ID
                1                              // 数量
            );

            // 3. 额外爆点金币（更像宝箱的感觉）
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.GoldCoin, Main.rand.Next(1, 4));

            // 4. 死亡粒子：金色尘爆
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.GoldCoin,
                    Main.rand.NextVector2Circular(5f, 5f), 0, Color.Gold, 1.5f);
                d.noGravity = true;
            }
        }

        // ========== 累积池核心：按 Boss 进度 + 事件进度解锁武器 ==========
        // 设计理念：
        // - 每打败一个 Boss，就把它的专属掉落 + 对应事件武器加入池子
        // - 旧武器保留，所以后期池子非常庞大，每次开箱都有新鲜感
        // - 事件武器绑定到该事件"通常触发的时间段"（如海盗入侵绑定肉后）
        private int GetProgressiveItem()
        {
            var pool = new List<int>();

            // ── 阶段 0：基础矿物武器（无任何 Boss 时也能开出东西）────
            // 包含所有金属剑、弓、宝石法杖——肉前开荒装备
            pool.AddRange(new int[] {
                ItemID.CopperBroadsword, ItemID.TinBroadsword,
                ItemID.IronBroadsword, ItemID.LeadBroadsword,
                ItemID.SilverBroadsword, ItemID.TungstenBroadsword,
                ItemID.GoldBroadsword, ItemID.PlatinumBroadsword,
                ItemID.CopperBow, ItemID.TinBow,
                ItemID.IronBow, ItemID.LeadBow,
                ItemID.SilverBow, ItemID.TungstenBow,
                ItemID.GoldBow, ItemID.PlatinumBow,
                ItemID.WandofSparking, ItemID.AmethystStaff,
                ItemID.TopazStaff, ItemID.SapphireStaff,
                ItemID.EmeraldStaff, ItemID.RubyStaff,
                ItemID.DiamondStaff, ItemID.AmberStaff,
            });

            // ── 阶段 1：克苏鲁之眼 + 哥布林入侵 + 血月 ───────────────
            // 克眼是玩家第一个正式 Boss，原版中哥布林入侵和血月也常在此时触发
            if (NPC.downedBoss1)
            {
                // 克眼本身掉落的武器/材料
                pool.AddRange(new int[] {
                    ItemID.ShadowScale, ItemID.TissueSample,
                    ItemID.DemoniteOre, ItemID.CrimtaneOre,
                    ItemID.TheUndertaker, ItemID.Musket,
                    ItemID.Vilethorn, ItemID.BallOHurt,
                    ItemID.CrimsonRod,
                });

                // 哥布林入侵武器（原版哥布林入侵在克眼后自然触发）
                pool.AddRange(new int[] {
                    ItemID.Harpoon,           // 哥布林掉落
                    ItemID.SpikyBall,         // 哥布林掉落
                    ItemID.TatteredCloth,     // 哥布林召唤师掉落
                });

                // 血月武器（血月早期就有，但把血月专属作为克眼后奖励）
                pool.AddRange(new int[] {
                    ItemID.SharkToothNecklace,   // 血月敌怪掉落
                    ItemID.ZombieArm,            // 僵尸掉落
                    ItemID.BloodyMachete,        // 小丑/血月敌怪
                    ItemID.Bananarang,           // 小丑
                    ItemID.CoinGun,              // 海盗/血月稀有
                    ItemID.VampireFrogStaff,     // 血月钓鱼
                });
            }

            // ── 阶段 2：世界吞噬者 / 克苏鲁之脑 ──────────────────────
            // 腐化/猩红第一个 Boss，解锁暗影珠/猩红心相关物品
            if (NPC.downedBoss2)
            {
                pool.AddRange(new int[] {
                    ItemID.BandofStarpower,   // 暗影珠宝箱
                    ItemID.MagicMirror,       // 暗影珠宝箱
                });
            }

            // ── 阶段 3：骷髅王 ─────────────────────────────────────
            // 地牢守护者，解锁地牢武器库
            if (NPC.downedBoss3)
            {
                pool.AddRange(new int[] {
                    ItemID.Muramasa,        // 地牢金箱
                    ItemID.AquaScepter,     // 地牢金箱
                    ItemID.BlueMoon,        // 地牢金箱
                    ItemID.MagicMissile,    // 地牢金箱
                    ItemID.Handgun,         // 地牢金箱
                    ItemID.CobaltShield,    // 地牢金箱
                });
            }

            // ── 阶段 4：蜂王 ─────────────────────────────────────
            // 丛林可选 Boss，丰富池子
            if (NPC.downedQueenBee)
            {
                pool.AddRange(new int[] {
                    ItemID.BeeGun,
                    ItemID.BeeKeeper,
                    ItemID.HiveWand,
                    ItemID.HoneyComb,
                    ItemID.Nectar,
                    ItemID.BeesKnees,
                });
            }

            // ── 阶段 5：血肉之墙 + 海盗入侵 ───────────────────────
            // 肉山是困难模式分水岭；原版海盗入侵只在困难模式触发
            if (Main.hardMode)
            {
                // 肉山掉落
                pool.AddRange(new int[] {
                    ItemID.BreakerBlade,
                    ItemID.ClockworkAssaultRifle,
                    ItemID.LaserRifle,
                    ItemID.WarriorEmblem,
                    ItemID.SorcererEmblem,
                    ItemID.RangerEmblem,
                    ItemID.SummonerEmblem,
                });

                // 海盗入侵武器（肉后事件）
                pool.AddRange(new int[] {
                    ItemID.GoldRing,         // 海盗船长
                    ItemID.LuckyCoin,        // 海盗船长
                    ItemID.DiscountCard,     // 海盗船长
                    ItemID.PirateStaff,      // 海盗船长
                    ItemID.Cutlass,          // 海盗敌怪
                    ItemID.CoinGun,          // 海盗稀有
                });
            }

            // ── 阶段 6：新三王（任意一个机械 Boss）────────────────
            // 神圣锭装备、机械Boss专属掉落
            if (NPC.downedMechBossAny)
            {
                pool.AddRange(new int[] {
                    ItemID.Excalibur,
                    ItemID.Gungnir,
                    ItemID.LightDisc,
                    ItemID.HallowedRepeater,
                    ItemID.Flamethrower,     // 毁灭者
                    ItemID.OpticStaff,       // 双子魔眼
                });
            }

            // ── 阶段 7：世纪之花 + 南瓜月 + 霜月 ───────────────────
            // 花后是原版后期分水岭；南瓜月和霜月都是花后事件
            if (NPC.downedPlantBoss)
            {
                // 世纪之花掉落
                pool.AddRange(new int[] {
                    ItemID.GrenadeLauncher,
                    ItemID.VenusMagnum,
                    ItemID.NettleBurst,
                    ItemID.LeafBlower,
                    ItemID.FlowerPow,
                    ItemID.WaspGun,
                    ItemID.Seedler,
                    ItemID.PygmyStaff,
                    ItemID.ThornHook,
                });

                // 南瓜月武器（花后事件）
                pool.AddRange(new int[] {
                    ItemID.TheHorsemansBlade,      // 南瓜王
                    ItemID.BatScepter,             // 南瓜王
                    ItemID.RavenStaff,             // 南瓜王
                    ItemID.JackOLanternLauncher,   // 南瓜王
                    ItemID.BlackFairyDust,         // 无头骑士
                    ItemID.SpiderEgg,              // 蜘蛛女王
                    ItemID.CursedSapling,          // 树精
                    ItemID.NecromanticScroll,      // 哀木
                    ItemID.CandyCornRifle,         // 稻草人
                });

                // 霜月武器（花后事件）
                pool.AddRange(new int[] {
                    ItemID.SnowmanCannon,      // 冰雪女王
                    ItemID.NorthPole,          // 冰雪女王
                    ItemID.BlizzardStaff,      // 冰雪女王
                    ItemID.ChristmasTreeSword, // 常绿尖叫怪
                    ItemID.Razorpine,          // 常绿尖叫怪
                    ItemID.ElfMelter,          // 圣诞坦克
                    ItemID.FestiveWings,       // 圣诞坦克
                });
            }

            // ── 阶段 8：石巨人 ───────────────────────────────────
            // 丛林神庙 Boss，解锁神庙和后期装备
            if (NPC.downedGolemBoss)
            {
                pool.AddRange(new int[] {
                    ItemID.Picksaw,
                    ItemID.StaffofEarth,
                    ItemID.HeatRay,
                    ItemID.EyeoftheGolem,
                    ItemID.Stynger,
                    ItemID.PossessedHatchet,
                    ItemID.SunStone,
                    ItemID.GolemFist,
                });
            }

            // ── 阶段 9：猪龙鱼公爵（可选后期挑战）────────────────
            // 原版最强可选 Boss 之一，掉落物非常强力
            if (NPC.downedFishron)
            {
                pool.AddRange(new int[] {
                    ItemID.Flairon,
                    ItemID.Tsunami,
                    ItemID.RazorbladeTyphoon,
                    ItemID.BubbleGun,
                    ItemID.TempestStaff,
                });
            }

            // ── 阶段 10：光之女皇（可选后期挑战）─────────────────
            // 花后可打，但难度极高，掉落毕业级召唤/近战武器
            if (NPC.downedEmpressOfLight)
            {
                pool.AddRange(new int[] {
                    ItemID.PiercingStarlight,
                    ItemID.SeveredHandBanner,
                    ItemID.EmpressBlade,
                    ItemID.RainbowWhip,
                    ItemID.FairyQueenMagicItem,
                    ItemID.FairyQueenRangedItem,
                    ItemID.RainbowCrystalStaff,
                });
            }

            // ── 阶段 11：月球领主（真·毕业池）────────────────────
            // 游戏最终 Boss，池子加入所有毕业武器
            if (NPC.downedMoonlord)
            {
                pool.AddRange(new int[] {
                    ItemID.Meowmere,
                    ItemID.Terrarian,
                    ItemID.StarWrath,
                    ItemID.SDMG,
                    ItemID.LastPrism,
                    ItemID.LunarFlareBook,
                    ItemID.RainbowCrystalStaff,
                    ItemID.MoonlordTurretStaff,
                    ItemID.SwordWhip,
                });
            }

            // 从累积好的池子里均匀随机抽一件
            return pool[Main.rand.Next(pool.Count)];
        }

        // ========== 自然生成概率（含事件期间大幅提升）==========
        // 设计理念：平时地下偶尔遇到，事件期间大量涌现，营造"事件专属福利"感
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // 只在地下洞穴层生成（地表、地牢、丛林神庙不算）
            if (!spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDungeon)
                return 0f;

            // ── 事件期间：大幅提升 ─────────────────────────────
            // 南瓜月 / 霜月：最高优先级（10%），地下到处刷宝箱怪
            if (Main.pumpkinMoon || Main.snowMoon)
                return 0.10f;

            // 血月：氛围契合，大幅提升（8%）
            if (Main.bloodMoon)
                return 0.08f;

            // 哥布林入侵：事件加成（6%）
            if (Main.invasionType == InvasionID.GoblinArmy)
                return 0.06f;

            // 海盗入侵：事件加成（6%）
            if (Main.invasionType == InvasionID.PirateInvasion)
                return 0.06f;

            // ── 常规时段：基础概率 ─────────────────────────────
            float baseChance = 0.02f; // 默认 2%

            // 困难模式后提升（3%）
            if (Main.hardMode)
                baseChance = 0.03f;

            // 世纪之花后再提升（4%）
            if (NPC.downedPlantBoss)
                baseChance = 0.04f;

            // 月球领主后最高常驻（5%）
            if (NPC.downedMoonlord)
                baseChance = 0.05f;

            return baseChance;
        }
    }
}