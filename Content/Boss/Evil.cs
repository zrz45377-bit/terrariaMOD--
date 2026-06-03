using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.Boss
{
    // ==================== 阶段接口 ====================
    /// <summary>
    /// 所有"本体战斗阶段"必须实现这个接口。
    /// Evil 主类通过接口统一调用 Phase1~Phase4，不需要知道内部细节。
    /// 这样每个阶段可以独立写文件，互不干扰。
    /// </summary>
    internal interface IEvilPhase
    {
        void Enter(Evil boss, NPC npc);                 // 刚进入这个阶段时调用一次（初始化）
        void Update(Evil boss, NPC npc, Player player); // 每帧调用（写攻击、移动逻辑）
        void Exit(Evil boss, NPC npc);                  // 离开这个阶段时调用一次（清理）
        void HitEffect(Evil boss, NPC npc, NPC.HitInfo hit); // 本体被击中时调用
    }

    // ==================== 主Boss类 ====================
    /// <summary>
    /// Evil Boss 主控类。
    /// 负责：阶段切换、召唤仆从、本体无敌/隐形/瞬移、死亡处理。
    /// 真正的攻击逻辑交给 Phase1~Phase4 实现。
    /// </summary>
    [Autoload]
    internal class Evil : ModNPC
    {
        // ========== 图鉴注册 ==========
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement(essay_value())
            });
        }

        private string essay_value()
        {
            return "从被撕裂的梦境深处浮现的古老守卫。它本为镇压噩梦而铸，却在无尽的轮回侵蚀中成为了噩梦本身。" +
                "巨大的阴阳玉在其周身缓缓旋转，所过之处，现实与虚幻的边界被彻底抹除。任何试图以弹幕对抗它的挑战者，都将亲眼目睹自己的攻击归于虚无。";
        }

        // ---------- 8 个状态定义 ----------
        // 前 4 个是"召唤阶段"：Boss隐形无敌，召唤一对仆从，死完进下一阶段。
        // 后 4 个是"本体阶段"：Boss现身，由对应的 Phase 类控制战斗。
        private enum AIState
        {
            Summon1 = 0,   // 召唤第1对Boss
            Summon2 = 1,   // 召唤第2对Boss
            Summon3 = 2,   // 召唤第3对Boss
            Summon4 = 3,   // 召唤第4对Boss
            Phase1 = 4,    // 本体阶段1
            Phase2 = 5,    // 本体阶段2
            Phase3 = 6,    // 本体阶段3
            Phase4 = 7,    // 本体最终阶段
        }

        // ---------- 运行时缓存 ----------
        // currentPhase：当前激活的 Phase 实例（如 Phase1 对象）。
        // 注意：它是"内存里的临时对象"，不会随存档保存，所以每次读档/切换阶段都要重建。
        private IEvilPhase currentPhase;

        // lastState：上一帧的状态值，用来检测"状态是否变了"。
        // 如果 State 从 Summon4 变成 Phase1，说明要切换阶段，触发 Exit/Enter。
        private AIState lastState = (AIState)(-1);

        // ---------- ai 数组映射（关键！这些值会随存档保存） ----------
        // tModLoader 给每个 NPC 提供了 float[4] 的 ai 数组，用来存自定义数据。
        // 因为 NPC 实例在退出世界后会销毁，所以必须把"阶段、计时器"存进 ai[]，否则读档会丢失进度。
        private AIState State
        {
            get => (AIState)NPC.ai[0];   // ai[0] = 当前处于哪个状态（Summon1~Phase4）
            set => NPC.ai[0] = (float)value;
        }

        // ai[1] 全局计时器，每帧+1，给 Phase 类内部做时间判断用。
        private ref float Timer => ref NPC.ai[1];

        // ai[2] 当前状态的局部计时器（如 Phase1 里用来算攻击间隔）。
        private ref float StateTimer => ref NPC.ai[2];

        // ai[3] 召唤标记：0=还没召唤，1=已经召唤过了（防止每帧重复召唤）。
        private ref float Spawned => ref NPC.ai[3];

        // ---------- 仆从索引 ----------
        // servantLeft / servantRight：当前阶段召唤的两个仆从在 NPC 数组里的索引。
        // -1 表示"没有仆从"。用来检测仆从是否死亡。
        private int servantLeft = -1;
        private int servantRight = -1;

        // 本体正常状态下的接触伤害，召唤阶段会临时改成 0。
        private const int BaseDamage = 9999999;

        // ==================== 初始化属性 ====================
        public override void SetDefaults()
        {
            NPC.width = 100;             // 碰撞箱宽度（像素）
            NPC.height = 100;            // 碰撞箱高度（像素）
            NPC.damage = BaseDamage;    // 接触玩家时造成的伤害
            NPC.defense = 9999999;           // 防御力
            NPC.lifeMax = 10000;       // 总血量
            NPC.HitSound = SoundID.NPCHit1;     // 受击音效
            NPC.DeathSound = SoundID.NPCDeath1; // 死亡音效
            NPC.value = Item.buyPrice(0, 20, 0, 0); // 掉落金钱
            NPC.knockBackResist = 0f;   // 击退抗性（0=完全不被击退）
            NPC.aiStyle = -1;           // -1 表示不用原版AI，完全手写
            NPC.boss = true;            // 标记为Boss（影响音乐、小地图等）
            NPC.noGravity = true;       // 不受重力影响（飞行Boss）
            NPC.noTileCollide = true;   // 穿墙
            Music = MusicID.Boss2;      // 播放Boss2音乐
        }

        // ==================== 主AI循环（每帧执行） ====================
        public override void AI()
        {
            // 1. 锁定最近玩家。如果玩家死了，NPC.target 会指向一个无效对象。
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            // 2. 玩家离线或死亡：Boss向上飞，10秒后消失。
            if (!player.active || player.dead)
            {
                NPC.velocity.Y -= 0.1f;   // 持续向上加速
                if (NPC.timeLeft > 10) NPC.timeLeft = 10; // 10帧后自动 Despawn
                return;
            }

            // 3. 阶段切换检测（核心逻辑）
            // 如果 ai[0] 变了（比如从 Summon4 推进到 Phase1），说明要换阶段。
            // 先调用旧阶段的 Exit，再根据新状态创建对应的 Phase 对象，最后调用 Enter。
            if (State != lastState)
            {
                currentPhase?.Exit(this, NPC);          // 清理旧阶段
                currentPhase = CreatePhase(State);      // 创建新阶段实例
                currentPhase?.Enter(this, NPC);         // 新阶段初始化
                lastState = State;
            }

            // 4. 状态机分发：根据当前状态执行不同逻辑
            switch (State)
            {
                // ---------- 前4个状态：召唤阶段 ----------
                // 参数：玩家对象、下一阶段、左侧Boss类型、右侧Boss类型。
                // 这里全部用 NPCID.EyeofCthulhu 占位，后续替换成你的 Mod Boss。
                case AIState.Summon1:
                    UpdateSummon(player, AIState.Summon2,
                        NPCID.EyeofCthulhu,
                        NPCID.EyeofCthulhu);
                    break;

                case AIState.Summon2:
                    UpdateSummon(player, AIState.Summon3, NPCID.EyeofCthulhu, NPCID.EyeofCthulhu);
                    break;

                case AIState.Summon3:
                    UpdateSummon(player, AIState.Summon4, NPCID.EyeofCthulhu, NPCID.EyeofCthulhu);
                    break;

                case AIState.Summon4:
                    UpdateSummon(player, AIState.Phase1, NPCID.EyeofCthulhu, NPCID.EyeofCthulhu);
                    break;

                // ---------- 后4个状态：本体战斗 ----------
                // 解除无敌和隐形，把控制权交给 Phase 类。
                case AIState.Phase1:
                case AIState.Phase2:
                case AIState.Phase3:
                case AIState.Phase4:
                    NPC.dontTakeDamage = false; // 可以被打
                    NPC.alpha = 0;              // 完全可见
                    currentPhase?.Update(this, NPC, player); // 执行 Phase 里的攻击逻辑
                    break;
            }

            // 5. 全局计时器自增（供 Phase 类或特效使用）
            Timer++;
        }

        // ==================== 召唤阶段核心逻辑 ====================
        /// <summary>
        /// 处理 Summon1~Summon4 的通用逻辑：
        /// - 隐形 + 无敌 + 无接触伤害
        /// - 玩家离太远时瞬移
        /// - 召唤两个仆从
        /// - 仆从全灭后推进到下一阶段
        /// </summary>
        private void UpdateSummon(Player player, AIState nextState, int leftType, int rightType)
        {
            // ---------- 隐形 + 无敌 + 无伤害 ----------
            // alpha=255 表示完全透明（玩家看不见Boss）。
            // dontTakeDamage=true 表示玩家打它不掉血。
            // damage=0 表示即使玩家撞到Boss也不受伤。
            NPC.alpha = 255;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;

            // ---------- 召唤前：清场 ----------
            // 进入新的召唤阶段时，先杀死场上所有其他敌对生物，避免干扰。
            if (Spawned == 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];
                    if (other.active && !other.friendly && other.whoAmI != NPC.whoAmI)
                    {
                        // 直接移除，不触发死亡掉落/特效；如需触发死亡特效可改为 other.life = 0;
                        other.active = false;
                    }
                }
            }

            // ---------- 距离检测 + 瞬移 ----------
            // maxDist = 2400f 约等于 150 格（一格=16像素）。
            // 如果玩家跑太远（比如回家补血），Boss直接闪到玩家头顶。
            float maxDist = 2400f;
            float distToPlayer = Vector2.Distance(NPC.Center, player.Center);

            if (distToPlayer > maxDist)
            {
                // 直接修改 Center 就是瞬移，没有移动过程。
                NPC.Center = player.Center + new Vector2(0, -300);
                NPC.velocity = Vector2.Zero; // 瞬移后清空速度，防止惯性飞出。

                // 瞬移特效：生成 30 个 Shadowflame 粒子，向四周散开。
                for (int i = 0; i < 30; i++)
                {
                    Vector2 speed = Main.rand.NextVector2Circular(4f, 4f);
                    Dust d = Dust.NewDustPerfect(
                        NPC.Center + Main.rand.NextVector2Circular(40f, 40f), // 生成位置在Boss周围随机
                        DustID.Shadowflame,    // 粒子类型：暗影火
                        speed,
                        150,                   // 透明度
                        default,
                        1.8f                   // 粒子大小
                    );
                    d.noGravity = true;        // 粒子不受重力，飘一会儿消失
                }
            }

            // ---------- 正常悬停 ----------
            // 如果没触发瞬移，Boss 会慢慢飘到玩家头顶 -280 像素处。
            // 这样玩家始终能感觉到"有个东西在头顶召唤"，但又打不到它。
            Vector2 hover = player.Center + new Vector2(0, -280);
            Vector2 dir = hover - NPC.Center;

            if (dir.Length() > 10f)
            {
                dir.Normalize(); // 转成单位向量（只保留方向）
                NPC.velocity = Vector2.Lerp(NPC.velocity, dir * 3f, 0.04f); // 柔和加速
            }
            else
            {
                NPC.velocity *= 0.9f; // 到位置后减速悬停
            }

            // ---------- 召唤仆从（只执行一次） ----------
            // Spawned 存在 ai[3] 里，读档后不会重复召唤。
            if (Spawned == 0)
            {
                // 左侧仆从：在Boss左边 140 像素处生成。
                // NPC.NewNPC 返回生成后的 NPC 在 Main.npc[] 数组里的索引。
                servantLeft = NPC.NewNPC(
                    NPC.GetSource_FromAI(),          // 生成原因（用于掉落判定）
                    (int)(NPC.Center.X - 140),       // X坐标
                    (int)NPC.Center.Y,               // Y坐标
                    leftType,                        // NPC类型（这里用眼怪占位）
                    0,                               // Start（默认0）
                    0f, 0f, 0f, 0f,                  // ai[0]~ai[3] 初始值
                    NPC.target                       // 目标玩家索引（传给仆从）
                );

                // 右侧仆从：对称生成。
                servantRight = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)(NPC.Center.X + 140),
                    (int)NPC.Center.Y,
                    rightType,
                    0, 0f, 0f, 0f, 0f,
                    NPC.target
                );

                // 把主人的索引（whoAmI）存进仆从的 ai[3]。
                // 这样仆从自己可以通过 Main.npc[(int)ai[3]] 找到Boss，方便做联动。
                if (servantLeft >= 0) Main.npc[servantLeft].ai[3] = NPC.whoAmI;
                if (servantRight >= 0) Main.npc[servantRight].ai[3] = NPC.whoAmI;

                Spawned = 1;    // 标记"已召唤"
                StateTimer = 0; // 重置局部计时器
            }

            // ---------- 仆从死亡检测 ----------
            // 判断左侧仆从是否死亡：
            // 1. 索引 < 0（没生成成功）
            // 2. active == false（NPC被移除）
            // 3. life <= 0（血量归零）
            bool leftDead = servantLeft < 0 || !Main.npc[servantLeft].active || Main.npc[servantLeft].life <= 0;
            bool rightDead = servantRight < 0 || !Main.npc[servantRight].active || Main.npc[servantRight].life <= 0;

            // 两个都死了，推进到下一阶段。
            if (Spawned == 1 && leftDead && rightDead)
            {
                State = nextState;   // 修改 ai[0]，触发阶段切换
                Spawned = 0;         // 重置召唤标记，为下一阶段做准备
                StateTimer = 0;
                servantLeft = -1;    // 清空索引，防止误检测
                servantRight = -1;

                // 恢复伤害，下一阶段现身时玩家撞到Boss会受伤。
                NPC.damage = BaseDamage;
            }
        }

        // ==================== 阶段工厂 ====================
        /// <summary>
        /// 根据状态枚举创建对应的 Phase 实例。
        /// 如果 Summon 阶段调用这个方法会返回 null（Summon 不走 Phase 接口）。
        /// </summary>
        private IEvilPhase CreatePhase(AIState state)
        {
            return state switch
            {
                AIState.Phase1 => new Phase1(),
                AIState.Phase2 => new Phase2(),
                AIState.Phase3 => new Phase3(),
                AIState.Phase4 => new Phase4(),
                _ => null
            };
        }

        // ==================== 受击处理 ====================
        public override void HitEffect(NPC.HitInfo hit)
        {
            // 如果是本体阶段，把受击事件转发给当前 Phase（比如用来统计受伤次数）。
            currentPhase?.HitEffect(this, NPC, hit);

            // 血量归零时的死亡粒子。
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 40; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection * 3, -3f);
            }
        }

        // ==================== 死亡处理 ====================
        public override void OnKill()
        {
            // ---------- 死亡时：清场 ----------
            // Boss 本体死亡时，杀死场上所有其他敌对生物。
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && !other.friendly && other.whoAmI != NPC.whoAmI)
                {
                    other.active = false;
                }
            }

            // Boss本体死亡时，在尸体左右各召唤一个最终Boss。
            // 这里同样用克苏鲁之眼占位，后续替换成你的 Mod NPC。
            int finalLeft = NPC.NewNPC(
                NPC.GetSource_Death(),    // 死亡时生成，用于掉落判定
                (int)(NPC.Center.X - 100),
                (int)NPC.Center.Y,
                NPCID.EyeofCthulhu,
                0, 0f, 0f, 0f, 0f,
                NPC.target
            );

            int finalRight = NPC.NewNPC(
                NPC.GetSource_Death(),
                (int)(NPC.Center.X + 100),
                (int)NPC.Center.Y,
                NPCID.EyeofCthulhu,
                0, 0f, 0f, 0f, 0f,
                NPC.target
            );

            // 可选：给最终Boss加血量，让它更难打。
            if (finalLeft >= 0) { Main.npc[finalLeft].lifeMax = 50000; Main.npc[finalLeft].life = 50000; }
            if (finalRight >= 0) { Main.npc[finalRight].lifeMax = 50000; Main.npc[finalRight].life = 50000; }

            base.OnKill();
        }
    }

    // ==================== Phase1 空模板 ====================
    /// <summary>
    /// 本体阶段1。
    /// 目前只有基础悬停，后续在这里写攻击逻辑（发射弹幕、冲刺等）。
    /// </summary>
    internal class Phase1 : IEvilPhase
    {
        // 进入阶段时触发一次。可以用来播放动画、重置计时器、喊话。
        public void Enter(Evil boss, NPC npc) { }

        // 每帧执行。npc 就是 Evil 本体，直接修改它的 velocity/position。
        public void Update(Evil boss, NPC npc, Player player)
        {
            // 示例：保持在玩家头顶 220 像素处悬停。
            // 写完自己的攻击逻辑后，可以删掉或替换这段。
            Vector2 target = player.Center + new Vector2(0, -220);
            Vector2 dir = target - npc.Center;

            if (dir.Length() > 10f)
            {
                dir.Normalize();
                npc.velocity = Vector2.Lerp(npc.velocity, dir * 5f, 0.05f);
            }
        }

        // 离开阶段时触发一次。清理临时数据（如弹幕、特效）。
        public void Exit(Evil boss, NPC npc) { }

        // 本体被击中时触发。可以用来做"受伤 N 次后切换攻击模式"的逻辑。
        public void HitEffect(Evil boss, NPC npc, NPC.HitInfo hit) { }
    }

    // ==================== Phase2 空模板 ====================
    internal class Phase2 : IEvilPhase
    {
        public void Enter(Evil boss, NPC npc) { }
        public void Update(Evil boss, NPC npc, Player player)
        {
            // TODO: 阶段2攻击逻辑
        }
        public void Exit(Evil boss, NPC npc) { }
        public void HitEffect(Evil boss, NPC npc, NPC.HitInfo hit) { }
    }

    // ==================== Phase3 空模板 ====================
    internal class Phase3 : IEvilPhase
    {
        public void Enter(Evil boss, NPC npc) { }
        public void Update(Evil boss, NPC npc, Player player)
        {
            // TODO: 阶段3攻击逻辑
        }
        public void Exit(Evil boss, NPC npc) { }
        public void HitEffect(Evil boss, NPC npc, NPC.HitInfo hit) { }
    }

    // ==================== Phase4 空模板（最终狂暴） ====================
    internal class Phase4 : IEvilPhase
    {
        public void Enter(Evil boss, NPC npc) { }
        public void Update(Evil boss, NPC npc, Player player)
        {
            // TODO: 最终阶段逻辑（通常更激进，比如高速追踪）
        }
        public void Exit(Evil boss, NPC npc) { }
        public void HitEffect(Evil boss, NPC npc, NPC.HitInfo hit) { }
    }
}