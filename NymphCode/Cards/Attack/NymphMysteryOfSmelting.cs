using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphMysteryOfSmelting : ModCardTemplate
{
    private bool _autoPlayNextCombat;

    public override bool CanBeGeneratedInCombat => false;

    protected override bool HasEnergyCostX => true;

    [SavedProperty]
    public bool AutoPlayNextCombat
    {
        get => _autoPlayNextCombat;
        set
        {
            AssertMutable();
            _autoPlayNextCombat = value;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreateId)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move)
    ];

    public NymphMysteryOfSmelting()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, true)
    {
    }

    public override async Task AfterAutoPrePlayPhaseEntered(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner
            || player.PlayerCombatState?.TurnNumber is not <= 1
            || !AutoPlayNextCombat)
        {
            return;
        }

        AutoPlayNextCombat = false;
        if (DeckVersion is NymphMysteryOfSmelting deckVersion)
        {
            deckVersion.AutoPlayNextCombat = false;
        }

        await CardCmd.AutoPlay(choiceContext, this, null);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(ResolveEnergyXValue())
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithAttackerFx(null, "event:/sfx/characters/attack_fire")
            .WithHitVfxNode(target => NFireBurstVfx.Create(target, 0.6f))
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
