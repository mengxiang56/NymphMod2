using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphSourcelessWater : ModCardTemplate, IHasMetaBenefit
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreatePotionId)
    ];

    public NymphSourcelessWater()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        List<(PotionModel Potion, int Slot)> potions = Owner.PotionSlots
            .Select((potion, slot) => (potion, slot))
            .Where(entry => entry.potion is not null)
            .Select(entry => (entry.potion!, entry.slot))
            .ToList();

        if (potions.Count == 0)
        {
            await ProcureRandomPotion();
            return;
        }

        foreach ((PotionModel potion, int _) in potions)
        {
            await PotionCmd.Discard(potion);
        }

        foreach ((PotionModel _, int slot) in potions)
        {
            await ProcureRandomPotion(slot);
        }
    }

    private async Task ProcureRandomPotion(int slot = -1)
    {
        PotionModel potion = PotionFactory.CreateRandomPotionInCombat(
            Owner,
            Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner, slot);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
