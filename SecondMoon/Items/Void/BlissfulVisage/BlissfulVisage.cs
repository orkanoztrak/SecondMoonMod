using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Void.BlissfulVisage;

public class BlissfulVisage : Item<BlissfulVisage>
{
    public static ConfigOption<float> BlissfulVisageGhostCooldown;
    public static ConfigOption<float> BlissfulVisageReduceTimerOnKillInit;
    public static ConfigOption<float> BlissfulVisageReduceTimerOnKillStack;
    public static ConfigOption<float> BlissfulVisageSuicideTimer;

    public override string ItemName => "Blissful Visage";

    public override string ItemLangTokenName => "SECONDMOONMOD_GHOSTONKILLVOID";

    public override string ItemPickupDesc => $"Periodically summon a <style=cIsVoid>corrupted</style> ghost of yourself - kills reduce this timer. <style=cIsVoid>Corrupts all Happiest Masks</style>.";

    public override string ItemFullDesc => $"Every {BlissfulVisageGhostCooldown} seconds, summon a <style=cIsVoid>corrupted ghost</style> of yourself that inherits your items. Kills reduce this timer by <style=cIsDamage>{BlissfulVisageReduceTimerOnKillInit}s</style> <style=cStack>(+{BlissfulVisageReduceTimerOnKillStack}s per stack)</style>. Lasts <style=cIsDamage>{BlissfulVisageSuicideTimer}s</style>. <style=cIsVoid>Corrupts all Happiest Masks</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.BrotherBlacklist, ItemTag.CannotCopy, ItemTag.OnKillEffect];

    public override ItemDef ItemToCorrupt => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/GhostOnKill/GhostOnKill.asset").WaitForCompletion();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {

    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        BlissfulVisageGhostCooldown = config.ActiveBind("Item: " + ItemName, "Cooldown for ghost to respawn", 120f, "How many seconds need to pass before the ghost respwans?");
        BlissfulVisageReduceTimerOnKillInit = config.ActiveBind("Item: " + ItemName, "Ghost cooldown reduction on kill with one " + ItemName, 0.5f, "How many seconds should the ghost cooldown be reduced by upon getting a kill with one Blissful Visage?");
        BlissfulVisageReduceTimerOnKillStack = config.ActiveBind("Item: " + ItemName, "Ghost cooldown reduction on kill per stack after one " + ItemName, 0.5f, "How many seconds should the ghost cooldown be reduced by upon getting a kill per stack of Blissful Visage after one?");
        BlissfulVisageSuicideTimer = config.ActiveBind("Item: " + ItemName, "Ghost lifespan", 30f, "How many seconds should the ghost last?");
    }
}

public class BlissfulVisageSuicideComponent : Item<BlissfulVisageSuicideComponent>
{
    public override string ItemName => "BlissfulVisageSuicideComponent";

    public override string ItemLangTokenName => "SECONDMOONMOD_GHOSTONKILLVOID_SUICIDE";

    public override string ItemPickupDesc => "";

    public override string ItemFullDesc => "";

    public override string ItemLore => "";

    public override ItemTierDef ItemTierDef => null;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility, ItemTag.CannotCopy];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += BlissfulVisageAddSuicideItemBehavior;
    }

    private void BlissfulVisageAddSuicideItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        if (NetworkServer.active)
        {
            self.AddItemBehavior<BlissfulVisageSuicideComponentBehavior>(self.inventory.GetItemCount(instance.ItemDef));
        }
        orig(self);
    }

    public override void Init(ConfigFile config)
    {
        EnableCheck = BlissfulVisage.instance.EnableCheck;
        CreateLang();
        CreateItem();
        Hooks();
    }

    public class BlissfulVisageSuicideComponentBehavior : CharacterBody.ItemBehavior
    {
        private float suicideTimer;

        private void Awake()
        {
            enabled = false;
        }
        private void Start()
        {
            suicideTimer = BlissfulVisage.BlissfulVisageSuicideTimer;
        }

        private void FixedUpdate()
        {
            suicideTimer -= Time.deltaTime;
            if (suicideTimer <= 0)
            {
                if (body.HasBuff(DLC1Content.Buffs.EliteVoid))
                {
                    body.RemoveBuff(DLC1Content.Buffs.EliteVoid);
                }
                body.healthComponent.Suicide();
            }
        }
    }
}

