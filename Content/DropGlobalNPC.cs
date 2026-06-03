using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace TEACHER.Content
{
    public class DropGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 暗黑法师 (Dark Caster) 概率掉落附魔剑 —— 10% 几率
            if (npc.type == NPCID.DarkCaster)
            {
                npcLoot.Add(ItemDropRule.Common(ItemID.EnchantedSword, 10));
            }

            // 骷髅王 (Skeletron) 掉落村正大刀 —— 25% 几率
            if (npc.type == NPCID.SkeletronHead)
            {
                npcLoot.Add(ItemDropRule.Common(ItemID.Muramasa, 4));
            }
        }
    }
}