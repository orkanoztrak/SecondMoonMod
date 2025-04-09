using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using SecondMoon.Utils;
using System;
using System.Collections;
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

    public override string ItemLangTokenName => "GHOSTONKILLVOID";

    public override string ItemPickupDesc => $"Periodically summon a <style=cIsVoid>corrupted</style> ghost of yourself - kills reduce this timer. <style=cIsVoid>Corrupts all Happiest Masks</style>.";

    public override string ItemFullDesc => $"Every <style=cIsUtility>{BlissfulVisageGhostCooldown}s</style>, summon a <style=cIsVoid>corrupted</style> ghost of yourself that inherits your items. Kills reduce this timer by <style=cIsDamage>{BlissfulVisageReduceTimerOnKillInit}s</style> <style=cStack>(+{BlissfulVisageReduceTimerOnKillStack}s per stack)</style>. Lasts <style=cIsDamage>{BlissfulVisageSuicideTimer}s</style>. <style=cIsVoid>Corrupts all Happiest Masks</style>.";

    public override string ItemLore => "<style=cMono>//--AUT?O-TR??AN?SCRI?PTI??ON FR??OM H???ER?E? --//\r\n\r\n</style>" +
        "”...uoy llet I ,tenalp sihT“ .nugtohs sih dedaol eh sa htaerb sih rednu desruc nam ehT\r\n\r\n" +
        ".wolg yldlrowrehto na htiw wolg ot nageb shtuom riehT .moor eht otni pets reilrae dellik dah eh snairumeL eht was dna denrut nam eht ,daerd fo esnes a htiW ”?...uoy era tahW“\r\n\r\n" +
        ".nam eht ta yltcerid gnitniop – regnif sih desiar reidlos eht ,ylwolS .devom ydob pmil s’reidlos ehT\r\n\r\n" +
        "”--ydaerla tser a ti evig os ,selteeB ot txen pu gnikaw fo derit m’I !ksam eht revo dnah dias I ?!em raeh uoy nac ,yeH“ .sekahs hguor wef a mih gnivig ,teef sih ot reidlos eht detsioh dna detnurg nam ehT .esnopser oN\r\n\r\n" +
        "”.revo ti kroF .daed eht htiw gniyalp nuf hguone dah ev’ew ,thgirlA“\r\n\r\n" +
        ".reidlos eht tas renroc eht ni dna ,ffo erew sthgil ehT .nepo rood eht demmals nam eht ,pmI na fo tsohg eht hguorht gnippetS .hguone saw hguone tuB\r\n\r\n" +
        ".etam-moor yltsohg a gnivah ot demotsucca nworg dah kcolb siht ni srebmem werc eht fo tsom taht ecalpnommoc os emoceb dah sihT .nam eht morf ecnalg dnoces a gninrae ylerab ,mih yb llah eht nwod deklaw yad taht reilrae dellik dah eh snairumeL ehT .skcarrab eht sdrawot llah eht nwod degdurt nam ehT ”.ti fo erac ekat ll’I“\r\n\r\n" +
        " .nirg yppah yllufniap a htiw denroda ,ksam elpmis a – thgiL tcatnoC eht draoba neeb evah ot thguoht tcafitra na derevocer dah - reidlos elpmis a – srebmem werc eht fo eno ,noitidepxe enituor a retfA .dehgis nam ehT\r\n\r\n" +
        "”.kcab era stsohg eht ,riS“";

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
        On.RoR2.GlobalEventManager.OnCharacterDeath += BlissfulVisageReduceGhostTimerOnKill;
    }

    private void BlissfulVisageReduceGhostTimerOnKill(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
    {
        if (damageReport != null)
        {
            var body = damageReport.attackerBody;
            if (body) 
            {
                var stackCount = GetCount(body);
                var component = body.gameObject.GetComponent<BlissfulVisageBodyBehavior>();
                if (stackCount > 0 && component)
                {
                    component.ghostResummonCooldown -= BlissfulVisageReduceTimerOnKillInit + ((stackCount - 1) * BlissfulVisageReduceTimerOnKillStack);
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
        BlissfulVisageGhostCooldown = config.ActiveBind("Item: " + ItemName, "Cooldown for ghost to respawn", 120f, "How many seconds need to pass before the ghost respwans?");
        BlissfulVisageReduceTimerOnKillInit = config.ActiveBind("Item: " + ItemName, "Ghost cooldown reduction on kill with one " + ItemName, 0.5f, "How many seconds should the ghost cooldown be reduced by upon getting a kill with one " + ItemName + "?");
        BlissfulVisageReduceTimerOnKillStack = config.ActiveBind("Item: " + ItemName, "Ghost cooldown reduction on kill per stack after one " + ItemName, 0.5f, "How many seconds should the ghost cooldown be reduced by upon getting a kill per stack of " + ItemName + " after one?");
        BlissfulVisageSuicideTimer = config.ActiveBind("Item: " + ItemName, "Ghost lifespan", 30f, "How many seconds should the ghost last?");
    }
}

public class BlissfulVisageSuicideComponent : Item<BlissfulVisageSuicideComponent>
{
    public override string ItemName => "BlissfulVisageSuicideComponent";

    public override string ItemLangTokenName => "GHOSTONKILLVOID_SUICIDE";

    public override string ItemPickupDesc => "";

    public override string ItemFullDesc => "";

    public override string ItemLore => "";

    public override ItemTierDef ItemTierDef => null;

    public override ItemTag[] Category => [ItemTag.CannotCopy];

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
        if (!NetworkServer.active) return;
        self.AddItemBehavior<BlissfulVisageSuicideComponentBehavior>(self.inventory.GetItemCount(instance.ItemDef));
        orig(self);
    }

    public override void Init(ConfigFile config)
    {
        EnableCheck = BlissfulVisage.instance.EnableCheck;
        if (EnableCheck)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
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
            suicideTimer -= Time.fixedDeltaTime;
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

