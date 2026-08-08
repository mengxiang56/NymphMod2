using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphSmallCleverThought : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Recreate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Recreate", 1)
    ];

    public NymphSmallCleverThought()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        IReadOnlyList<RecreateResult> results =
            await RecreateMechanics.SelectFromHand(
                choiceContext,
                this,
                DynamicVars["Recreate"].IntValue,
                DynamicVars["Recreate"].IntValue);
        if (results.Count == 0)
        {
            return;
        }

        CardModel replacement = results[0].Replacement;
        List<EnchantmentModel> enchantments =
            SmallCleverThoughtEnchantments.GetEligibleEnchantments(
                replacement);
        EnchantmentModel? selected =
            Owner.RunState.Rng.CombatCardSelection.NextItem(
                enchantments);
        if (selected is not null)
        {
            int amount = SmallCleverThoughtEnchantments.RollAmount(
                selected,
                Owner.PlayerRng.Rewards);
            CardCmd.Enchant(
                selected.ToMutable(),
                replacement,
                amount);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
