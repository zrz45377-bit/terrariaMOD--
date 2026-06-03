using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

// 引用判定点系统的命名空间（按你实际路径）
using TEACHER.Content.ITEM_S.N12.HitboxConfig_G;

namespace TEACHER.Content.ITEM_S.N12
{
    /// <summary>
    /// 被挖下来的眼睛 【饰品】
    /// </summary>
    public class DugOutEye : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 2, silver: 50);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<DugOutEyePlayer>();
            modPlayer.HasEye = true;

            // 核心：强制开启判定点
            HitboxConfig.ShowHitboxes = true;

            // 血色尘埃特效
            if (Main.rand.NextBool(15))
            {
                Dust.NewDust(player.position, player.width, player.height,
                    DustID.Blood, 0f, -1.5f, 100, default, 0.9f);
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var lore = new TooltipLine(Mod, "DugOutEye_Lore",
                "[c/AA2222:\"没人知道它在想什么...\"]");
            tooltips.Add(lore);
        }


    }

    /// <summary>
    /// 配套 ModPlayer：读心 + 疯狂 + 卸下自动关闭
    /// </summary>
    public class DugOutEyePlayer : ModPlayer
    {
        public bool HasEye = false;
        private bool hadEyeLastFrame = false;

        private int readMindTimer = 0;
        private int madnessTimer = 0;

        public override void ResetEffects()
        {
            HasEye = false;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
                return;

            // 卸下检测：上一帧有，这帧没了 → 关闭判定点
            if (hadEyeLastFrame && !HasEye)
            {
                HitboxConfig.ShowHitboxes = false;
                Main.NewText("[被挖下来的眼睛] 视野消散...", Color.DarkRed);
            }
            hadEyeLastFrame = HasEye;

            if (!HasEye) return;

            // 读心：鼠标悬停显示 NPC 信息
            readMindTimer++;
            if (readMindTimer >= 30)
            {
                readMindTimer = 0;
                Vector2 mouseWorld = Main.MouseWorld;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.active) continue;
                    if (!npc.Hitbox.Contains(mouseWorld.ToPoint())) continue;

                    string txt = $"Def:{npc.defense}  HP:{npc.life}/{npc.lifeMax}";
                    CombatText.NewText(npc.Hitbox, Color.MediumPurple, txt);
                    break;
                }
            }

            // 疯狂：每 7 秒随机使附近敌人混乱
            madnessTimer++;
            if (madnessTimer >= 420)
            {
                madnessTimer = 0;

                if (Main.rand.NextBool(5))
                {
                    NPC target = null;
                    float bestDist = 800f * 800f;

                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                        float d = npc.DistanceSQ(Player.Center);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            target = npc;
                        }
                    }

                    if (target != null)
                    {
                        target.AddBuff(BuffID.Confused, 180);
                        SoundEngine.PlaySound(SoundID.NPCDeath13, target.Center);

                        for (int k = 0; k < 12; k++)
                        {
                            Dust.NewDust(target.position, target.width, target.height,
                                DustID.Blood, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f),
                                100, default, 1.2f);
                        }
                    }
                }
            }
        }
    }
}