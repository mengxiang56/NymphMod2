#if STS2_PUBLIC
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Nymph.Cards;

internal static class PublicAttackCommandCompatibility
{
    internal static AttackCommand FromCard(
        this AttackCommand command,
        CardModel card,
        CardPlay cardPlay)
    {
        return command.FromCard(card);
    }
}
#endif
