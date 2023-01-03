using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FactionPowers
{
    [BepInPlugin(GUID, PluginName, PluginVersion)]
    public partial class FactionPowers : BaseUnityPlugin
    {
        private const string GUID = "FactionPowers";
        private const string PluginName = "FactionPowers";
        private const string PluginVersion = "1.0.0";
        private static FactionPowers _thistype;
        private static ConfigEntry<int> UIx;
        private static ConfigEntry<int> UIy;
        private static AssetBundle asset;
        private static GameObject FactionAltar;
        private static readonly Dictionary<UIHandler.Faction, Item> FactionItems = new();
        private static readonly Dictionary<UIHandler.Faction, Power> PowersActivation = new();

        private void Awake()
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("FactionPowers.Resources.MeshEffectsLib.dll");
            var buffer = new byte[stream!.Length];
            stream.Read(buffer, 0, buffer.Length);
            try
            {
                Assembly.Load(buffer);
            }
            catch
            {
                // ignored
            }

            _thistype = this;
            asset = GetAssetBundle("factionpowers");
            FactionAltar = asset.LoadAsset<GameObject>("FactionAltar");
            FactionAltar.AddComponent<FactionAltarComponent>();
            FactionItems.Add(UIHandler.Faction.Ravens, new Item(asset.LoadAsset<GameObject>("Ravens_Item")));
            FactionItems.Add(UIHandler.Faction.LokisSerpents,
                new Item(asset.LoadAsset<GameObject>("LokisSerpents_Item")));
            FactionItems.Add(UIHandler.Faction.ByzantineEmpire,
                new Item(asset.LoadAsset<GameObject>("ByzantiteEmpire_Item")));
            FactionItems.Add(UIHandler.Faction.EyesOfFenrir,
                new Item(asset.LoadAsset<GameObject>("EyesOfFenrir_Item")));
            FactionItems.Add(UIHandler.Faction.Balrogs, new Item(asset.LoadAsset<GameObject>("Balrogs_Item")));
            UIHandler.Init();

            FactionItems[UIHandler.Faction.EyesOfFenrir].Name.English("The Wolf Skull");
            FactionItems[UIHandler.Faction.EyesOfFenrir].Crafting.Add(CraftingTable.Workbench, 1);
            FactionItems[UIHandler.Faction.EyesOfFenrir].Configurable = Configurability.Disabled;
            FactionItems[UIHandler.Faction.EyesOfFenrir].RequiredItems.Add("TrophyWolf", 1);
            FactionItems[UIHandler.Faction.EyesOfFenrir].RequiredItems.Add("BoneFragments", 35);

            FactionItems[UIHandler.Faction.Balrogs].Name.English("The Demon Skull");
            FactionItems[UIHandler.Faction.Balrogs].Crafting.Add(CraftingTable.Workbench, 1);
            FactionItems[UIHandler.Faction.Balrogs].Configurable = Configurability.Disabled;
            FactionItems[UIHandler.Faction.Balrogs].RequiredItems.Add("BoneFragments", 35);
            FactionItems[UIHandler.Faction.Balrogs].RequiredItems.Add("Bloodbag", 35);

            FactionItems[UIHandler.Faction.Ravens].Name.English("The Raven Skull");
            FactionItems[UIHandler.Faction.Ravens].Crafting.Add(CraftingTable.Workbench, 1);
            FactionItems[UIHandler.Faction.Ravens].Configurable = Configurability.Disabled;
            FactionItems[UIHandler.Faction.Ravens].RequiredItems.Add("Feathers", 30);
            FactionItems[UIHandler.Faction.Ravens].RequiredItems.Add("BoneFragments", 35); 

            FactionItems[UIHandler.Faction.LokisSerpents].Name.English("The Serpent Skull");
            FactionItems[UIHandler.Faction.LokisSerpents].Crafting.Add(CraftingTable.Workbench, 1);
            FactionItems[UIHandler.Faction.LokisSerpents].Configurable = Configurability.Disabled;
            FactionItems[UIHandler.Faction.LokisSerpents].RequiredItems.Add("Guck", 35);
            FactionItems[UIHandler.Faction.LokisSerpents].RequiredItems.Add("BoneFragments", 35);

            FactionItems[UIHandler.Faction.ByzantineEmpire].Name.English("The Byzantine Cross");
            FactionItems[UIHandler.Faction.ByzantineEmpire].Crafting.Add(CraftingTable.Workbench, 1);
            FactionItems[UIHandler.Faction.ByzantineEmpire].Configurable = Configurability.Disabled;
            FactionItems[UIHandler.Faction.ByzantineEmpire].RequiredItems.Add("Bronze", 15);
            FactionItems[UIHandler.Faction.ByzantineEmpire].RequiredItems.Add("Tin", 35);


            foreach (var item in FactionItems)
            {
                item.Value.Description.English("Bring that to Faction Altar in order to get powers ");
            }

            PowersActivation[UIHandler.Faction.ByzantineEmpire] = new ByzantitePower();
            PowersActivation[UIHandler.Faction.EyesOfFenrir] = new FenringPower();
            PowersActivation[UIHandler.Faction.Balrogs] = new BalrogPower();
            PowersActivation[UIHandler.Faction.Ravens] = new RavenPower();
            PowersActivation[UIHandler.Faction.LokisSerpents] = new LokiPower();
            new Harmony(GUID).PatchAll();
        }

        public interface Power
        {
            void Activate();
            int GetCooldown();
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix(ZNetScene __instance)
            {
                __instance.m_namedPrefabs[FactionAltar.name.GetStableHashCode()] = FactionAltar;

                var hammer = __instance.GetPrefab("Hammer").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
                if (!hammer.m_pieces.Contains(FactionAltar)) hammer.m_pieces.Add(FactionAltar);

                List<Piece.Requirement> reqs = new();
                reqs.Add(new Piece.Requirement
                {
                    m_resItem = __instance.GetPrefab("Bloodbag").GetComponent<ItemDrop>(),
                    m_amount = 50,
                    m_amountPerLevel = 0,
                    m_recover = false
                });

                reqs.Add(new Piece.Requirement
                {
                    m_resItem = __instance.GetPrefab("Guck").GetComponent<ItemDrop>(),
                    m_amount = 50,
                    m_amountPerLevel = 0,
                    m_recover = false
                });

                reqs.Add(new Piece.Requirement
                {
                    m_resItem = __instance.GetPrefab("GreydwarfEye").GetComponent<ItemDrop>(),
                    m_amount = 1,
                    m_amountPerLevel = 0,
                    m_recover = false
                });

                reqs.Add(new Piece.Requirement
                {
                    m_resItem = __instance.GetPrefab("TinOre").GetComponent<ItemDrop>(),
                    m_amount = 30,
                    m_amountPerLevel = 0,
                    m_recover = false
                });

                reqs.Add(new Piece.Requirement
                {
                    m_resItem = __instance.GetPrefab("CopperOre").GetComponent<ItemDrop>(),
                    m_amount = 30,
                    m_amountPerLevel = 0,
                    m_recover = false
                });

                reqs.Add(new Piece.Requirement
                {
                    m_resItem = __instance.GetPrefab("Stone").GetComponent<ItemDrop>(),
                    m_amount = 50,
                    m_amountPerLevel = 0,
                    m_recover = false
                });


                FactionAltar.GetComponent<Piece>().m_resources = reqs.ToArray();
            }
        }


        private DateTime lastUpdate = DateTime.Now;

        private void Update()
        {
            if (!Player.m_localPlayer) return;
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.F) && UIHandler.CanUseSkill)
            {
                Player.m_localPlayer.m_zanim.SetTrigger("gpower");
                lastUpdate = DateTime.Now;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ActivateGuardianPower))]
        static class Player_ActivateGuardianPower_Patch
        {
            static bool Prefix(Player __instance)
            {
                if (DateTime.Now - _thistype.lastUpdate < TimeSpan.FromSeconds(2))
                {
                    UIHandler.SetSkillUsage();
                    return false;
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Awake))]
        static class AudioMan_Awake_Patch
        {
            static void Postfix(AudioMan __instance)
            {
                foreach (GameObject allAsset in asset.LoadAllAssets<GameObject>())
                {
                    foreach (AudioSource audioSource in allAsset.GetComponentsInChildren<AudioSource>(true))
                    {
                        audioSource.outputAudioMixerGroup = __instance.m_masterMixer.outputAudioMixerGroup;
                    }
                }
            }
        }


        public class DragUI : MonoBehaviour, IDragHandler, IEndDragHandler
        {
            public RectTransform dragRect;

            private void Awake()
            {
                dragRect = GetComponent<RectTransform>();
            }

            public void OnDrag(PointerEventData eventData)
            {
                var vec = dragRect.anchoredPosition + eventData.delta;
                dragRect.anchoredPosition += eventData.delta;
            }

            public void OnEndDrag(PointerEventData data)
            {
                UIx.Value = (int)dragRect.anchoredPosition.x;
                UIy.Value = (int)dragRect.anchoredPosition.y;
                _thistype.Config.Save();
            }
        }


        private static AssetBundle GetAssetBundle(string filename)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
            using Stream stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }


        public class FactionAltarComponent : MonoBehaviour, Interactable, Hoverable
        {
            private ZNetView _znv;
            private void Awake()
            {
                _znv = GetComponent<ZNetView>();
                if(!_znv.IsValid()) return;
                _znv.Register("UseFactionAltar", new Action<long,int>(UseFactionAltar));
                SetVisuals((UIHandler.Faction)_znv.m_zdo.GetInt("Faction"));
            }

            private void UseFactionAltar(long sender, int id)
            {
                if (_znv.IsOwner())
                {
                    _znv.m_zdo.Set("Faction", id);
                }
                SetVisuals((UIHandler.Faction)id);
             
            }

            private void SetVisuals(UIHandler.Faction id)
            {
                if(id == UIHandler.Faction.None) return;

                var t = transform.Find("Items");
                foreach (Transform child in t)
                {
                    child.gameObject.SetActive(false);
                }
                
                t.Find(id.ToString()).gameObject.SetActive(true);
            }
            
            
            public bool Interact(Humanoid user, bool hold, bool alt)
            {
                if (_znv.m_zdo.GetInt("Faction") != (int)UIHandler.Faction.None)
                {
                    var f = (UIHandler.Faction)_znv.m_zdo.GetInt("Faction");
                    UIHandler.SetFaction(f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, Texts[f]);
                    return true; 
                }
                return false;
            }


            private static readonly Dictionary<UIHandler.Faction, string> Texts = new()
            {
                { UIHandler.Faction.None, "" },
                {
                    UIHandler.Faction.Balrogs,
                    "The blood of balrogs burn inside your veins and his wings aid your stride!"
                },
                { UIHandler.Faction.EyesOfFenrir, "Your feral hearts burst out of your chests!" },
                { UIHandler.Faction.Ravens, "The wings of the raven lift the world's weight off your shoulders!" },
                {
                    UIHandler.Faction.ByzantineEmpire,
                    "You can feel how God wraps your soul and your body with power and holines!"
                },
                { UIHandler.Faction.LokisSerpents, "The mischievous aid of loki surges through your body!" }
            };

            public bool UseItem(Humanoid user, ItemDrop.ItemData item)
            {
                if (_znv.m_zdo.GetInt("Faction") != (int)UIHandler.Faction.None) return false;
                foreach (var factionItem in FactionItems)
                {
                    var fItem = factionItem.Value.Prefab.name; 
                    if (item == null || item.m_dropPrefab == null) continue;
                    if (item.m_dropPrefab.name == fItem)
                    {
                        _znv.InvokeRPC(ZNetView.Everybody,"UseFactionAltar", (int)factionItem.Key);
                        user.m_inventory.RemoveOneItem(item);
                        return true;
                    }
                }

                return false;
            }
            

            public string GetHoverText()
            {
                string addText = _znv.m_zdo.GetInt("Faction") != (int)UIHandler.Faction.None ? Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] Get Altar Power") : "";
                return Localization.instance.Localize("[<color=yellow><b>1-8</b></color>] Use faction item") + addText;
            }

            public string GetHoverName()
            {
                return "Faction Altar";
            }
        }


        public static class UIHandler
        {
            public enum Faction
            {
                None,
                ByzantineEmpire,
                EyesOfFenrir,
                Ravens,
                Balrogs,
                LokisSerpents
            }


            private static GameObject UI;
            private static Transform Main;
            private static Image Sprite;
            private static Image Cooldown;

            private static float CooldownTime;

            private static Faction _currentFaction;


            private static Sprite NullSprite;

            private static Dictionary<Faction, Sprite> FactionSprites = new Dictionary<Faction, Sprite>();


            public static void Init()
            {
                UI = Instantiate(asset.LoadAsset<GameObject>("FactionPowersUI"));
                Main = UI.transform.Find("Canvas/Skill");
                Sprite = Main.Find("SkillSprite").GetComponent<Image>();
                Cooldown = Sprite.transform.Find("Cooldown").GetComponent<Image>();
                Main.gameObject.AddComponent<DragUI>();
                DontDestroyOnLoad(UI);
                UI.SetActive(false);
                CooldownTime = 0;
                NullSprite = Sprite.sprite;
                FactionSprites.Add(Faction.ByzantineEmpire,
                    FactionItems[Faction.ByzantineEmpire].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared
                        .m_icons[0]);
                FactionSprites.Add(Faction.EyesOfFenrir,
                    FactionItems[Faction.EyesOfFenrir].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0]);
                FactionSprites.Add(Faction.Ravens,
                    FactionItems[Faction.Ravens].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0]);
                FactionSprites.Add(Faction.Balrogs,
                    FactionItems[Faction.Balrogs].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0]);
                FactionSprites.Add(Faction.LokisSerpents,
                    FactionItems[Faction.LokisSerpents].Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0]);

                UIx = _thistype.Config.Bind("UI", "UIx", 246, "UI X position");
                UIy = _thistype.Config.Bind("UI", "UIy", 142, "UI Y position");
                Main.GetComponent<RectTransform>().anchoredPosition = new Vector2(UIx.Value, UIy.Value);
            }


            private static void LoadFaction()
            {
                const string startsWith = "FactionPowers.";

                if (!Player.m_localPlayer.m_customData.ContainsKey("KG_FACTION_Scribex"))
                {
                    _currentFaction = Faction.None;
                    return;
                }

                var faction = Player.m_localPlayer.m_customData["KG_FACTION_Scribex"];
                _currentFaction = (Faction)Enum.Parse(typeof(Faction), faction);
                SetIcons();
            }

            public static void SetFaction(Faction faction)
            {
                _currentFaction = faction;
                var name = faction.ToString();
                if (faction == Faction.None)
                {
                    Player.m_localPlayer.m_customData.Remove("KG_FACTION_Scribex");
                    return;
                }

                Player.m_localPlayer.m_customData["KG_FACTION_Scribex"] = name;
                SetIcons();
            }

            private static void SetIcons()
            {
                Sprite.sprite = FactionSprites[_currentFaction];
            }

            public static void SetSkillUsage()
            {
                if (!CanUseSkill) return;
                PowersActivation[_currentFaction].Activate();
                CooldownTime = PowersActivation[_currentFaction].GetCooldown();
                _thistype.StartCoroutine(DrawCooldown());
            }

            private static IEnumerator DrawCooldown()
            {
                float cd = CooldownTime;
                while (CooldownTime > 0)
                {
                    Cooldown.fillAmount = CooldownTime / cd;
                    CooldownTime -= Time.deltaTime;
                    yield return null;
                }

                CooldownTime = 0;
                Cooldown.fillAmount = 0;
            }

            public static bool CanUseSkill => _currentFaction != Faction.None && CooldownTime <= 0;


            public static void Show()
            {
                CooldownTime = 0;
                UI.SetActive(true);
            }

            public static void Hide()
            {
                UI.SetActive(false);
            }

            [HarmonyPatch(typeof(Player), nameof(Player.Load))]
            private class InitUIPlayer
            {
                private static void Postfix()
                {
                    if (!Player.m_localPlayer) return;
                    Show();
                    LoadFaction();
                }
            }

            [HarmonyPatch(typeof(Menu), nameof(Menu.OnLogoutYes))]
            private class CloseUIMenuLogout
            {
                private static void Postfix()
                {
                    Hide();
                }
            }
        }
    }
}