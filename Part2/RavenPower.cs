using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FactionPowers;

public partial class FactionPowers
{
    public class RavenPower : Power
    {
        private static readonly StatusEffect ToObjectDB;
        const string SE_NAME = "RavenPower";
        const int cooldown = 15*60;
        const int duration = 4*60;

        static RavenPower()
        { 
            PowerSE powerSe = ScriptableObject.CreateInstance<PowerSE>();
            powerSe.name = SE_NAME; 
            powerSe.m_name = "Blessing of the Raven";
            powerSe.m_tooltip = "The wings of the raven lift the world's weight of your shoulders";
            powerSe.m_ttl = duration;
            powerSe.m_icon = FactionItems[UIHandler.Faction.Ravens].Prefab.GetComponent<ItemDrop>()
                .m_itemData.m_shared.m_icons[0];
            powerSe.m_startEffects = new EffectList
            {
                m_effectPrefabs = new[] 
                {
                    new EffectList.EffectData 
                    { 
                        m_prefab = asset.LoadAsset<GameObject>("raveneff"),  
                        m_enabled = true, 
                        m_attach = true, 
                        m_inheritParentRotation = true,   
                        m_childTransform = "Neck"
                    },  
                    new EffectList.EffectData 
                    { 
                        m_prefab = asset.LoadAsset<GameObject>("raveneff2"),  
                        m_enabled = true, 
                        m_attach = true, 
                        m_inheritParentRotation = true,
                        m_inheritParentScale = true
                    }, 
                    new EffectList.EffectData 
                    {    
                        m_prefab = asset.LoadAsset<GameObject>("FactionActivationRaven"),
                        m_enabled = true,
                        m_attach = true,
                        m_randomRotation = false
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
                __instance.m_namedPrefabs["raveneff".GetStableHashCode()] = asset.LoadAsset<GameObject>("raveneff");
                __instance.m_namedPrefabs["raveneff2".GetStableHashCode()] = asset.LoadAsset<GameObject>("raveneff2");
                __instance.m_namedPrefabs["FactionActivationRaven".GetStableHashCode()] = asset.LoadAsset<GameObject>("FactionActivationRaven");
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
            static void Prefix(Player __instance,ref float v)
            {
              if(__instance.m_seman.GetStatusEffect(SE_NAME)) v *= 0.5f;
            }
        }
        
        public class PowerSE : StatusEffect
        {
            public override void ModifyFallDamage(float baseDamage, ref float damage)
            {
                damage = 0;
            }

            public override void ModifyWalkVelocity(ref Vector3 vel)
            {
                if ( vel.y < -0.5f)
                {
                    vel.y = -0.5f;
                }
            }
            
            public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
            {
                var total = hitData.GetTotalBlockableDamage();
                hitData.m_damage.m_lightning += total * 0.25f;
            }
            
            public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers) 
            {
                modifiers.m_lightning = HitData.DamageModifier.Resistant;
            }
            
        }
    }
}