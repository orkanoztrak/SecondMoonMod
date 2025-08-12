using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.UI;
using SecondMoon.Items.Prototype.GravityFlaskHealing;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SecondMoon.Items.Prototype.GravityFlaskDamage;

public class GravityFlaskDamage : Item<GravityFlaskDamage>
{
    public static ConfigOption<int> GravityFlaskDamageFirstThreshold;
    public static ConfigOption<int> GravityFlaskDamageSecondThreshold;
    public static ConfigOption<int> GravityFlaskDamageThirdThreshold;
    public static ConfigOption<int> GravityFlaskDamageFinalThreshold;

    public static ConfigOption<float> GravityFlaskDamageAttackSpeedInit;
    public static ConfigOption<float> GravityFlaskDamageAttackSpeedStack;
    public static ConfigOption<float> GravityFlaskDamageDamageInit;
    public static ConfigOption<float> GravityFlaskDamageDamageStack;
    public static ConfigOption<float> GravityFlaskDamageCritInit;
    public static ConfigOption<float> GravityFlaskDamageCritStack;
    public static ConfigOption<float> GravityFlaskDamageProcDamageInit;
    public static ConfigOption<float> GravityFlaskDamageProcDamageStack;
    public static ConfigOption<float> GravityFlaskDamageProcCoefficient;

    public override string ItemName => "Test";

    public override string ItemLangTokenName => "GRAVITY_FLASK_DAMAGE";

    public override string ItemPickupDesc => "Gather \"Damage\" items to gain different bonuses.";

    public override string ItemFullDesc => "Every <style=cIsDamage>1 Damage item</style> you have gives you <style=cIsDamage>2% <style=cStack>(+2% per stack)</style> attack speed</style>.\r\n\r\n" +
        "Every <style=cIsDamage>5 Damage items</style> you have gives you <style=cIsDamage>10% <style=cStack>(+10% per stack)</style> critical strike chance</style>.\r\n\r\n" +
        "Every <style=cIsDamage>10 Damage items</style> you have gives you <style=cIsDamage>20% <style=cStack>(+20% per stack)</style> base damage</style>.\r\n\r\n" +
        "If you have at least 20 Damage items, attacks smite enemies for <style=cIsDamage>50%</style> <style=cStack>(+50% per stack)</style> TOTAL damage.";

    public override string ItemLore => throw new NotImplementedException();

    public override ItemTierDef ItemTierDef => throw new NotImplementedException();

    public override ItemTag[] Category => throw new NotImplementedException();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        throw new NotImplementedException();
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        RecalculateStatsAPI.GetStatCoefficients += GravityFlaskDamageBuffStats;
        On.RoR2.GlobalEventManager.ProcessHitEnemy += GravityFlaskDamageSmite;
        IL.RoR2.UI.ItemIcon.ItemClicked += DynamicItemDescription;
    }

    private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        if (NetworkServer.active)
        {
            self.AddItemBehavior<GravityFlaskDamageBehavior>(self.inventory.GetItemCount(instance.ItemDef));
        }
        orig(self);
    }

    private void GravityFlaskDamageBuffStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            var tracker = sender.gameObject.GetComponent<GravityFlaskDamageBehavior>();
            if (tracker)
            {
                args.attackSpeedMultAdd += (GravityFlaskDamageAttackSpeedInit + ((stackCount - 1) * GravityFlaskDamageAttackSpeedStack)) * (tracker.GravityFlaskDamageTracker / GravityFlaskDamageFirstThreshold);
                args.critAdd += (GravityFlaskDamageCritInit + ((stackCount - 1) * GravityFlaskDamageCritStack)) * (tracker.GravityFlaskDamageTracker / GravityFlaskDamageSecondThreshold);
                args.damageMultAdd += (GravityFlaskDamageDamageInit + ((stackCount - 1) * GravityFlaskDamageDamageStack)) * (tracker.GravityFlaskDamageTracker / GravityFlaskDamageThirdThreshold);
            }
        }
    }

    private void GravityFlaskDamageSmite(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var attacker = attackerBody.master;
            var victimBody = victim.GetComponent<CharacterBody>();
            if (attacker && attackerBody && victimBody)
            {
                var stackCount = GetCount(attacker);
                if (stackCount > 0)
                {
                    var tracker = attackerBody.gameObject.GetComponent<GravityFlaskDamageBehavior>();
                    if (tracker)
                    {
                        if (tracker.GravityFlaskDamageTracker >= GravityFlaskDamageFinalThreshold && Util.CheckRoll(100 * damageInfo.procCoefficient, attacker))
                        {
                            var smiteDamage = GravityFlaskDamageProcDamageInit + ((stackCount - 1) * GravityFlaskDamageProcDamageStack);
                            var smiteOrb = new GravityFlaskDamageSmiteOrb
                            {
                                attacker = attackerBody.gameObject,
                                damageColorIndex = DamageColorIndex.Item,
                                damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, smiteDamage),
                                isCrit = damageInfo.crit,
                                procChainMask = damageInfo.procChainMask,
                            };
                            HurtBox target = victimBody.mainHurtBox;
                            if (target)
                            {
                                if ((bool)victimBody.hurtBoxGroup)
                                {
                                    target = victimBody.hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, victimBody.hurtBoxGroup.hurtBoxes.Length)];
                                }
                                smiteOrb.target = target;
                                OrbManager.instance.AddOrb(smiteOrb);
                                DamageInfo newDamageInfo = new()
                                {
                                    damage = smiteOrb.damageValue,
                                    crit = smiteOrb.isCrit,
                                    inflictor = damageInfo.inflictor,
                                    attacker = smiteOrb.attacker,
                                    position = damageInfo.position,
                                    force = damageInfo.force,
                                    rejected = damageInfo.rejected,
                                    procChainMask = damageInfo.procChainMask,
                                    procCoefficient = GravityFlaskDamageProcCoefficient,
                                    damageType = smiteOrb.damageType,
                                    damageColorIndex = smiteOrb.damageColorIndex
                                };
                                orig(self, newDamageInfo, victim);
                                GlobalEventManager.instance.OnHitAllProcess(newDamageInfo, victim);
                            }
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
    }

    private void DynamicItemDescription(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
            x => x.MatchLdfld(typeof(ItemIcon), nameof(RoR2.UI.ItemIcon.OnItemClicked)),
            x => x.MatchDup()))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoPrev(x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<ItemIcon>("get_inspectPanel"),
                x => x.MatchLdloc(0)))
            {
                cursor.MoveBeforeLabels();
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0);
                cursor.EmitDelegate<Func<ItemIcon, ItemDef, bool>>((icon, itemDef) =>
                {
                    if (icon && itemDef)
                    {
                        var panel = icon.inspectPanel;
                        if (panel && itemDef == ItemDef)
                        {
                            var inventory = icon.gameObject.GetComponentInParent<ItemInventoryDisplay>();
                            if (inventory)
                            {
                                var master = GeneralUtils.FindMasterByInventory(inventory.inventory);
                                var bodyObject = master.GetBodyObject();
                                if (bodyObject)
                                {
                                    var tracker = bodyObject.GetComponent<GravityFlaskDamageBehavior>();
                                    if (tracker)
                                    {
                                        var stringToDisplay = ConstructInspectString(tracker, inventory.inventory);
                                        InspectInfo info = new InspectInfo
                                        {
                                            Visual = itemDef.pickupIconSprite,
                                            TitleToken = itemDef.nameToken,
                                            DescriptionToken = stringToDisplay,
                                            FlavorToken = itemDef.loreToken,
                                            TitleColor = ColorCatalog.GetColor(ItemTierCatalog.GetItemTierDef(itemDef.tier)?.colorIndex ?? ColorCatalog.ColorIndex.Tier1Item),
                                            isDynamicInspectInfo = true,
                                            dynamicInspectPickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex),
                                            isConsumedItem = itemDef.isConsumed
                                        };
                                        panel.Show(info);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    return false;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
            }
        }
        Debug.Log(il.ToString());
    }

    private string ConstructInspectString(GravityFlaskDamageBehavior tracker, Inventory inventory)
    {
        if (tracker && inventory)
        {
            var stackCount = inventory.GetItemCount(ItemDef);
            if (stackCount > 0)
            {
                var finalString = $"<color=#7CFDEA>The following bonuses are applied to you: </color>\r\n";
                if (GravityFlaskDamageFirstThreshold <= tracker.GravityFlaskDamageTracker)
                {
                    finalString += $"• Gain <style=cIsDamage>{(GravityFlaskDamageAttackSpeedInit + (stackCount - 1) * GravityFlaskDamageAttackSpeedStack) * (tracker.GravityFlaskDamageTracker / GravityFlaskDamageFirstThreshold) * 100}%</style> attack speed." +
                        $" <color=#7CFDEA>{GravityFlaskDamageFirstThreshold - (tracker.GravityFlaskDamageTracker % GravityFlaskDamageFirstThreshold)} more Damage item(s) to upgrade!</color>\r\n\r\n";
                }
                if (GravityFlaskDamageSecondThreshold <= tracker.GravityFlaskDamageTracker)
                {
                    finalString += $"• Gain <style=cIsDamage>{(GravityFlaskDamageCritInit + (stackCount - 1) * GravityFlaskDamageCritStack) * (tracker.GravityFlaskDamageTracker / GravityFlaskDamageSecondThreshold) * 100}%</style> critical strike chance." +
                        $" <color=#7CFDEA>{GravityFlaskDamageSecondThreshold - (tracker.GravityFlaskDamageTracker % GravityFlaskDamageSecondThreshold)} more Damage item(s) to upgrade!</color>\r\n\r\n";
                }
                if (GravityFlaskDamageThirdThreshold <= tracker.GravityFlaskDamageTracker)
                {
                    finalString += $"• Gain <style=cIsDamage>{(GravityFlaskDamageDamageInit + (stackCount - 1) * GravityFlaskDamageDamageStack) * (tracker.GravityFlaskDamageTracker / GravityFlaskDamageThirdThreshold)}%</style> base damage." +
                        $" <color=#7CFDEA>{GravityFlaskDamageThirdThreshold - (tracker.GravityFlaskDamageTracker % GravityFlaskDamageThirdThreshold)} more Damage item(s) to upgrade!</color>\r\n\r\n";
                }
                if (GravityFlaskDamageFinalThreshold <= tracker.GravityFlaskDamageTracker)
                {
                    finalString += $"• Hits smite enemies for <style=cIsDamage>{(GravityFlaskDamageProcDamageInit + (stackCount - 1) * GravityFlaskDamageProcDamageStack) * 100}%</style> TOTAL damage.\r\n";
                }
                return finalString;
            }
        }
        return "";
    }


    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            OrbAPI.AddOrb(typeof(GravityFlaskDamageSmiteOrb));
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        GravityFlaskDamageFirstThreshold = config.ActiveBind("Item: " + ItemName, "First threshold", 1, "Every this many items the holder has of a category grants the first respective category boost.");
        GravityFlaskDamageSecondThreshold = config.ActiveBind("Item: " + ItemName, "Second threshold", 5, "Every this many items the holder has of a category grants the second respective category boost.");
        GravityFlaskDamageThirdThreshold = config.ActiveBind("Item: " + ItemName, "Third threshold", 10, "Every this many items the holder has of a category grants the third respective category boost.");
        GravityFlaskDamageFinalThreshold = config.ActiveBind("Item: " + ItemName, "Final threshold", 20, "Having at least this many items of a category grants the final respective category boost.");

        GravityFlaskDamageAttackSpeedInit = config.ActiveBind("Item: " + ItemName, "Attack speed with one " + ItemName, 0.02f, "How much should attack speed be increased by with one " + ItemName + "? (0.02 = 2%)");
        GravityFlaskDamageAttackSpeedStack = config.ActiveBind("Item: " + ItemName, "Attack speed per stack after one " + ItemName, 0.02f, "How much should attack speed be increased by per stack of " + ItemName + " after one? (0.02 = 2%)");
        GravityFlaskDamageDamageInit = config.ActiveBind("Item: " + ItemName, "Damage with one " + ItemName, 0.1f, "How much should damage be increased by with one " + ItemName + "? (0.1 = 10%)");
        GravityFlaskDamageDamageStack = config.ActiveBind("Item: " + ItemName, "Damage per stack after one " + ItemName, 0.1f, "How much should damage be increased by per stack of " + ItemName + " after one? (0.1 = 10%)");
        GravityFlaskDamageCritInit = config.ActiveBind("Item: " + ItemName, "Critical chance with one " + ItemName, 20f, "How much should critical chance be increased by with one " + ItemName + "?");
        GravityFlaskDamageCritStack = config.ActiveBind("Item: " + ItemName, "Critical per stack after one " + ItemName, 20f, "How much should critical chance be increased by per stack of " + ItemName + " after one?");
        GravityFlaskDamageProcDamageInit = config.ActiveBind("Item: " + ItemName, "Damage of the proc with one " + ItemName, 0.5f, "What % of TOTAL damage should the proc do with one " + ItemName + "? (0.5 = 50%)");
        GravityFlaskDamageProcDamageStack = config.ActiveBind("Item: " + ItemName, "Damage of the proc per stack after one " + ItemName, 0.5f, "What % of TOTAL damage should be added to the proc per stack of " + ItemName + " after one? (0.5 = 50%)");
        GravityFlaskDamageProcCoefficient = config.ActiveBind("Item: " + ItemName, "Proc coefficient of the proc", 1f, "What should the proc coefficient of the proc be?");

    }

    public class GravityFlaskDamageBehavior : CharacterBody.ItemBehavior
    {
        public int GravityFlaskDamageTracker;

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                body.inventory.onInventoryChanged += UpdateTrackers;
                UpdateTrackers();
            }
        }

        private void UpdateTrackers()
        {
            GravityFlaskDamageTracker = 0;
            if (body)
            {
                if (body.inventory)
                {
                    foreach (var item in body.inventory.itemAcquisitionOrder)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(item);
                        if (itemDef.tier != ItemTier.NoTier)
                        {
                            if (itemDef.ContainsTag(ItemTag.Damage))
                            {
                                GravityFlaskDamageTracker += body.inventory.itemStacks[(int)item];
                            }
                        }
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (body)
            {
                if (body.inventory)
                {
                    body.inventory.onInventoryChanged -= UpdateTrackers;
                }
            }
        }
    }
}
