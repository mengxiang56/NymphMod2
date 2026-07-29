using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Afflictions;

[RegisterAffliction]
public sealed class CursePollutionAffliction : ModAfflictionTemplate
{
    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<NymphDreadkaz>()
    ];

    public override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        Creature? target)
    {
        CardModel dreadkaz =
            Card.CombatState!.CreateCard<NymphDreadkaz>(Card.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            dreadkaz,
            PileType.Hand,
            Card.Owner);
        CardCmd.ClearAffliction(Card);
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Card.Owner.Creature))
        {
            CardCmd.ClearAffliction(Card);
        }

        return Task.CompletedTask;
    }
}
