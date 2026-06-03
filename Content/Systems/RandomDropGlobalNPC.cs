using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TEACHER.Content.Systems
{
    /// <summary>
    /// 服务器端功能配置面板。
    /// 在 Esc → 设置 → 模组配置 → TEACHER 里可见。
    /// </summary>
    public class TEACHERConfig : ModConfig
    {
        // 配置类型：ServerSide 表示由主机/单人玩家控制，加入服务器的客户端自动同步
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("危险功能")]
        [Label("启用随机掉落怪生成")]
        [Tooltip("开启后，'遗落者'将在夜晚的地表自然生成。\n击杀后会从全游戏物品池里随机掉落物品。\n警告：可能导致存档混乱或获得异常物品，后果自负！")]
        [DefaultValue(false)]
        public bool EnableRandomDropSpawner;
    }
}