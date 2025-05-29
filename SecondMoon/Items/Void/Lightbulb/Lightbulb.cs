using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Tier3;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Void.Lightbulb;

public class Lightbulb : Item<Lightbulb>
{
    public static ConfigOption<float> LightbulbInfusionConversionMultiplier;

    public override string ItemName => "Lightbulb";

    public override string ItemLangTokenName => "INFUSIONVOID";

    public override string ItemPickupDesc => $"Killing an enemy permanently increases your shields, up to {LightbulbInfusionConversionMultiplier * 100}. <style=cIsVoid>Corrupts all Infusions</style>.";

    public override string ItemFullDesc => $"Killing an enemy increases your <style=cIsHealing>shields permanently</style> by <style=cIsHealing>{LightbulbInfusionConversionMultiplier}</style> <style=cStack>(+{LightbulbInfusionConversionMultiplier} per stack)</style>, " +
        $"up to a <style=cIsHealing>maximum</style> of <style=cIsHealing>{LightbulbInfusionConversionMultiplier * 100}</style> <style=cStack>(+{LightbulbInfusionConversionMultiplier * 100} per stack). " +
        $"<style=cIsVoid>Corrupts all Infusions</style>.";

    public override string ItemLore => $"Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.OnKillEffect];

    public override ItemDef ItemToCorrupt => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Infusion/Infusion.asset").WaitForCompletion();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnCharacterDeath += LightbulbGainInfusionBonus;
        IL.RoR2.CharacterBody.RecalculateStats += LightbulbBoostShields;
    }

    private void LightbulbBoostShields(ILContext il)
    {
        var maxShieldIndex = 72;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
            x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.EngiShield))))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, maxShieldIndex);
            cursor.EmitDelegate<Func<CharacterBody, float, float>>((body, shield) =>
            {
                var stackCount = GetCount(body);
                if (stackCount > 0 && body.inventory)
                {
                    shield += body.inventory.infusionBonus * LightbulbInfusionConversionMultiplier;
                }
                return shield;
            });
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, maxShieldIndex);
        }
    }

    private void LightbulbGainInfusionBonus(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
    {
        if (damageReport != null)
        {
            var body = damageReport.attackerBody;
            if (body)
            {
                if (body.master)
                {
                    var stackCount = GetCount(damageReport.attackerBody);
                    var infusion = stackCount * 100;
                    if (stackCount > 0 && body.master.inventory.infusionBonus < infusion)
                    {
                        OrbManager.instance.AddOrb(new VoidInfusionOrb
                        {
                            origin = damageReport.victim.gameObject.transform.position,
                            target = Util.FindBodyMainHurtBox(body),
                            maxShieldValue = stackCount
                        });
                    }
                }
            }
        }
        orig(self, damageReport);
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
        LightbulbInfusionConversionMultiplier = config.ActiveBind("Item: " + ItemName, "Multiplier for conversion from Infusion to Lightbulb", 1.5f, "Multiply Infusion's health gain and cap by this to use for Lightbulb's shield gain and cap respectively.");
    }
}
