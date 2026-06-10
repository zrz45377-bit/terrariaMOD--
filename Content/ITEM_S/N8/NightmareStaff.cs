using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TEACHER.Content.ITEM_S.N8
{
    public class NightmareStaff : ModItem
    {
        // 六种宝石矢 Projectile（对应原版宝石法杖弹幕）
        private static readonly int[] GemProjs = new int[]
        {
            ProjectileID.AmethystBolt,   // 紫晶
            ProjectileID.TopazBolt,      // 黄玉
            ProjectileID.SapphireBolt,   // 蓝玉
            ProjectileID.EmeraldBolt,    // 翡翠
            ProjectileID.RubyBolt,       // 红玉
            ProjectileID.DiamondBolt,    // 钻石
        };

        // 模式提示
        private static readonly string[] ModeNames = new string[]
        {
            "紫晶模式 [贯穿]",
            "黄玉模式 [散射]",
            "蓝玉模式 [霜冻]",
            "翡翠模式 [剧毒]",
            "红玉模式 [吸血]",
            "钻石模式 [爆裂]",
            "传送模式 [瞬移]"
        };

        private static readonly Color[] ModeColors = new Color[]
        {
            Color.Purple, Color.Gold, Color.Cyan,
            Color.LimeGreen, Color.Red, Color.White, Color.Gray
        };

        public override void SetStaticDefaults()
        {
            // 提示在 .hjson 里写
        }

        public override void SetDefaults()
        {
            Item.damage = 180;              // 月后毕业伤害
            Item.DamageType = DamageClass.Magic;
            Item.width = 44;
            Item.height = 44;
            Item.useTime = 7;               // 极快射速
            Item.useAnimation = 7;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = Item.sellPrice(0, 25, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.AmethystBolt;
            Item.shootSpeed = 18f;
            Item.mana = 0;                  // 不耗魔！
        }

        // 允许右键使用
        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            var g = Item.GetGlobalItem<NightmareStaffGlobalItem>();

            // ========== 右键：循环切换 7 种模式 ==========
            if (player.altFunctionUse == 2)
            {
                g.Mode++;
                if (g.Mode >= 7) g.Mode = 0;

                // 客户端特效：只在客户端执行
                if (Main.netMode != NetmodeID.Server)
                {
                    // 头顶弹出模式名
                    CombatText.NewText(player.getRect(), ModeColors[g.Mode],
                        ModeNames[g.Mode], true);

                    SoundEngine.PlaySound(SoundID.Item4, player.Center);

                    // 切换粒子
                    for (int i = 0; i < 12; i++)
                    {
                        Dust d = Dust.NewDustPerfect(player.Center, DustID.RainbowTorch,
                            Main.rand.NextVector2Circular(3f, 3f), 0,
                            ModeColors[g.Mode], 1.2f);
                        d.noGravity = true;
                    }
                }

                return false; // 右键只切换，不触发使用
            }

            return true;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity,
            ref int type, ref int damage, ref float knockback)
        {
            int mode = Item.GetGlobalItem<NightmareStaffGlobalItem>().Mode;

            if (mode < 6) // 攻击模式
            {
                type = GemProjs[mode];
                damage = Item.damage + mode * 15; // 钻石模式 255 伤
            }
            else // 传送模式
            {
                type = ProjectileID.None; // 不射弹幕
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = Item.GetGlobalItem<NightmareStaffGlobalItem>().Mode;

            // ========== 模式 6：传送 ==========
            if (mode == 6)
            {
                Vector2 target = Main.MouseWorld;

                // 直接传送：无混沌状态、无冷却、无消耗
                player.Teleport(target, 1);
                player.velocity = Vector2.Zero; // 重置速度，防止惯性飞出

                // 多人同步
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null,
                        0, player.whoAmI, target.X, target.Y, 1);
                }

                // 传送特效：起点紫雾，落点粉雾
                SoundEngine.PlaySound(SoundID.Item8, player.Center);
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDustPerfect(player.Center, DustID.MagicMirror,
                        Main.rand.NextVector2Circular(4f, 4f), 0, Color.Purple, 1.5f).noGravity = true;
                }
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDustPerfect(target, DustID.MagicMirror,
                        Main.rand.NextVector2Circular(4f, 4f), 0, Color.Pink, 1.5f).noGravity = true;
                }

                return false;
            }

            // ========== 模式 0~5：六种宝石矢 ==========
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            Projectile p = Main.projectile[proj];

            // 根据当前模式赋予弹幕额外特性
            switch (mode)
            {
                case 0: // 紫晶：高穿透
                    p.penetrate = 8;
                    p.usesLocalNPCImmunity = true;
                    p.localNPCHitCooldown = 8;
                    break;

                case 1: // 黄玉：散射三发
                    p.timeLeft = 240; // 主弹幕也统一寿命
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 splitVel = velocity.RotatedBy(MathHelper.ToRadians(14 * i));
                        int p2 = Projectile.NewProjectile(source, position, splitVel, type,
                            damage * 2 / 3, knockback, player.whoAmI);
                        Main.projectile[p2].timeLeft = 240;
                    }
                    break;

                case 2: // 蓝玉：霜冻减速
                    p.GetGlobalProjectile<NightmareStaffGlobalProjectile>().ApplyFrostburn = true;
                    p.extraUpdates = 1; // 更快
                    break;

                case 3: // 翡翠：剧毒
                    p.GetGlobalProjectile<NightmareStaffGlobalProjectile>().ApplyVenom = true;
                    p.penetrate = 4;
                    break;

                case 4: // 红玉：吸血
                    p.GetGlobalProjectile<NightmareStaffGlobalProjectile>().LifeSteal = true;
                    p.penetrate = 3;
                    break;

                case 5: // 钻石：命中爆裂
                    p.GetGlobalProjectile<NightmareStaffGlobalProjectile>().ExplodeOnHit = true;
                    p.penetrate = 5;
                    p.scale = 1.3f;
                    break;
            }

            return false; // 手动发射完毕
        }

        // 持有时的彩虹光效
        public override void HoldItem(Player player)
        {
            if (Main.rand.NextBool(3))
            {
                float hue = (Main.GameUpdateCount % 60) / 60f;
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(0, -20),
                    DustID.RainbowTorch, new Vector2(0, -1f), 0,
                    Main.hslToRgb(hue, 1f, 0.7f), 0.9f);
                d.noGravity = true;
            }
        }

        // ========== 复杂合成 ==========
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.RodofDiscord)           // 和谐传送杖
                .AddIngredient(ItemID.RainbowCrystalStaff, 1) // 彩虹水晶法杖
                .AddIngredient(ItemID.LunarBar, 20)           // 夜明锭
                .AddIngredient(ItemID.FragmentNebula, 15)     // 星云碎片
                .AddIngredient(ItemID.Amethyst, 20)
                .AddIngredient(ItemID.Topaz, 20)
                .AddIngredient(ItemID.Sapphire, 20)
                .AddIngredient(ItemID.Emerald, 20)
                .AddIngredient(ItemID.Ruby, 20)
                .AddIngredient(ItemID.Diamond, 20)
                .AddIngredient(ItemID.TorchGodsFavor, 1)
                .AddTile(TileID.LunarCraftingStation)         // 远古操纵机
                .Register();
        }
    }

    // ========== 全局物品：每个 NightmareStaff 实例独立保存模式 ==========
    public class NightmareStaffGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        public int Mode = 0;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ModContent.ItemType<NightmareStaff>();
        }

        public override void SaveData(Item item, TagCompound tag)
        {
            tag["nsm"] = Mode;
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            Mode = tag.GetInt("nsm");
        }
    }

    // ========== 全局弹幕：处理宝石矢额外特效 ==========
    public class NightmareStaffGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool ApplyFrostburn = false;
        public bool ApplyVenom = false;
        public bool LifeSteal = false;
        public bool ExplodeOnHit = false;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (ApplyFrostburn)
                target.AddBuff(BuffID.Frostburn, 180); // 3秒霜冻

            if (ApplyVenom)
                target.AddBuff(BuffID.Venom, 300);  // 5秒剧毒

            if (LifeSteal && projectile.owner == Main.myPlayer && damageDone > 0)
            {
                Player player = Main.player[projectile.owner];
                int heal = damageDone / 8; // 12.5% 吸血
                if (heal > 0)
                {
                    player.statLife = Math.Min(player.statLife + heal, player.statLifeMax2);
                    player.HealEffect(heal);
                }
            }

            if (ExplodeOnHit)
            {
                // 钻石爆裂：小型粒子爆
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.GemDiamond,
                        Main.rand.NextVector2Circular(6f, 6f), 0, Color.White, 1.8f);
                    d.noGravity = true;
                }
                // 范围伤害（小型 AOE）
                if (projectile.owner == Main.myPlayer)
                {
                    Player player = Main.player[projectile.owner];
                    int aoeDmg = damageDone / 2;
                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.immortal == false
                            && npc.Distance(target.Center) < 80f)
                        {
                            if (npc.whoAmI != target.whoAmI)
                                npc.SimpleStrikeNPC(aoeDmg, projectile.direction, false, 0, DamageClass.Magic);
                        }
                    }
                }
            }
        }
    }
}