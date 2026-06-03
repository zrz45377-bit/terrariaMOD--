using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N10
{
    /// <summary>
    /// 冈格尼尔之矛（Gungnir）物品类
    /// 双模式武器：mode 0 为近战伸缩长矛，mode 1 为投掷长矛
    /// </summary>
    public class GungnirSpear : ModItem
    {
        /// <summary>当前武器模式：0=近战，1=投掷。切换后由 HoldItem 实时同步到物品属性</summary>
        public int mode = 0;

        public override void SetDefaults()
        {
            // 物品贴图碰撞箱
            Item.width = 116;
            Item.height = 116;

            // 基础伤害与类型
            Item.damage = 10000;
            Item.DamageType = DamageClass.Melee;

            // 使用动画时长（帧）。useTime 在 HoldItem 中会根据模式动态覆盖，
            // 这里先给一个安全的默认值，避免切换前出现意外快速连发。
            Item.useAnimation = 22;
            Item.useTime = 22;

            // 使用方式：Shoot 表示发射弹幕，具体动作由 HoldItem 动态覆盖
            Item.useStyle = ItemUseStyleID.Shoot;

            // 物品本体不造成近战碰撞伤害，伤害完全由弹幕承担
            Item.noMelee = true;

            Item.knockBack = 6.4f;

            // 弹幕初速度，近战模式下仅影响弹幕生成时的 velocity 传递
            Item.shootSpeed = 5.6f;

            // 售价与稀有度
            Item.value = Item.sellPrice(0, 4, 60, 0);
            Item.rare = ItemRarityID.Pink;

            // 默认关闭自动挥舞，避免误触
            Item.autoReuse = false;

            // 默认发射近战弹幕
            Item.shoot = ModContent.ProjectileType<GungnirMeleeProj>();
        }

        /// <summary>
        /// 手持时每帧更新物品属性。
        /// 这是双模式切换的核心：根据 mode 实时重写 useStyle、useTime、shoot 等字段，
        /// 让玩家在手中看到并使用的是当前模式的参数。
        /// </summary>
        public override void HoldItem(Player player)
        {
            if (mode == 0)
            {
                // ── 近战伸缩矛模式 ──
                Item.useStyle = ItemUseStyleID.Shoot;       // 发射弹幕式使用
                // 【修改1】useTime 与 useAnimation 保持一致，防止动画未结束就允许下次使用
                Item.useTime = 22;
                Item.useAnimation = 22;
                Item.shootSpeed = 5.6f;                     // 低初速（近战弹幕不依赖此值飞行）
                Item.noUseGraphic = true;                   // 隐藏物品贴图，由弹幕（长矛）代替绘制
                Item.autoReuse = false;                     // 手动点击
                Item.shoot = ModContent.ProjectileType<GungnirMeleeProj>();
            }
            else
            {
                // ── 投掷模式 ──
                Item.useStyle = ItemUseStyleID.Swing;       // 挥动动作，配合投掷手感
                Item.useTime = 100;                         // 长前摇/后摇，体现重型投掷
                Item.useAnimation = 100;
                Item.shootSpeed = 50f;                      // 高初速，配合 ModifyShootStats 实际发射
                Item.noUseGraphic = true;                   // 同样隐藏本体，由投掷弹幕绘制
                Item.autoReuse = false;                     // 手动投掷
                Item.shoot = ModContent.ProjectileType<GungnirThrownProj>();
            }
        }

        /// <summary>允许右键（AltFunctionUse）使用此物品</summary>
        public override bool AltFunctionUse(Player player) => true;

        /// <summary>
        /// 使用前的条件判断。
        /// 右键（altFunctionUse == 2）时执行模式切换，不真正消耗本次使用；
        /// 左键则正常放行，由 HoldItem 已同步好的属性进行发射。
        /// 
        /// 【修改2】近战模式下额外检查：如果玩家已存在存活的 GungnirMeleeProj，
        /// 则阻止本次使用，彻底避免快速点击导致的多矛重叠问题。
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            // ── 右键切换模式 ──
            if (player.altFunctionUse == 2)
            {
                mode = mode == 0 ? 1 : 0;

                // 播放切换音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick, player.Center);

                // 头顶弹出模式提示文字
                string text = mode == 0 ? "近战模式" : "投掷模式";
                CombatText.NewText(player.getRect(), mode == 0 ? Color.Red : Color.DarkRed, text, true);

                // 生成切换特效粒子
                for (int i = 0; i < 8; i++)
                {
                    Vector2 speed = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 2f;
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.MagicMirror, speed, 100, mode == 0 ? Color.Yellow : Color.Cyan, 1.2f);
                    d.noGravity = true;
                }

                // 返回 false 阻止本次右键被当作“使用武器”消耗
                return false;
            }

            // ── 左键发射检查 ──
            if (mode == 0)
            {
                int meleeProjType = ModContent.ProjectileType<GungnirMeleeProj>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == meleeProjType && p.owner == player.whoAmI)
                    {
                        // 已有近战矛在场，禁止再次发射
                        return false;
                    }
                }
            }

            return base.CanUseItem(player);
        }

        /// <summary>
        /// 发射前最后修正弹幕参数。
        /// 投掷模式下强制指定弹幕类型并锁定速度为 18f；
        /// 近战模式则保持默认的 GungnirMeleeProj 由弹幕自身 AI 处理方向。
        /// </summary>
        public override void ModifyShootStats(
            Player player,
            ref Vector2 position,
            ref Vector2 velocity,
            ref int type,
            ref int damage,
            ref float knockback)
        {
            if (mode == 1)
            {
                // 投掷模式：强制替换弹幕类型，并归一化方向后赋予固定飞行速度
                type = ModContent.ProjectileType<GungnirThrownProj>();
                velocity = velocity.SafeNormalize(Vector2.UnitX) * 18f;
            }
            else
            {
                // 近战模式：保持原近战弹幕，velocity 由弹幕 AI 在首次更新时根据鼠标重新计算并锁定
                type = ModContent.ProjectileType<GungnirMeleeProj>();
            }
        }
    }
}