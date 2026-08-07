using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphSeeThroughPast : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [NymphKeywords.Recreate];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new DynamicVar("Hits", 2)
    ];

    public NymphSeeThroughPast()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true)
    {
    }

    public static void AddRecreatedAttackDamage(
        Player player,
        IReadOnlyList<RecreateResult> results)
    {
        int added = results
            .Where(result => result.Original.Type == CardType.Attack)
            .Sum(GetAttackDamage);
        if (added <= 0)
        {
            return;
        }

        foreach (NymphSeeThroughPast card in GetAllInstances(player))
        {
            card.DynamicVars.Damage.BaseValue += added;
            card.DynamicVars.Damage.ResetToBase();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        for (int i = 0; i < DynamicVars["Hits"].IntValue; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static IEnumerable<NymphSeeThroughPast> GetAllInstances(Player player)
    {
        foreach (PileType pileType in CombatPiles)
        {
            foreach (CardModel card in pileType.GetPile(player).Cards)
            {
                if (card is NymphSeeThroughPast seeThroughPast)
                {
                    yield return seeThroughPast;
                }
            }
        }
    }

    private static int GetAttackDamage(RecreateResult result)
    {
        CardModel card = result.Original;
        if (card.Type != CardType.Attack
            || !card.DynamicVars.TryGetValue("Damage", out DynamicVar? damageVar)
            || damageVar is null)
        {
            return 0;
        }

        return damageVar.IntValue;
    }

    private static readonly PileType[] CombatPiles =
    [
        PileType.Hand,
        PileType.Draw,
        PileType.Discard,
        PileType.Exhaust,
        PileType.Play
    ];
}
