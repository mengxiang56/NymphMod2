using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphRevenantRemnant : ModRelicTemplate
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("死魂灵残躯");

    [SavedProperty]
    public bool UsedThisCombat
    {
        get => _usedThisCombat;
        set
        {
            AssertMutable();
            _usedThisCombat = value;
        }
    }

    public override Task BeforeCombatStart()
    {
        UsedThisCombat = false;
        return Task.CompletedTask;
    }

    internal static void TryApplyFirstRecreateReplay(
        Player player,
        IReadOnlyList<RecreateResult> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        NymphRevenantRemnant? relic = player.Relics
            .OfType<NymphRevenantRemnant>()
            .FirstOrDefault();
        if (relic is null || relic.UsedThisCombat)
        {
            return;
        }

        relic.UsedThisCombat = true;
        relic.Flash();
        ApplyReplay(results[0].Replacement);
    }

    private static void ApplyReplay(CardModel card)
    {
        card.BaseReplayCount += 1;
        card.FinalizeUpgradeInternal();
        if (card.DeckVersion is { } deckVersion)
        {
            deckVersion.BaseReplayCount += 1;
            deckVersion.FinalizeUpgradeInternal();
        }
    }
}
