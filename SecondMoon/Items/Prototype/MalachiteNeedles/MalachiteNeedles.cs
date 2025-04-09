using R2API;
using RoR2;
using UnityEngine;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using static RoR2.DotController;
using SecondMoon.Utils;
using BepInEx.Configuration;
using UnityEngine.Networking;
using SecondMoon.Items.ItemTiers.TierPrototype;

namespace SecondMoon.Items.Prototype.MalachiteNeedles;

public class MalachiteNeedles : Item<MalachiteNeedles>
{
    public static ConfigOption<float> MalachiteNeedlesCorrosionDmgInit;
    public static ConfigOption<float> MalachiteNeedlesCorrosionDmgStack;
    public static ConfigOption<float> MalachiteNeedlesDOTBurstConversion;

    public override string ItemName => "Malachite Needles";

    public override string ItemLangTokenName => "MALACHITE_NEEDLES";

    public override string ItemPickupDesc => $"Corrode enemies on hit. Damage over time effects deal damage based on their total when applied.";

    public override string ItemFullDesc => $"Hits <color=#8aa626>corrode</color> enemies for <style=cIsDamage>{MalachiteNeedlesCorrosionDmgInit * 100}%</style> <style=cStack>(+{MalachiteNeedlesCorrosionDmgStack * 100}% per stack)</style> TOTAL damage. " +
        $"<color=#7CFDEA>Damage over time effects, in addition to their normal effects, deal damage equal to {MalachiteNeedlesDOTBurstConversion * 100}% of their total when applied</color>.";

    public override string ItemLore => "I'll have to admit that there is some charm in a fighter that relies on strength, like how a puppy is adorable. This type is always the most straightforward in both their thinking and feelings. Fighters like this will always take you head on and aim to overwhelm you by crashing their selves onto you. Everything must be so simple for them, ignorance is bliss after all.\r\n\r\n" +
        "However, in the end, I am one that fights smart, not hard. I'll slip through the cracks and eliminate my target in the most efficient and effective way possible, no matter what it takes. A straightforward fighter is easy prey, they'll always have something obvious that binds them. Relatives, guilt, some creed, whatever it is, it'll always be there. And I'll exploit it to bring them down.\r\n\r\n" +
        "Many call me underhanded and cowardly. I don't care for any of it. What good is valor and dignity if you die at the end? What good are these things if you fail to achieve your objective? It's the delusion of losers. They think they can just make it as if their failure isn't real by pretending that they at least upheld their values, their pride or whatever other nonsense they so foolishly cling to. Let them be that way. Makes my job a lot easier.\r\n\r\n" +
        "- Unknown";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage];
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += MalachiteNeedlesApplyTotalCorrosion;
        On.RoR2.DotController.OnDotStackAddedServer += MalachiteNeedlesDOTBurst;
    }

    private void MalachiteNeedlesDOTBurst(On.RoR2.DotController.orig_OnDotStackAddedServer orig, DotController self, object dotStack)
    {
        orig(self, dotStack);
        var dot = (DotStack)dotStack;
        var attackerBody = dot.attackerObject.GetComponent<CharacterBody>();
        if (attackerBody)
        {
            var stackCount = GetCount(attackerBody);
            if (stackCount > 0)
            {
                DamageInfo damageInfo = new DamageInfo
                {
                    attacker = dot.attackerObject,
                    crit = false,
                    damage = (dot.damage * dot.timer / dot.dotDef.interval) * MalachiteNeedlesDOTBurstConversion,
                    force = Vector3.zero,
                    inflictor = null,
                    position = self.victimBody.corePosition,
                    procCoefficient = 0f,
                    damageColorIndex = dot.dotDef.damageColorIndex,
                    damageType = (DamageType)(dot.damageType | DamageType.DoT)
                };
                self.victimHealthComponent.TakeDamage(damageInfo);
            }
        }
    }

    private void MalachiteNeedlesApplyTotalCorrosion(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0f && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    InflictDotInfo ınflictDotInfo = default;
                    ınflictDotInfo.victimObject = victim;
                    ınflictDotInfo.attackerObject = damageInfo.attacker;
                    ınflictDotInfo.dotIndex = Corrosion.instance.DotIndex;
                    ınflictDotInfo.damageMultiplier = 1f;
                    ınflictDotInfo.totalDamage = damageInfo.damage * (MalachiteNeedlesCorrosionDmgInit + ((stackCount - 1) * MalachiteNeedlesCorrosionDmgStack)) * damageInfo.procCoefficient;
                    InflictDot(ref ınflictDotInfo);
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
        MalachiteNeedlesCorrosionDmgInit = config.ActiveBind("Item: " + ItemName, "Corrosion multiplier with one " + ItemName, 0.5f, "What % of TOTAL damage should Corrosion do with one " + ItemName + "? (0.5 = 50%)");
        MalachiteNeedlesCorrosionDmgStack = config.ActiveBind("Item: " + ItemName, "Corrosion multiplier per stack after one " + ItemName, 0.5f, "What % of TOTAL damage should be added to Corrosion per stack of " + ItemName + " after one? (0.5 = 50%)");
        MalachiteNeedlesDOTBurstConversion = config.ActiveBind("Item: " + ItemName, "DOT burst damage multiplier", 1f, "What % of a damage over time effect's total damage should be dealt as additional damage when it is applied? (1 = 100%)");
    }
}
