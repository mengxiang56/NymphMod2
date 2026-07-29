using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphNewGenerationConception : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    public NymphNewGenerationConception()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int count = Math.Max(
            0,
            CardPile.MaxCardsInHand
            - PileType.Hand.GetPile(Owner).Cards.Count);
        List<CardModel> cards = [];
        for (int i = 0; i < count; i++)
        {
            cards.Add(
                Owner.Creature.CombatState!
                    .CreateCard<NymphUntoldMatter>(Owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(
            cards,
            PileType.Hand,
            Owner);
        PlayerCmd.EndTurn(
            Owner,
            canBackOut: false);
    }

    protected override void OnUpgrade()
    {
    }
}
