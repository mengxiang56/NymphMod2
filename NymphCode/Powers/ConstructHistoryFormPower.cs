using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class ConstructHistoryFormPower : ModPowerTemplate
{
    private int _upgradedDiscoveries;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/cards/NymphConstructHistoryForm.png",
        BigIconPath: $"{Entry.ResPath}/images/cards/NymphConstructHistoryForm.png");

    [SavedProperty]
    public int UpgradedDiscoveries
    {
        get => _upgradedDiscoveries;
        set
        {
            AssertMutable();
            _upgradedDiscoveries = Math.Max(0, value);
        }
    }

    public void RegisterSource(bool upgraded)
    {
        if (upgraded)
        {
            UpgradedDiscoveries++;
        }
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        for (int i = 0; i < Amount; i++)
        {
            await DiscoveryMechanics.Discover(
                choiceContext,
                player,
                upgraded: i < UpgradedDiscoveries);
        }
    }
}
