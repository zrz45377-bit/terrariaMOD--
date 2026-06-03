using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content.Items
{
    /// <summary>
    /// BASIS（噩梦构造体/阴阳玉发射器）
    /// 
    /// 武器特性：
    /// - 极慢攻速（80帧≈1.3秒），高单发伤害
    /// - 发射巨大的噩梦阴阳玉，穿透敌人与玩家
    /// - 弹幕碰墙反弹，20秒后自动爆炸清屏
    /// - 爆炸会伤害范围内所有存在（包括使用者自己）
    /// </summary>
    public class BASIS : ModItem
    {
        // ========== 核心参数 ==========
        // Basis_number：武器基础帧数，同时影响攻速和伤害倍率
        // 80帧 = 1.33秒攻击间隔，伤害 = 80 × 120 = 9600
        // 改这个数字可以同时调整手感和威力
        int Basis_number = 80;

        public override void SetDefaults()
        {
            // ---------- 基础属性 ----------
            Item.damage = Basis_number * 120;   // 动态计算面板伤害（80×120=9600）
            Item.DamageType = DamageClass.Melee; // 归类为近战（享受近战加成，但弹幕实际行为由Projectile决定）

            // 物品贴图尺寸（背包里显示的大小）
            Item.width = 40;
            Item.height = 40;

            // ---------- 使用动画 ----------
            Item.useTime = Basis_number;        // 实际攻击冷却：80帧（约1.33秒）
            Item.useAnimation = Basis_number;   // 挥动动画播放时长，和useTime同步
            Item.useStyle = ItemUseStyleID.Swing; // 挥砍动画（虽然是远程武器，但保留近战手感）

            Item.knockBack = 6;                 // 弹幕附带击退力
            Item.value = Item.buyPrice(gold: 1); // NPC售价1金
            Item.rare = ItemRarityID.Blue;      // 蓝色品质（前期可获得）

            // ---------- 音效与自动连发 ----------
            Item.UseSound = SoundID.Item1;       // 标准挥砍音效
            Item.autoReuse = true;              // 按住鼠标自动连续攻击

            // ---------- 弹幕发射 ----------
            Item.shoot = ModContent.ProjectileType<YinYangOrb>(); // 发射自定义阴阳玉弹幕
            Item.shootSpeed = 14f;              // 弹幕初速度（像素/帧）
            Item.noMelee = true;                // 挥动动画本身不造成伤害，全靠弹幕
        }

        // ========== 核心：修改弹幕生成位置 ==========
        /// <summary>
        /// 在引擎生成弹幕前拦截，强制把发射点从玩家中心沿鼠标方向推出150像素。
        /// 原因：阴阳玉贴图120×120，如果直接在玩家中心生成，会有一半卡在身体里。
        /// </summary>
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // velocity 的方向就是鼠标指向的方向（引擎已经根据鼠标位置算好了）
            // 把它归一化成单位向量（长度变为1，只保留方向）
            Vector2 direction = Vector2.Normalize(velocity);

            // 如果因为某些原因 velocity 是零向量（极少见），用玩家面朝方向兜底
            // player.direction 的值：1 = 面朝右，-1 = 面朝左
            if (direction == Vector2.Zero)
            {
                direction = new Vector2(player.direction, 0f);
            }

            // 把生成点从玩家中心，沿鼠标方向推出 150 像素
            // 这样 120 像素大的阴阳玉就不会卡在玩家身体里了
            position = player.Center + direction * 150f;
        }

        // ========== 合成配方 ==========
        // ========== 合成配方（月后） ==========
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // 核心材料：月亮领主掉落
            recipe.AddIngredient(ItemID.LunarBar, 100);           // 夜明锭 ×20

            // 主题材料：阴阳/噩梦意象
            recipe.AddIngredient(ItemID.Ectoplasm, 100);          // 灵气 ×15（幽灵/噩梦主题）
            recipe.AddIngredient(ItemID.BrokenHeroSword, 3);     // 断裂英雄剑 ×1（近战终极材料）

            // 合成站：远古操纵机（月亮领主后专属）
            recipe.AddTile(TileID.LunarCraftingStation);

            recipe.Register();
        }
    }
}