using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Nymph.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Nymph.Powers;

[RegisterPower]
public sealed class KeyToHeartStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<NymphKeyToHeart>();

    protected override bool IsPositive => false;
}
