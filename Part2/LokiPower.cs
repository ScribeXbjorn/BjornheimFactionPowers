using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FactionPowers;

public partial class FactionPowers
{
    public class LokiPower : Power
    {
        private static readonly StatusEffect ToObjectDB;
        const string SE_NAME = "LokiPower";
        const int cooldown = 15*60;
        const int duration = 4*60;
        private const int armorAdd = 45;

        static LokiPower()
        { 
            PowerSE powerSe = ScriptableObject.CreateInstance<PowerSE>();
            powerSe.name = SE_NAME; 
            powerSe.m_name = "The serpents blessing";
            powerSe.m_tooltip = "The mischievous aid of loki surges through your body";
            powerSe.m_ttl = duration;
            powerSe.m_icon = FactionItems[UIHandler.Faction.LokisSerpents].Prefab.GetComponent<ItemDrop>()
                .m_itemData.m_shared.m_icons[0];
            powerSe.m_startEffects = new EffectList
            {
                m_effectPrefabs = new[] 
                {
                    new EffectList.EffectData 
                    { 
                        m_prefab = asset.LoadAsset<GameObject>("lokieff"),
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
                        m_prefab = asset.LoadAsset<GameObject>("FactionActivationLoki"),
                        m_enabled = true,
                        m_attach = true,
                        m_randomRotation = false
                    },
                    new EffectList.EffectData
                    {
                        m_prefab = asset.LoadAsset<GameObject>("lokieff2"),
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

        [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(ZNetScene __instance)
            {
                __instance.m_namedPrefabs["lokieff".GetStableHashCode()] = asset.LoadAsset<GameObject>("lokieff");
                __instance.m_namedPrefabs["lokieff2".GetStableHashCode()] = asset.LoadAsset<GameObject>("lokieff2");
                __instance.m_namedPrefabs["FactionActivationLoki".GetStableHashCode()] = asset.LoadAsset<GameObject>("FactionActivationLoki");
            }
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

        
        [HarmonyPatch(typeof(Player),nameof(Player.UseStamina))]
        static class Player_UseStamina_Patch
        {
            static void Prefix(Player __instance, ref float v)
            {
                if (__instance.InDodge() && __instance.m_seman.GetStatusEffect(SE_NAME))
                {
                    v = 0;
                } 
            }
        }
        
        public class PowerSE : StatusEffect
        {
            public override void ModifySkillLevel(Skills.SkillType skill, ref float level)
            {
                switch (skill)
                {
                    case Skills.SkillType.Sneak:
                        level += 50;
                        break;
                    case Skills.SkillType.Axes or Skills.SkillType.Bows or Skills.SkillType.Clubs
                        or Skills.SkillType.Crossbows or Skills.SkillType.Knives
                        or Skills.SkillType.Polearms or Skills.SkillType.Spears or Skills.SkillType.Swords
                        or Skills.SkillType.Unarmed:
                        level += 10f;
                        break;
                }
            }


            public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
            {
                var total = hitData.GetTotalBlockableDamage();
                hitData.m_damage.m_poison += total * 0.25f;
            }
            
            public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
            {
                modifiers.m_poison = HitData.DamageModifier.Resistant;
            }
        }
    }
}