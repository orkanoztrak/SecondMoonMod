using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Void.MirrorVeil;

public class MirrorVeil : Item<MirrorVeil>
{
    public static ConfigOption<float> MirrorVeilInvisDurationInit;
    public static ConfigOption<float> MirrorVeilInvisDurationStack;

    public static ConfigOption<float> MirrorVeilInvisProcChance;
    public override string ItemName => "Mirror Veil";

    public override string ItemLangTokenName => "SECONDMOONMOD_STEALTHKITVOID";

    public override string ItemPickupDesc => "Chance on hit to gain invisibility. <style=cIsVoid>Corrupts all Old War Stealthkits</style>.";

    public override string ItemFullDesc => $"Hits have a <style=cIsUtility>{MirrorVeilInvisProcChance}%</style> chance to grant you <style=cIsUtility>invisibility</style> and <style=cIsUtility>40% movement speed</style> " +
        $"for <style=cIsUtility>{MirrorVeilInvisDurationInit}s</style> <style=cStack>(+{MirrorVeilInvisDurationStack}s per stack)</style>. " +
        $"<style=cIsVoid>Corrupts all Old War Stealthkits</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.AIBlacklist];

    public override ItemDef ItemToCorrupt => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Phasing/Phasing.asset").WaitForCompletion();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnHitEnemy += MirrorVeilProcInvis;
    }

    private void MirrorVeilProcInvis(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var attacker = attackerBody.master;
                if (attacker)
                {
                    var stackCount = GetCount(attacker);
                    if (stackCount > 0)
                    {
                        if (Util.CheckRoll(MirrorVeilInvisProcChance * damageInfo.procCoefficient, attacker))
                        {
                            attackerBody.AddTimedBuffAuthority(RoR2Content.Buffs.Cloak.buffIndex, MirrorVeilInvisDurationInit + ((stackCount - 1) * MirrorVeilInvisDurationStack));
                            attackerBody.AddTimedBuffAuthority(RoR2Content.Buffs.CloakSpeed.buffIndex, MirrorVeilInvisDurationInit + ((stackCount - 1) * MirrorVeilInvisDurationStack));
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
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
        MirrorVeilInvisDurationInit = config.ActiveBind("Item: " + ItemName, "Invisibility duration with one " + ItemName, 2f, "Invisibility lasts this many seconds.");
        MirrorVeilInvisDurationStack = config.ActiveBind("Item: " + ItemName, "Additional invisibility duration per stack after one " + ItemName, 1f, "Invisibility is extended by this many seconds.");
        MirrorVeilInvisProcChance = config.ActiveBind("Item: " + ItemName, "Invisibility proc chance", 7f, "Invisibility will proc this % of the time on hit.");
    }
}
