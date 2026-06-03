using System;
using TEACHER.Content.ITEM_S.N0;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.ITEM_S.N7
{
    /// <summary>
    /// 【月球的遗失之眼】
    /// 
    /// 装备类型：纯数值加成类饰品
    /// 阶段定位：月球领主前（拜月教邪教徒阶段）
    /// 主题：月都战场 / 兔子眼睛 / 顶级掠食者
    /// 
    /// 加成方向：
    /// - 远程伤害 +20%（邪教徒阶段射手核心提升）
    /// - 远程暴击 +12%（注视弱点，精准猎杀）
    /// - 移动速度 +10%（掠食者的追捕本能）
    /// 
    /// 合成材料获取时期：
    /// - 狙击镜：石巨人后（世纪之花后地牢+机械Boss）
    /// - 灵气：世纪之花后地牢幽灵怪
    /// - 视域之魂：毁灭者/机械Boss
    /// 全部可在邪教徒前获取，于工匠作坊合成
    /// </summary>
    public class LunarLostEye : ModItem
    {
        public override void SetDefaults()
        {
            // ---------- 物品基础属性 ----------
            Item.width = 80;                    // 贴图宽度
            Item.height = 26;                   // 贴图高度
            Item.accessory = true;              // 标记为饰品

            // 品质与价值（邪教徒阶段 = 黄色/青色品质区间）
            Item.rare = ItemRarityID.Yellow;    // 黄色品质（石巨人后~邪教徒前）
            Item.value = Item.buyPrice(0, 5, 0, 0);
        }

        // ========== 核心：邪教徒阶段数值加成 ==========
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // --- 1. 远程伤害 +20% ---
            // 邪教徒阶段射手毕业饰品标准：略高于狙击镜（+10%），低于天界壳（+10%全+狼人）
            // 20%是自定义饰品在该阶段的合理上限，不会破坏四柱挑战难度
            player.GetDamage(DamageClass.Ranged) += 0.25f;

            // --- 2. 远程暴击 +12% ---
            // 与毁灭者徽章（+8%全暴击）和狙击镜（+10%远程暴击）形成叠加优势
            // 12%是专属远程饰品的合理溢价
            player.GetCritChance(DamageClass.Ranged) += 20;

            // --- 3. 移动速度 +10% ---
            // 邪教徒/四柱阶段需要大量走位，10%移速是生存向刚需
            // 不与翅膀速度直接叠加，但地面机动性显著提升
            player.moveSpeed += 0.50f;
        }

        // ========== 合成配方（邪教徒前可获取） ==========
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // 基底：狙击镜（石巨人后射手核心，象征"眼睛/注视"）
            recipe.AddIngredient(ItemID.SniperScope).AddIngredient(ModContent.ItemType<Humanity>(), 20);  // 人性

            // 地牢灵气（世纪之花后地牢幽灵怪掉落，狂气/幽灵主题）
            recipe.AddIngredient(ItemID.Ectoplasm, 8);

            // 视域之魂（机械Boss毁灭者掉落，纯粹的眼睛意象）
            recipe.AddIngredient(ItemID.SoulofSight, 5);

            // 合成站：工匠作坊（石巨人后阶段已可获取）
            recipe.AddTile(TileID.TinkerersWorkbench);

            recipe.Register();
        }
    }
}