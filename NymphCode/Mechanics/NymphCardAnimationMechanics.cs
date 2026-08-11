using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using STS2RitsuLib.Cards;

namespace Nymph.Mechanics;

internal static class NymphCardAnimationMechanics
{
    private static bool _initialized;

    internal static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        CardOnPlayHook.RegisterGlobalListener(new TargetedSkillCastListener());
    }

    private sealed class TargetedSkillCastListener : ICardOnPlayHookListener
    {
        public async Task<bool> BeforeCardOnPlay(
            BeforeCardOnPlayContext context)
        {
            CardModel card = context.CardPlay.Card;
            if (card.Owner.Character is NymphCharacter
                && card.Type == CardType.Skill
                && card.TargetType is TargetType.AnyEnemy
                    or TargetType.AllEnemies)
            {
                await CreatureCmd.TriggerAnim(
                    card.Owner.Creature,
                    "Cast",
                    card.Owner.Character.CastAnimDelay);
            }

            return false;
        }
    }
}
