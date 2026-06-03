using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace TEACHER.Content
{
    // 第一个类：地上雪原
    public class SnowSurfaceMusic : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeMedium;
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Content/Sounds/Music/HorrorSnowSurface");
        
        public override bool IsSceneEffectActive(Player player)
        {
            return player.ZoneSnow && player.position.Y <= Main.worldSurface * 16;
        }
    }

    // 第二个类：地下雪世界（同文件内）
    public class SnowUndergroundMusic : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Content/Sounds/Music/HorrorSnowCave");
        
        public override bool IsSceneEffectActive(Player player)
        {
            return player.ZoneSnow && player.position.Y > Main.worldSurface * 16;
        }
    }
    
}