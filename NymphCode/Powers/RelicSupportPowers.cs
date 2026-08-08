using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Nymph.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class DisasterOriginUsedThisTurnPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            RemoveInternal();
        }

        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class BridgeOfKnowledgeUsedThisTurnPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            RemoveInternal();
        }

        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class NymphSoulBindingBonePower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<NymphSoulBindingBone>();
}
