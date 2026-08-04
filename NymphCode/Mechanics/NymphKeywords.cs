using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace Nymph.Mechanics;

[RegisterOwnedCardKeyword(
    "Conceive",
    IncludeInCardHoverTip = true)]
public sealed class ConceiveKeywordRegistration
{
}

[RegisterOwnedCardKeyword(
    "Narrate",
    IncludeInCardHoverTip = true)]
public sealed class NarrateKeywordRegistration
{
}

[RegisterOwnedCardKeyword(
    "Recreate",
    IncludeInCardHoverTip = true)]
public sealed class RecreateKeywordRegistration
{
}

[RegisterOwnedCardKeyword(
    "Inspiration",
    IncludeInCardHoverTip = true)]
public sealed class InspirationKeywordRegistration
{
}

[RegisterOwnedCardKeyword(
    "RecreatePotion",
    IncludeInCardHoverTip = true)]
public sealed class RecreatePotionKeywordRegistration
{
}

[RegisterOwnedCardKeyword(
    "RecreateRelic",
    IncludeInCardHoverTip = true)]
public sealed class RecreateRelicKeywordRegistration
{
}

[RegisterOwnedCardKeyword(
    "Threshold",
    IncludeInCardHoverTip = true)]
public sealed class ThresholdKeywordRegistration
{
}

public static class NymphKeywords
{
    public const string ConceiveId = "NYMPH_KEYWORD_CONCEIVE";
    public const string NarrateId = "NYMPH_KEYWORD_NARRATE";
    public const string RecreateId = "NYMPH_KEYWORD_RECREATE";
    public const string InspirationId = "NYMPH_KEYWORD_INSPIRATION";
    public const string RecreatePotionId = "NYMPH_KEYWORD_RECREATE_POTION";
    public const string RecreateRelicId = "NYMPH_KEYWORD_RECREATE_RELIC";
    public const string ThresholdId = "NYMPH_KEYWORD_THRESHOLD";

    public static CardKeyword Conceive =>
        ModKeywordRegistry.GetCardKeyword(ConceiveId);

    public static CardKeyword Narrate =>
        ModKeywordRegistry.GetCardKeyword(NarrateId);

    public static CardKeyword Recreate =>
        ModKeywordRegistry.GetCardKeyword(RecreateId);

    public static CardKeyword Inspiration =>
        ModKeywordRegistry.GetCardKeyword(InspirationId);

    public static CardKeyword RecreateRelic =>
        ModKeywordRegistry.GetCardKeyword(RecreateRelicId);

    public static CardKeyword Threshold =>
        ModKeywordRegistry.GetCardKeyword(ThresholdId);

    public static IHoverTip CreateThresholdHoverTip() =>
        ModKeywordRegistry.CreateHoverTip(ThresholdId);
}
