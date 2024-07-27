using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Void.GlassSpinner;

public class GlassSpinner : Item<GlassSpinner>
{
    public static ConfigOption<float> GlassSpinnerRadius;
    public static ConfigOption<float> GlassSpinnerDamageInit;
    public static ConfigOption<float> GlassSpinnerDamageStack;

    public override string ItemName => "Glass Spinner";

    public override string ItemLangTokenName => "SECONDMOONMOD_NEARBYDAMAGEBONUSVOID";

    public override string ItemPickupDesc => "Deal bonus damage to enemies at a distance. <style=cIsVoid>Corrupts all Focus Crystals</style>.";

    public override string ItemFullDesc => $"Increase damage to enemies at least <style=cIsDamage>{GlassSpinnerRadius}m</style> away by <style=cIsDamage>{GlassSpinnerDamageInit * 100}%</style> <style=cStack>(+{GlassSpinnerDamageStack * 100}% per stack)</style>. " +
        $"<style=cIsVoid>Corrupts all Focus Crystals</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDef ItemToCorrupt => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/NearbyDamageBonus/NearbyDamageBonus.asset").WaitForCompletion();
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamage += GlassSpinnerModifyDamage;
    }

    private void GlassSpinnerModifyDamage(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdloc(0),
                               x => x.MatchCallvirt<CharacterMaster>("get_inventory"),
                               x => x.MatchLdsfld(typeof(DLC1Content.Items), nameof(DLC1Content.Items.FragileDamageBonus))))
        {
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(25)))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, 6);
                cursor.EmitDelegate<Func<DamageInfo, float, float>>((info, num) =>
                {
                    if (info.attacker)
                    {
                        var attackerBody = info.attacker.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            var stackCount = GetCount(attackerBody);
                            if (stackCount > 0)
                            {
                                var position = attackerBody.corePosition - info.position;
                                if (position.sqrMagnitude >= Math.Pow(GlassSpinnerRadius, 2))
                                {
                                    info.damageColorIndex = DamageColorIndex.Void;
                                    num *= 1 + (GlassSpinnerDamageInit + ((stackCount - 1) * GlassSpinnerDamageStack));
                                    EffectManager.SimpleImpactEffect(HealthComponent.AssetReferences.diamondDamageBonusImpactEffectPrefab, info.position, position, true);
                                }
                            }
                        }
                    }
                    return num;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, 6);
            }
        }
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        GlassSpinnerRadius = config.ActiveBind("Item: " + ItemName, "Effect radius", 30f, "Deal bonus damage to enemies at least this many meters away.");
        GlassSpinnerDamageInit = config.ActiveBind("Item: " + ItemName, "Damage increase with one " + ItemName, 0.2f, "How much should damage dealt be increased by with one " + ItemName + "? (0.2 = 20%)");
        GlassSpinnerDamageStack = config.ActiveBind("Item: " + ItemName, "Damage increase per stack after one " + ItemName, 0.2f, "How much should damage dealt be increased by per stack of " + ItemName + " after one? (0.2 = 20%)");
    }
}
