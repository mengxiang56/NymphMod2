using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;

namespace Nymph.Mechanics;

/// <summary>
/// Mirrors vanilla <see cref="PotionRewardOdds"/> for normal and elite Inspiration drops.
/// Boss combats always drop a Rare Inspiration and do not call <see cref="Roll"/>.
/// </summary>
public sealed class InspirationRewardOdds : AbstractOdds
{
    public const float TargetOdds = 0.5f;

    public const float BaseOdds = 0.4f;

    private const float PityDelta = 0.1f;

    /// <summary>
    /// Run saves only accept integer properties, so the odds are persisted as permille.
    /// </summary>
    private const int PermilleScale = 1000;

    public const int BaseOddsPermille = (int)(BaseOdds * PermilleScale);

    public InspirationRewardOdds(Rng rng)
        : base(BaseOdds, rng)
    {
    }

    public InspirationRewardOdds(float initialValue, Rng rng)
        : base(initialValue, rng)
    {
    }

    public int CurrentPermille => (int)MathF.Round(CurrentValue * PermilleScale);

    public static InspirationRewardOdds FromPermille(int permille, Rng rng)
    {
        return new InspirationRewardOdds(
            permille / (float)PermilleScale,
            rng);
    }

    public bool Roll()
    {
        if (_rng.NextFloat() < CurrentValue)
        {
            CurrentValue -= PityDelta;
            return true;
        }

        CurrentValue += PityDelta;
        return false;
    }
}
