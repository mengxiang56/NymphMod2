using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphThoughtSort : ModCardTemplate
{
    private const string BonusPerThoughtVar = "BonusPerThought";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ThoughtSortDamageVar(6, ValueProp.Move),
        new DynamicVar(BonusPerThoughtVar, 2)
    ];

    public NymphThoughtSort()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        DynamicVars.Damage.UpdateCardPreview(
            this,
            CardPreviewMode.Normal,
            cardPlay.Target,
            runGlobalHooks: true);

        await DamageCmd.Attack(DynamicVars.Damage.PreviewValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[BonusPerThoughtVar].UpgradeValueBy(1);
    }

    private sealed class ThoughtSortDamageVar : DamageVar
    {
        public ThoughtSortDamageVar(decimal damage, ValueProp props)
            : base(damage, props)
        {
        }

        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

            if (card is not NymphThoughtSort thoughtSort)
            {
                return;
            }

            int conceived = card.Owner.Creature
                .GetPower<ConceivedThoughtThisTurnPower>()?.Amount ?? 0;
            int bonusPerThought = thoughtSort
                .DynamicVars[BonusPerThoughtVar].IntValue;
            PreviewValue += conceived * bonusPerThought;
        }
    }
}
