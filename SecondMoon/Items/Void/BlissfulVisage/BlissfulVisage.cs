using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Skills;
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
    public static float BlissfulVisageGhostCooldown = 120f;
    public static float BlissfulVisageReduceTimerOnKillInit = 0.5f;
    public static float BlissfulVisageReduceTimerOnKillStack = 0.5f;
    public static float BlissfulVisageSuicideTimer = 30f;

    public override string ItemName => "Blissful Visage";

    public override string ItemLangTokenName => "SECONDMOONMOD_GHOSTONKILLVOID";

    public override string ItemPickupDesc => $"Periodically summon a <style=cIsVoid>corrupted</style> ghost of yourself - kills reduce this timer. <style=cIsVoid>Corrupts all Happiest Masks</style>.";

    public override string ItemFullDesc => $"Every {BlissfulVisageGhostCooldown} seconds, summon a <style=cIsVoid>corrupted ghost</style> of yourself that inherits your items. Kills reduce this timer by <style=cIsDamage>{BlissfulVisageReduceTimerOnKillInit}s</style> <style=cStack>(+{BlissfulVisageReduceTimerOnKillStack}s per stack)</style>. Lasts <style=cIsDamage>{BlissfulVisageSuicideTimer}s</style>. <style=cIsVoid>Corrupts all Happiest Masks</style>.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.VoidTier3;

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

    public override void Init()
    {
        CreateLang();
        CreateItem();
    }

    public class BlissfulVisageSuicideComponent : Item<BlissfulVisageSuicideComponent>
    {
        public override string ItemName => "BlissfulVisageSuicideComponent";

        public override string ItemLangTokenName => "SECONDMOONMOD_GHOSTONKILLVOID_SUICIDE";

        public override string ItemPickupDesc => "";

        public override string ItemFullDesc => "";

        public override string ItemLore => "";

        public override ItemTier ItemTier => ItemTier.NoTier;

        public override ItemTag[] Category => [ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.CannotCopy, ItemTag.CannotDuplicate];

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

        public override void Init()
        {
            CreateLang();
            CreateItem();
            Hooks();
        }


        public class BlissfulVisageSuicideComponentBehavior : CharacterBody.ItemBehavior
        {
            private float suicideTimer;

            private void Awake()
            {
                base.enabled = false;
            }
            private void Start()
            {
                suicideTimer = BlissfulVisageSuicideTimer;
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
}
