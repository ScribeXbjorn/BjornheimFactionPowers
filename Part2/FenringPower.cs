using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FactionPowers;

public partial class FactionPowers
{
    private static void ReplaceMyModel(string prefab)
    {
        ZDOID zdoID = Player.m_localPlayer.GetZDOID();
        ZPackage pkg = new();
        pkg.Write(zdoID);
        pkg.Write(prefab);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "KGchangemodel_Faction", pkg);
    }


    [HarmonyPatch(typeof(Player), "Start")]
    private static class PlayerStartPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!Player.m_localPlayer) return;
            string @string = __instance.m_nview.m_zdo.GetString("KGmodelchanged");
            if (!string.IsNullOrWhiteSpace(@string)) ReplacePlayerModel(__instance, @string);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    private static class AddingZroutMethods
    {
        private static void Postfix()
        {
            ZRoutedRpc.instance.Register("KGchangemodel_Faction",
                new Action<long, ZPackage>(PlayerChangedModel));
        }
    }

    private static void PlayerChangedModel(long sender, ZPackage pkg)
    {
        ZDOID id = pkg.ReadZDOID();
        string changedModel = pkg.ReadString();
        GameObject go = ZNetScene.instance.FindInstance(id);
        if (!go || !go.GetComponent<Player>()) return;
        Player component = go.GetComponent<Player>();
        ReplacePlayerModel(component, changedModel);
    }

    private static T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        Type type = original.GetType();
        Component component = destination.AddComponent(type);
        try
        {
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic;
            PropertyInfo[] properties = type.GetProperties(bindingAttr);
            foreach (PropertyInfo propertyInfo in properties)
            {
                bool canWrite = propertyInfo.CanWrite;
                if (canWrite)
                {
                    propertyInfo.SetValue(component, propertyInfo.GetValue(original, null), null);
                }
            }

            FieldInfo[] fields = type.GetFields(bindingAttr);
            foreach (FieldInfo fieldInfo in fields)
            {
                fieldInfo.SetValue(component, fieldInfo.GetValue(original));
            }
        }
        catch
        {
            // ignored
        }

        return component as T;
    }

    private static void ResetPlayerModel(Player p)
    {
        Transform transform = p.transform.Find("KG_transform_Faction");
        if (!transform) return;
        Destroy(p.transform.Find("KG_transform_Faction").gameObject);
        p.m_visual = p.transform.Find("Visual").gameObject;
        p.m_visual.transform.SetSiblingIndex(0);
        p.m_visual.SetActive(true);
        p.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_animator.SetBool("wakeup", false);
        p.m_animator.Update(0f);
        p.m_zanim.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_visEquipment.m_visual = p.m_visual;
        p.GetComponent<FootStep>().m_feet = new[] 
        {
            Utils.FindChild(p.m_visual.transform, "LeftFoot"),
            Utils.FindChild(p.m_visual.transform, "RightFoot")
        };
        p.m_collider.enabled = true;
    }

    private static void ReplacePlayerModel(Player p, string changedModel)
    {
        ResetPlayerModel(p);
        if (p.m_nview.IsOwner()) p.m_nview.m_zdo.Set("KGmodelchanged", changedModel);
        GameObject gameObject = ZNetScene.instance.GetPrefab(changedModel);
        if (!gameObject || !gameObject.GetComponent<Character>()) return;
        p.m_visual = Instantiate(gameObject.GetComponentInChildren<Animator>().gameObject, p.transform);
        p.m_visual.layer = LayerMask.NameToLayer("character");
        p.m_visual.transform.SetSiblingIndex(0);
        p.m_visual.transform.name = "KG_transform_Faction";
        Collider collider = CopyComponent(ZNetScene.instance.GetPrefab(changedModel).GetComponent<Collider>(),
            p.m_visual);
        collider.gameObject.layer = LayerMask.NameToLayer("character");
        p.m_visual.transform.localPosition = Vector3.zero;
        if (changedModel == "Fenring") p.m_visual.transform.localScale *= 0.6f;
        p.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_animator.runtimeAnimatorController = p.transform.Find("Visual").GetComponent<Animator>().runtimeAnimatorController;
        p.m_animator.SetBool("wakeup", false);
        p.m_animator.Update(0f);
        p.m_zanim.m_animator = p.m_visual.GetComponent<Animator>();
        p.transform.Find("Visual").gameObject.SetActive(false);
        p.m_visEquipment.m_visual = p.m_visual;
        p.m_animator.logWarnings = false;
        p.m_collider.enabled = false;
        p.GetComponent<FootStep>().m_feet = new[]
        {
            Utils.FindChild(p.m_visual.transform, "LeftFoot"),
            Utils.FindChild(p.m_visual.transform, "RightFoot")
        };
    }


    public class FenringPower : Power
    {
        private static readonly StatusEffect ToObjectDB;
        const string SE_NAME = "FenringPower";
        const int cooldown = 15*60;
        const int duration = 4*60;

        static FenringPower()
        {
            PowerSE powerSe = ScriptableObject.CreateInstance<PowerSE>();
            powerSe.name = SE_NAME;
            powerSe.m_name = "Strengh of Fenrir";
            powerSe.m_tooltip = "Your feral hearts burst out of your chests!";
            powerSe.m_ttl = duration;
            powerSe.m_icon = FactionItems[UIHandler.Faction.EyesOfFenrir].Prefab.GetComponent<ItemDrop>()
                .m_itemData.m_shared.m_icons[0];
            powerSe.m_startEffects = new EffectList
            {  
                m_effectPrefabs = new[]
                { 
                    new EffectList.EffectData
                    {
                        m_prefab = asset.LoadAsset<GameObject>("FactionActivationFenring"),
                        m_enabled = true,
                        m_attach = true,
                        m_randomRotation = false
                    }, 
                    new EffectList.EffectData 
                    { 
                        m_prefab = asset.LoadAsset<GameObject>("fenringeff"),
                        m_enabled = true,
                        m_attach = true,
                        m_randomRotation = false,
                        m_inheritParentRotation = true, 
                        m_inheritParentScale = true,
                        m_scale = true
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
                __instance.m_namedPrefabs["fenringeff".GetStableHashCode()] = asset.LoadAsset<GameObject>("fenringeff");
                __instance.m_namedPrefabs["FactionActivationFenring".GetStableHashCode()] = asset.LoadAsset<GameObject>("FactionActivationFenring");
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
        

        public class PowerSE : StatusEffect
        {
            public override void UpdateStatusEffect(float dt)
            {
                base.UpdateStatusEffect(dt);
                if (IsDone())
                {
                    ReplaceMyModel("");
                }
            }

            public override void Setup(Character character)
            {
                base.Setup(character);
                ReplaceMyModel("Fenring");
            }

            public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
            {
                var total = hitData.GetTotalBlockableDamage();
                hitData.m_damage.m_frost += total * 0.25f;
            }

            public override void ModifySpeed(float baseSpeed, ref float speed)
            {
                speed *= 1.15f;
            }

            public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
            {
                staminaUse *= 0.8f;
            }

            public override void ModifyRunStaminaDrain(float baseDrain, ref float drain)
            {
                drain *= 0.8f;
            }

            public override void ModifyHealthRegen(ref float regenMultiplier)
            {
                regenMultiplier *= 3f;
            }
            
            
            public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
            {
                modifiers.m_frost = HitData.DamageModifier.Resistant;
            }
        }
    }
}