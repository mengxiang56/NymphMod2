using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphFremontEvoke : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Ethereal,
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphFearBlast.png");

    public NymphFremontEvoke()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        FremontMechanicsPower? mechanics = Owner.Creature.CombatState?.Enemies
            .Select(enemy => enemy.GetPower<FremontMechanicsPower>())
            .FirstOrDefault(power => power is not null);
        if (mechanics is not null)
        {
            await mechanics.EvokeAllOrbs(Owner.Creature);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
