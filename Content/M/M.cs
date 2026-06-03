using System;
using TEACHER.Content.Systems;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.M
{
    public class M : ModNPC
    {

        //普通 怪物
        //1.几乎不变的属性
        //获取状态[0->6]
        public int stage_s;
            // ========== 基础属性设置 ==========
            public override void SetDefaults()
            {
                NPC.lifeMax = 50;                 // 最大生命值
                NPC.damage = 1;                    // 接触伤害（对玩家造成的伤害）
                NPC.defense = 1;                   // 防御力（减少受到的伤害）
                NPC.boss = false;                    // 是否为Boss（影响音乐、死亡消息等）
                NPC.npcSlots = 10f;                  // 占用NPC槽位（影响刷怪）
                NPC.knockBackResist = 0;         // 击退抗性（0=无击退，1=全额击退）
                NPC.lavaImmune = true;              // 是否免疫岩浆伤害
                SetDefaults_Change();
            }
            //AI
            public override void AI()
            {
                AI_Change();
            }
            // 【可选】检查生成条件（自然生成时）
            public override float SpawnChance(NPCSpawnInfo spawnInfo)
            {
                return SpawnChance_Change(spawnInfo);
            }
            // 【可选】修改掉落物
            public override void ModifyNPCLoot(NPCLoot npcLoot)
            {
                ModifyNPCLoot_Change(npcLoot);
            }
            // 【可选】死亡时调用
            public override void OnKill()
            {
                OnKill_Change();
            }
            /// <summary>
            /// 设置图鉴（Bestiary）信息
            /// 玩家可以在游戏中的生物图鉴查看这些信息
            /// </summary>
            public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
            {
                bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] 
                {   
                    // FlavorText：背景故事描述
                    new FlavorTextBestiaryInfoElement(
                        essay_value()
                        )
                });
            }
        //2.要调整的属性 【NPC 一定要修改的东西】
            //1.基础
            public virtual void SetDefaults_Change()
            {
                // // 【必须】NPC基础标识
                    NPC.width = 40;                    // NPC碰撞箱宽度（像素）
                    NPC.height = 40;                   // NPC碰撞箱高度（像素）
                    // 【必须】AI与行为
                    NPC.aiStyle = -1;                   // AI样式ID，-1表示使用自定义AI（配合AI()方法）
                    //【可选】
                    NPC.noGravity = false;               // 是否不受重力影响（飞行NPC）
                    NPC.noTileCollide = false;          // 是否穿透方块（true=穿墙）
                    NPC.canGhostHeal = true;            // 幽灵盔甲是否能治疗此NPC
                    // 【可选】声音
                    NPC.HitSound = SoundID.NPCHit1;     // 受击音效
                    NPC.DeathSound = SoundID.NPCDeath1; // 死亡音效
                    // 【可选】其他
                    NPC.SpawnWithHigherTime(30);        // 生成时增加时间计数（防止立即消失）
                    NPC.scale = 1f;                     // 缩放比例（1=正常大小）
            }
        //文案修改
        public virtual String essay_value()
        {
            return
                "这种生物能够扭曲周围的空间结构，将世界各地的杂物吸入体内。击败它后，被压缩的次元裂隙会破裂，将储存的物品释放到现实中。" +
                "\n\n学者们认为这是一种原始的次元袋现象，而非真正的消化系统。";
        }
        //AI
        public virtual void AI_Change()
            {
                
            }
            //生成率
            // 在 M.cs 里，把原来的 SpawnChance_Change 改成读取配置：

            public virtual float SpawnChance_Change(NPCSpawnInfo spawnInfo)
            {
                // 【核心】如果配置面板里没开开关，生成率强制归零
                if (!ModContent.GetInstance < TEACHERConfig > ().EnableRandomDropSpawner)
                    return 0f;

                // 只在夜晚 + 地表 + 非神圣/腐化/猩红地区生成
                if (spawnInfo.Player.ZoneOverworldHeight &&
                    !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
                {
                    return 0.07f;
                }
                return 0f;
            }
        //凋落物
        public virtual void ModifyNPCLoot_Change(NPCLoot npcLoot)
            {
                
            }
            // 【可选】死亡时调用
            public virtual void OnKill_Change()
            {
            // 从所有可用物品中随机掉落（包括MOD物品）
                for (int i = 0; i < 2; i++)
                {
                    int randomItemId = Main.rand.Next(1, ItemLoader.ItemCount);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Hitbox, randomItemId, Main.rand.Next(1, 3));
                }
            }
        //3.不知道是什么鬼
             // 【可选】受到攻击时
            public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
            {
                // 被近战武器击中时的反应
            }
            public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
            {
                // 被弹幕击中时的反应
            }
            // 【可选】与玩家接触时
            public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
            {
                // 给玩家施加debuff
                // target.AddBuff(BuffID.OnFire, 300); // 着火5秒
            }
            // 【可选】设置Boss血条信息
            public override void BossHeadSlot(ref int index)
            {
                // 自定义Boss头像槽位（如有多阶段形态）
                // index = ModContent.GetModBossHeadSlot("ForgottenLand/Content/Basis_Head_Boss");
            }
            // 【可选】设置Boss血条旋转（用于长形Boss如蠕虫）
            public override void BossHeadRotation(ref float rotation)
            {
                // rotation = NPC.rotation;
            }
    }
}