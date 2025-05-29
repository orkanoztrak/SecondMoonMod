using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Equipment.AffixGuardian;
using SecondMoon.Items.ItemTiers.VoidTierPrototypeDormant;
using UnityEngine.Networking;

namespace SecondMoon.Items.Void.TwistedRegrets;

public class CoreOfCorruption : Item<CoreOfCorruption>
{

    public override string ItemName => "Core of Corruption";

    public override string ItemLangTokenName => "TWISTED_REGRETS_DORMANT";

    public override string ItemPickupDesc => "Provides no benefit. <color=#7CFDEA>Awaken this item to reveal its true power...</color><style=cIsVoid>but beware of corrupted enemies.</style>";

    public override string ItemFullDesc => $"Any enemy spawned has a <style=cIsVoid>{TwistedRegrets.CoreOfCorruptionVoidCorruptionChanceInit}%</style> <style=cStack>(+{TwistedRegrets.CoreOfCorruptionVoidCorruptionChanceStack}% per stack across the team)</style> chance to gain <style=cIsVoid>Void elite</style> powers. These enemies will also keep their original powers, if any. " +
        $"Can be given to the <color=#7CFDEA>Awakening Shrine</color> to add <style=cIsVoid>Void elite</style> powers to its boss and set its reward to <color=#7CFDEA>{TwistedRegrets.instance.ItemName}</color>.";

    public override string ItemLore => "";

    public override ItemTierDef ItemTierDef => VoidTierPrototypeDormant.instance.ItemTierDef;

    public override ItemTag[] Category => [];

    public override ItemIndex ActivateIntoPrototypeItem => TwistedRegrets.instance.ItemDef.itemIndex;
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterMaster.OnBodyStart += CheckForCorruption;
    }

    private void CheckForCorruption(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
    {
        orig(self, body);
        if (!NetworkServer.active) return;
        if (!self.isBoss && self.teamIndex != TeamIndex.Player)
        {
            int stacks = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                stacks += pcmc.master.inventory.GetItemCount(ItemDef);
            }
            if (stacks > 0)
            {
                if (Util.CheckRoll(TwistedRegrets.CoreOfCorruptionVoidCorruptionChanceInit + (stacks - 1) * TwistedRegrets.CoreOfCorruptionVoidCorruptionChanceStack, self))
                {
                    body.AddBuff(DLC1Content.Buffs.EliteVoid);
                }
            }
        }
    }

    public override void Init(ConfigFile config)
    {
        EnableCheck = TwistedRegrets.instance.EnableCheck;
        CreateLang();
        CreateItem();
        Hooks();
    }
}
