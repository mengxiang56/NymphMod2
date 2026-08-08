using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphSoulBindingBone : ModRelicTemplate
{
    private const int ThoughtThreshold = 10;
    private const int StrengthGain = 3;

    private int _thoughtSpentThisTurn;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("束灵骨");

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => ThoughtSpentThisTurn;

    [SavedProperty]
    public int ThoughtSpentThisTurn
    {
        get => _thoughtSpentThisTurn;
        set
        {
            AssertMutable();
            _thoughtSpentThisTurn = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner)
        {
            ThoughtSpentThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    internal static async Task OnThoughtSpent(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        NymphSoulBindingBone? relic = player.Relics
            .OfType<NymphSoulBindingBone>()
            .FirstOrDefault();
        if (relic is null)
        {
            return;
        }

        relic.ThoughtSpentThisTurn += amount;
        while (relic.ThoughtSpentThisTurn >= ThoughtThreshold)
        {
            relic.ThoughtSpentThisTurn -= ThoughtThreshold;
            relic.Flash();
            await PowerCmd.Apply<NymphSoulBindingBonePower>(
                choiceContext,
                player.Creature,
                StrengthGain,
                player.Creature,
                null);
        }
    }
}
