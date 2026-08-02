using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphNewBranch : ModCardTemplate
{
    private static readonly Queue<NymphNewBranch> PendingAutoPlays = new();
    private static readonly BlockingPlayerChoiceContext PendingChoiceContext = new();
    private static bool _flushSubscribed;

    private bool _manualPlayStarted;

    public static void EnsureAutoPlayFlushSubscribed()
    {
        if (_flushSubscribed)
        {
            return;
        }

        _flushSubscribed = true;
        RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(
            OnCardPlayedFlushPendingAutoPlays);
    }

    private static void OnCardPlayedFlushPendingAutoPlays(CardPlayedEvent e)
    {
        _ = FlushPendingAutoPlaysAsync();
    }

    private static void RemoveFromPendingAutoPlays(NymphNewBranch card)
    {
        if (PendingAutoPlays.Count == 0)
        {
            return;
        }

        int count = PendingAutoPlays.Count;
        for (int i = 0; i < count; i++)
        {
            NymphNewBranch pending = PendingAutoPlays.Dequeue();
            if (pending != card)
            {
                PendingAutoPlays.Enqueue(pending);
            }
        }
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
        {
            _manualPlayStarted = true;
            RemoveFromPendingAutoPlays(this);
        }

        return Task.CompletedTask;
    }

    private static async Task FlushPendingAutoPlaysAsync()
    {
        await Cmd.Wait(0f);
        while (PendingAutoPlays.Count > 0)
        {
            NymphNewBranch card = PendingAutoPlays.Dequeue();
            if (card._manualPlayStarted
                || card.Pile?.Type != PileType.Hand
                || card.CombatState is null
                || CombatManager.Instance.IsOverOrEnding)
            {
                continue;
            }

            if (CombatManager.Instance.IsExecutingCardOrPotionEffect(card.Owner))
            {
                PendingAutoPlays.Enqueue(card);
                return;
            }

            await card.AutoPlayFromDraw(PendingChoiceContext);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Conceive
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("Create", 3),
        new CardsVar(1)
    ];

    public NymphNewBranch()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy, true)
    {
    }

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card != this || CombatState is null || _manualPlayStarted)
        {
            return;
        }

        if (CombatManager.Instance.IsExecutingCardOrPotionEffect(Owner))
        {
            PendingAutoPlays.Enqueue(this);
            return;
        }

        await AutoPlayFromDraw(choiceContext);
        await FlushPendingAutoPlaysAsync();
    }

    private async Task AutoPlayFromDraw(PlayerChoiceContext choiceContext)
    {
        if (_manualPlayStarted || Pile?.Type != PileType.Hand)
        {
            return;
        }

        Creature? target = Owner.RunState.Rng.CombatTargets.NextItem(
            CombatState!
                .GetOpponentsOf(Owner.Creature)
                .Where(enemy => !enemy.IsDead));
        if (target is null)
        {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, this, target);
        await FlushPendingAutoPlaysAsync();
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            DynamicVars["Create"].IntValue,
            this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.IntValue,
            Owner);

        var copy = CombatState!.CreateCard<NymphNewBranch>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(
                copy,
                MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(
            new CardModel[] { copy },
            PileType.Discard,
            Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
