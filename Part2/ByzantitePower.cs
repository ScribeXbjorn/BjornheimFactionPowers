using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FactionPowers;

public partial class FactionPowers
{
    public class ByzantitePower : Power
    {
        private static readonly StatusEffect ToObjectDB;
        const string SE_NAME = "ByzantitePower";
        const int cooldown = 15*60;
        const int duration = 4*60;
        private const int armorAdd = 45;

        static ByzantitePower()
        { 
            PowerSE powerSe = ScriptableObject.CreateInstance<PowerSE>();
            powerSe.name = SE_NAME; 
            powerSe.m_name = "Byzantine Blessing";
            powerSe.m_tooltip = "Your allies are your strengh, and multiplying it with each group member in range!";
            powerSe.m_ttl = duration;
            powerSe.m_icon = FactionItems[UIHandler.Faction.ByzantineEmpire].Prefab.GetComponent<ItemDrop>()
                .m_itemData.m_shared.m_icons[0];
            powerSe.m_startEffects = new EffectList
            {
                m_effectPrefabs = new[] 
                {
                    new EffectList.EffectData 
                    { 
                        m_prefab = asset.LoadAsset<GameObject>("byzantiteeff"),
                        m_enabled = true,
                        m_attach = true,
                        m_childTransform = "Helmet_attach",
                        m_randomRotation = false,
                        m_inheritParentRotation = true, 
                        m_inheritParentScale = true,
                        m_scale = true
                    },
                    new EffectList.EffectData
                    { 
                        m_prefab = asset.LoadAsset<GameObject>("FactionActivationByzantite"),
                        m_enabled = true,
                        m_attach = true,
                        m_randomRotation = false
                    },
                    new EffectList.EffectData
                    {
                        m_prefab = asset.LoadAsset<GameObject>("byzantiteeff2"),
                        m_enabled = true,
                        m_attach = true,
                        m_randomRotation = false,
                        m_scale = true,
                        m_inheritParentRotation = true,
                        m_inheritParentScale = true
                    },
                }
            };
            ToObjectDB = powerSe;
        }

        public void Activate()
        {
            if (!Player.m_localPlayer) return;
            Player.m_localPlayer.m_seman.AddStatusEffect(SE_NAME);
        }

        public int GetCooldown()
        {
            return cooldown;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
        static class Humanoid_GetBodyArmor_Patch
        {
            static void Postfix(Player __instance, ref float __result)
            {
                if(__instance != Player.m_localPlayer) return;
                if(__instance.m_seman.GetStatusEffect(SE_NAME)) __result += armorAdd;
            } 
        }
        
        [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(ZNetScene __instance)
            {
                __instance.m_namedPrefabs["byzantiteeff".GetStableHashCode()] = asset.LoadAsset<GameObject>("byzantiteeff");
                __instance.m_namedPrefabs["byzantiteeff2".GetStableHashCode()] = asset.LoadAsset<GameObject>("byzantiteeff2");
                __instance.m_namedPrefabs["FactionActivationByzantite".GetStableHashCode()] = asset.LoadAsset<GameObject>("FactionActivationByzantite");
            }
        }
        
        [HarmonyPatch]
        static class ObjectDB_Awake_Patch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.Awake));
                yield return AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB));
            }
            [HarmonyPostfix]
            static void AddSE(ObjectDB __instance)
            {
                if (__instance.m_StatusEffects.Find(se => se.name == ToObjectDB.name)) return;
                __instance.m_StatusEffects.Add(ToObjectDB);
            }
        }

        public class PowerSE : StatusEffect
        {
            public float counter;
            public int PlayerNearby;

            public override void ModifySkillLevel(Skills.SkillType skill, ref float level)
            {
                if (skill is Skills.SkillType.Axes or Skills.SkillType.Bows or Skills.SkillType.Clubs
                    or Skills.SkillType.Crossbows or Skills.SkillType.Knives
                    or Skills.SkillType.Polearms or Skills.SkillType.Spears or Skills.SkillType.Swords
                    or Skills.SkillType.Unarmed or Skills.SkillType.Blocking)
                {
                    level += PlayerNearby * 10f;
                }
            }

            public override void UpdateStatusEffect(float dt)
            {
                base.UpdateStatusEffect(dt);
                counter += dt;
                if (counter < 1f) return;
                counter = 0;
                PlayerNearby = Player.GetAllPlayers().Count(x =>
                    x != m_character && Vector3.Distance(m_character.transform.position, x.transform.position) <= 20f);
            }

            public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
            {
                modifiers.m_pierce = HitData.DamageModifier.Resistant;
                modifiers.m_blunt = HitData.DamageModifier.Resistant;
            }
            
            public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
            {
                var total = hitData.GetTotalBlockableDamage();
                hitData.m_damage.m_spirit += total * 0.25f;
            }
        }
    }
}