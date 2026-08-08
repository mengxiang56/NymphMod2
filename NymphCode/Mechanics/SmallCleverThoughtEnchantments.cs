using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Random;

namespace Nymph.Mechanics;

internal static class SmallCleverThoughtEnchantments
{
    private static readonly HashSet<Type> StackedEnchantmentTypes =
    [
        typeof(Adroit),
        typeof(Momentum),
        typeof(Nimble),
        typeof(Sharp),
        typeof(Swift),
        typeof(Vigorous),
    ];

    private static readonly EnchantmentModel[] UniversalEnchantments =
    [
        ModelDb.Enchantment<Adroit>(),
        ModelDb.Enchantment<Glam>(),
        ModelDb.Enchantment<PerfectFit>(),
        ModelDb.Enchantment<Slither>(),
        ModelDb.Enchantment<SlumberingEssence>(),
        ModelDb.Enchantment<RoyallyApproved>(),
        ModelDb.Enchantment<Sown>(),
        ModelDb.Enchantment<Spiral>(),
        ModelDb.Enchantment<Steady>(),
        ModelDb.Enchantment<Swift>(),
    ];

    private static readonly EnchantmentModel[] AttackOnlyEnchantments =
    [
        ModelDb.Enchantment<Corrupted>(),
        ModelDb.Enchantment<Inky>(),
        ModelDb.Enchantment<Instinct>(),
        ModelDb.Enchantment<Momentum>(),
        ModelDb.Enchantment<Sharp>(),
        ModelDb.Enchantment<TezcatarasEmber>(),
        ModelDb.Enchantment<Vigorous>(),
    ];

    private static readonly EnchantmentModel[] SkillOnlyEnchantments =
    [
        ModelDb.Enchantment<Nimble>(),
    ];

    internal static List<EnchantmentModel> GetEligibleEnchantments(CardModel card)
    {
        List<EnchantmentModel> pool = [];
        foreach (EnchantmentModel enchantment in GetCandidatesFor(card.Type))
        {
            if (enchantment.CanEnchant(card))
            {
                pool.Add(enchantment);
            }
        }

        return pool;
    }

    internal static int RollAmount(EnchantmentModel enchantment, Rng rng)
    {
        if (StackedEnchantmentTypes.Contains(enchantment.GetType()))
        {
            return rng.NextInt(1, 4);
        }

        return 1;
    }

    private static IEnumerable<EnchantmentModel> GetCandidatesFor(CardType cardType)
    {
        foreach (EnchantmentModel enchantment in UniversalEnchantments)
        {
            yield return enchantment;
        }

        if (cardType == CardType.Attack)
        {
            foreach (EnchantmentModel enchantment in AttackOnlyEnchantments)
            {
                yield return enchantment;
            }
        }

        if (cardType == CardType.Skill)
        {
            foreach (EnchantmentModel enchantment in SkillOnlyEnchantments)
            {
                yield return enchantment;
            }
        }
    }
}
