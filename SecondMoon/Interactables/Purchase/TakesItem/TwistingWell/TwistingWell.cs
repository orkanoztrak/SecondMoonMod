using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Hologram;
using SecondMoon.Items.Lunar.IrradiatedMoonstone;
using SecondMoon.Items.Lunar.Moonstone;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.ColorCatalog;

namespace SecondMoon.Interactables.Purchase.TakesItem.TwistingWell;

public class TwistingWell : Interactable
{
    public static ConfigOption<bool> IrradiantPearlCanBeTwisted;
    public static ConfigOption<bool> IrradiantPearlGivesIrradiatedMoonstone;
    public static ConfigOption<bool> PearlHasPriority;

    public override string InteractableName => "Twisting Well";

    public override string InteractableContext => "Use Twisting Well";
    
    public override string InteractableLangToken => "TWISTING_WELL";

    public override string InteractableInspectDesc => "Allows survivors to sacrifice a Pearl in exchange for a Moonstone item.";

    public override GameObject InteractableModel => SecondMoonPlugin.SecondMoonAssets.LoadAsset<GameObject>("Assets/SecondMoonAssets/Models/Interactables/TwistingWell/TwistingWell.prefab");

    public Sprite TwistingWellSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texShrineIconOutlined.png").WaitForCompletion();

    [SyncVar]
    public static GameObject InteractableBodyModelPrefab;

    public GameObject PrefabInstance;

    public static CostTypeDef PearlCost;

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            Hooks();
        }
    }

    public override void Hooks()
    {
        CostTypeCatalog.modHelper.getAdditionalEntries += AddPearlCost;
        RoR2Application.onLoad += ConstructInteractable;
        Stage.onStageStartGlobal += PutTwistingWellInBazaar;
        if (IrradiantPearlCanBeTwisted && IrradiantPearlGivesIrradiatedMoonstone)
        {
            On.RoR2.ShopTerminalBehavior.DropPickup += ChangeDrop;
        }
    }

    private void ChangeDrop(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
    {
        bool fix = false;
        if (self.gameObject.Equals(PrefabInstance))
        {
            var tracker = self.gameObject.GetComponent<TwistingWellIrradiantPearlTracker>();
            if (tracker)
            {
                if (tracker.IrradiantPearlTakenCount > 0)
                {
                    fix = true;
                    self.pickupIndex = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(IrradiatedMoonstone.instance.ItemDef.itemIndex)).pickupIndex;
                }
            }
        }
        orig(self);
        if (fix)
        {
            self.gameObject.GetComponent<TwistingWellIrradiantPearlTracker>().IrradiantPearlTakenCount--;
        }
    }

    private void PutTwistingWellInBazaar(Stage stage)
    {
        if (!NetworkServer.active) return;
        if (stage.sceneDef.nameToken != "MAP_BAZAAR_TITLE") return;
        PrefabInstance = UnityEngine.Object.Instantiate(InteractableBodyModelPrefab, new Vector3(-74f, -25.65001f, -42.92929f), Quaternion.Euler(2.476025f, 100f, 355.9525f));
        NetworkServer.Spawn(PrefabInstance);
    }

    private void CreateConfig(ConfigFile config)
    {
        IrradiantPearlCanBeTwisted = config.ActiveBind("Interactable: " + InteractableName, "Irradiant Pearl can be twisted", false, "If enabled, Twisting Wells will be able to take Irradiant Pearls.");
        IrradiantPearlGivesIrradiatedMoonstone = config.ActiveBind("Interactable: " + InteractableName, "Irradiant Pearl gives Irradiated Moonstone", true, "If enabled alongside the ability to twist Irradiant Pearls, the Twisting Well will always drop an Irradiated Moonstone in exchange.");
        PearlHasPriority = config.ActiveBind("Interactable: " + InteractableName, "Pearl has priority when twisting", true, "If enabled alongside the ability to twist Irradiant Pearls, Twisting Wells will prioritize Pearls over Irradiant Pearls.");
    }

    protected override void CreateLang()
    {
        base.CreateLang();
        LanguageAPI.Add("COST_PEARL_FORMAT", "1 " + Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Pearl/Pearl.asset").WaitForCompletion().name);
    }
    private void AddPearlCost(List<CostTypeDef> list)
    {
        PearlCost = new CostTypeDef()
        {
            name = "Pearl",
            isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
            {
                CharacterBody body = context.activator.GetComponent<CharacterBody>();
                if (body)
                {
                    Inventory inventory = body.inventory;
                    if ((bool)inventory)
                    {
                        if (IrradiantPearlCanBeTwisted)
                        {
                            return inventory.GetItemCount(RoR2Content.Items.Pearl) + inventory.GetItemCount(RoR2Content.Items.ShinyPearl) >= context.cost;
                        }
                        return inventory.GetItemCount(RoR2Content.Items.Pearl) >= context.cost;
                    }
                }
                return false;
            },
            payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
            {
                if (IrradiantPearlCanBeTwisted)
                {
                    WeightedSelection<ItemIndex> ws = new WeightedSelection<ItemIndex>();
                    WeightedSelection<ItemIndex> ws2 = new WeightedSelection<ItemIndex>();
                    if (PearlHasPriority)
                    {
                        ws.AddChoice(RoR2Content.Items.Pearl.itemIndex, context.activatorBody.inventory.GetItemCount(RoR2Content.Items.Pearl));
                        ws2.AddChoice(RoR2Content.Items.ShinyPearl.itemIndex, context.activatorBody.inventory.GetItemCount(RoR2Content.Items.ShinyPearl));
                    }
                    else
                    {
                        ws.AddChoice(RoR2Content.Items.Pearl.itemIndex, context.activatorBody.inventory.GetItemCount(RoR2Content.Items.Pearl));
                        ws.AddChoice(RoR2Content.Items.ShinyPearl.itemIndex, context.activatorBody.inventory.GetItemCount(RoR2Content.Items.ShinyPearl));
                    }
                    for (int i = 0; i < context.cost; i++)
                    {
                        if (ws.totalWeight > 0)
                        {
                            var choiceIndex = ws.EvaluateToChoiceIndex(context.rng.nextNormalizedFloat);
                            var choice = ws.GetChoice(choiceIndex);
                            ItemIndex ind = choice.value;
                            int num = (int)choice.weight;
                            num--;
                            if (num <= 0)
                            {
                                ws.RemoveChoice(choiceIndex);
                            }
                            else
                            {
                                ws.ModifyChoiceWeight(choiceIndex, num);
                            }
                            context.results.itemsTaken.Add(ind);
                            if (ind == RoR2Content.Items.ShinyPearl.itemIndex)
                            {
                                var tracker = PrefabInstance.GetComponent<TwistingWellIrradiantPearlTracker>();
                                tracker.IrradiantPearlTakenCount++;
                            }
                            context.activatorBody.inventory.RemoveItem(ind, 1);
                        }
                        else if (ws2.totalWeight > 0)
                        {
                            var choiceIndex = ws2.EvaluateToChoiceIndex(context.rng.nextNormalizedFloat);
                            var choice = ws2.GetChoice(choiceIndex);
                            ItemIndex ind = choice.value;
                            int num = (int)choice.weight;
                            num--;
                            if (num <= 0)
                            {
                                ws2.RemoveChoice(choiceIndex);
                            }
                            else
                            {
                                ws2.ModifyChoiceWeight(choiceIndex, num);
                            }
                            context.results.itemsTaken.Add(ind);
                            if (ind == RoR2Content.Items.ShinyPearl.itemIndex)
                            {
                                var tracker = PrefabInstance.GetComponent<TwistingWellIrradiantPearlTracker>();
                                tracker.IrradiantPearlTakenCount++;
                            }
                            context.activatorBody.inventory.RemoveItem(ind, 1);
                        }
                    }
                }
                else
                {
                    context.activatorBody.inventory.RemoveItem(RoR2Content.Items.Pearl, context.cost);
                    for (int i = 0; i < context.cost; i++)
                    {
                        context.results.itemsTaken.Add(RoR2Content.Items.Pearl.itemIndex);
                    }
                }
                RoR2.Items.MultiShopCardUtils.OnNonMoneyPurchase(context);
            },
            colorIndex = ColorIndex.BossItem,
            costStringFormatToken = "COST_PEARL_FORMAT"
        };
        list.Add(PearlCost);
    }

    private void ConstructInteractable()
    {
        SetSomeModelMats();
        InteractableBodyModelPrefab = InteractableModel;
        var pingInfoProvider = InteractableBodyModelPrefab.AddComponent<PingInfoProvider>();
        pingInfoProvider.pingIconOverride = TwistingWellSprite;

        var inspect = ScriptableObject.CreateInstance<InspectDef>();
        var info = new RoR2.UI.InspectInfo();
        info.Visual = TwistingWellSprite;
        info.DescriptionToken = $"INTERACTABLE_{InteractableLangToken}_INSPECT";
        info.TitleToken = $"INTERACTABLE_{InteractableLangToken}_TITLE";
        inspect.Info = info;

        var shopTerminalBehavior = InteractableBodyModelPrefab.GetComponent<ShopTerminalBehavior>();
        shopTerminalBehavior.dropTransform = InteractableBodyModelPrefab.transform.Find("mdlTwistingWell").Find("DropPivot");
        shopTerminalBehavior.dropVelocity = new Vector3(0f, 25f, 6f);
        shopTerminalBehavior.inspectShop = true;
        shopTerminalBehavior.shopInspectDef = inspect;
        shopTerminalBehavior.disablesInspectionOnPurchase = false;
        var dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
        dropTable.pickupEntries = [new ExplicitPickupDropTable.PickupDefEntry { pickupDef = Moonstone.instance.ItemDef, pickupWeight = 4 }, new ExplicitPickupDropTable.PickupDefEntry { pickupDef = IrradiatedMoonstone.instance.ItemDef, pickupWeight = 1 }];
        shopTerminalBehavior.dropTable = dropTable;

        var purchaseInteraction = InteractableBodyModelPrefab.GetComponent<PurchaseInteraction>();
        purchaseInteraction.displayNameToken = $"INTERACTABLE_{InteractableLangToken}_NAME";
        purchaseInteraction.contextToken = $"INTERACTABLE_{InteractableLangToken}_CONTEXT";
        purchaseInteraction.costType = (CostTypeIndex)Array.IndexOf(CostTypeCatalog.costTypeDefs, PearlCost);
        purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
        purchaseInteraction.cost = 1;
        purchaseInteraction.available = true;
        purchaseInteraction.setUnavailableOnTeleporterActivated = true;
        purchaseInteraction.isShrine = true;
        purchaseInteraction.isGoldShrine = false;

        var genericInspectInfoProvider = InteractableBodyModelPrefab.gameObject.AddComponent<GenericInspectInfoProvider>();
        genericInspectInfoProvider.InspectInfo = inspect;

        var entityStateMachine = InteractableBodyModelPrefab.AddComponent<EntityStateMachine>();
        entityStateMachine.customName = "TwistingWell";
        entityStateMachine.initialStateType.stateType = typeof(Idle);
        entityStateMachine.mainStateType.stateType = typeof(Idle);

        var networkStateMachine = InteractableBodyModelPrefab.AddComponent<NetworkStateMachine>();
        networkStateMachine.stateMachines = [entityStateMachine];

        var genericNameDisplay = InteractableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
        genericNameDisplay.displayToken = $"INTERACTABLE_{InteractableLangToken}_NAME";

        var highlightController = InteractableBodyModelPrefab.GetComponent<Highlight>();
        highlightController.targetRenderer = InteractableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject.name.Contains("mdlTwistingWell")).First();
        highlightController.strength = 1;
        highlightController.highlightColor = Highlight.HighlightColor.interactive;

        var hologramController = InteractableBodyModelPrefab.AddComponent<HologramProjector>();
        hologramController.hologramPivot = InteractableBodyModelPrefab.transform.Find("HologramPivot");
        hologramController.displayDistance = 10;
        hologramController.disableHologramRotation = true;

        if (IrradiantPearlCanBeTwisted && IrradiantPearlGivesIrradiatedMoonstone)
        {
            InteractableBodyModelPrefab.AddComponent<TwistingWellIrradiantPearlTracker>();
        }

        var lunarCostDef = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarItemOrEquipment);

        InteractableBodyModelPrefab.RegisterNetworkPrefab();

        void SetSomeModelMats()
        {
            //helfire range for foam, turn off liquid mesh
            var foamRenderer = InteractableModel.transform.Find("mdlTwistingWell").Find("Foam").gameObject.GetComponent<MeshRenderer>();
            foamRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/BurnNearby/matHelfireRangeIndicator.mat").WaitForCompletion();

            //change swirls
            var swirls = InteractableModel.transform.Find("mdlTwistingWell").Find("Swirls").gameObject.GetComponent<ParticleSystem>();
            var swirlsRenderer = InteractableModel.transform.Find("mdlTwistingWell").Find("Swirls").gameObject.GetComponent<ParticleSystemRenderer>();
            swirlsRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Chef/matBoostedSearFireballFlame.mat").WaitForCompletion();
        }
    }

    public class TwistingWellIrradiantPearlTracker : MonoBehaviour
    {
        public int IrradiantPearlTakenCount = 0;
    }
}
