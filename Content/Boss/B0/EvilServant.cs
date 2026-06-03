using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.Boss
{
    /// <summary>
    /// Evil 召唤出的仆从 Boss 基类。
    /// 封装了：共同属性、主人追踪、开场动画（隐形→显现+旋转）、死亡联动。
    /// 子类只需重写 ServantAI() 写自己的攻击/移动。
    /// </summary>
    internal abstract class EvilServant : ModNPC
    {
        // ==================== 主人快捷访问 ====================
        // ai[3] 由 Evil 在召唤时写入 = 主人的 whoAmI
        protected int MasterIndex => (int)NPC.ai[3];
        protected NPC Master => MasterIndex >= 0 && Main.npc[MasterIndex].active ? Main.npc[MasterIndex] : null;
        protected bool HasMaster => Master != null;

        // ==================== 开场动画字段 ====================
        // 注意：这是运行时内存字段，退出世界后重置。
        // 重新加载游戏时会再播放一次开场（90帧），不影响功能。
        private int spawnAnimTimer = 0;         // 开场计时器
        private const int SpawnAnimDuration = 90; // 开场持续 90 帧（1.5秒）
        private float spawnRotationDir = 1f;      // 旋转方向：1=顺时针，-1=逆时针

        // ==================== 共同属性 ====================
        public override void SetDefaults()
        {
            NPC.width = 80;
            NPC.height = 80;
            NPC.damage = 50;
            NPC.defense = 20;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;       // 完全自定义
            NPC.boss = true;        // 标记为 Boss（影响小地图等）
            NPC.noGravity = true;
            NPC.noTileCollide = true;

            // 初始状态：完全隐形，等待开场动画显现
            NPC.alpha = 255;
        }

        // ==================== 主 AI 循环 ====================
        public override void AI()
        {
            // 1. 锁定最近玩家
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            // 2. 玩家死亡或离线：向上飞走消失
            if (!player.active || player.dead)
            {
                NPC.velocity.Y -= 0.1f;
                if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                return;
            }

            // 3. ========== 开场动画（生成后只执行一次） ==========
            // 第一帧：随机决定这次出场的旋转方向
            if (spawnAnimTimer == 0)
            {
                spawnRotationDir = Main.rand.NextBool() ? 1f : -1f;
            }

            // 开场期间（90帧）：只执行显现+旋转，不移动、不攻击
            if (spawnAnimTimer < SpawnAnimDuration)
            {
                // --- 3.1 隐形 → 显现（alpha 255 线性渐变到 0）---
                float progress = spawnAnimTimer / (float)SpawnAnimDuration;
                NPC.alpha = (int)(255 * (1f - progress)); // 第0帧=255，第90帧=0

                // --- 3.2 不断旋转 ---
                // 前 30 帧：旋转加速（从慢到快）
                // 后 60 帧：匀速快速旋转
                float rotSpeed;
                if (spawnAnimTimer < 30)
                    rotSpeed = spawnAnimTimer * 0.025f; // 0 -> 0.75
                else
                    rotSpeed = 0.75f;

                NPC.rotation += rotSpeed * spawnRotationDir;

                // --- 3.3 开场期间原地悬停（不要到处飞）---
                NPC.velocity *= 0.82f;

                // 开场计时器自增，然后直接 return，跳过子类 AI
                spawnAnimTimer++;
                return;
            }
            else
            {
                // 开场结束后确保完全实体化
                NPC.alpha = 0;
            }

            // 4. 主人死亡检测（子类可通过 HasMaster 读取）
            if (!HasMaster)
            {
                // 基类不做处理，留给子类通过 HasMaster 判断狂暴逻辑
            }

            // 5. 调用子类专属 AI（开场结束后才执行）
            ServantAI(player);

            // 6. 速度上限保险（防止意外飞出世界）
            if (NPC.velocity.Length() > 16f)
            {
                NPC.velocity = Vector2.Normalize(NPC.velocity) * 16f;
            }
        }

        // ==================== 子类必须实现 ====================
        /// <summary>
        /// 子类只需要实现这个方法，写自己的攻击和移动逻辑。
        /// 注意：开场动画期间此方法不会被调用。
        /// </summary>
        protected abstract void ServantAI(Player player);

        // ==================== 受击与死亡 ====================
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection * 2, -2f);
                }
            }
        }

        // ==================== 绘图 ====================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 默认使用原版绘制。
            // 如果需要发光、残影、Shader，子类可 override 此方法。
            return true;
        }
    }
}