using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using Nymph.Characters;
using Nymph.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Hive))]
public sealed class NymphEncounter : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath:
            $"{Entry.ResPath}/images/events/NymphEncounter.png");

    public override bool IsAllowed(IRunState runState) =>
        runState.Players.Count == 1
        && runState.Players[0].Character is NymphCharacter;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        CreateRelicPreviewOption<NymphHopeEraGraffiti>(ChooseHope),
        CreateRelicPreviewOption<NymphNemesisEraHatred>(ChooseNemesis),
        CreateRelicPreviewOption<NymphBeautifulWishEraRemembrance>(ChooseWish)
    ];

    private EventOption CreateRelicPreviewOption<T>(Func<Task> onChosen)
        where T : RelicModel
    {
        RelicModel relic = ModelDb.Relic<T>().ToMutable();
        relic.Owner = Owner!;
        string optionKey = InitialOptionKey(relic.Id.Entry);
        return new EventOption(this, onChosen, optionKey, relic.HoverTips)
            .WithRelic(relic);
    }

    private async Task ChooseHope()
    {
        await RelicCmd.Obtain<NymphHopeEraGraffiti>(Owner!);
        SetEventFinished(PageDescription("HOPE"));
    }

    private async Task ChooseNemesis()
    {
        await RelicCmd.Obtain<NymphNemesisEraHatred>(Owner!);
        SetEventFinished(PageDescription("NEMESIS"));
    }

    private async Task ChooseWish()
    {
        RelicModel? mostRecentRelic = Owner!.Relics.LastOrDefault();
        if (mostRecentRelic is not null)
        {
            await RelicCmd.Remove(mostRecentRelic);
        }

        NymphBeautifulWishEraRemembrance relic =
            await RelicCmd.Obtain<NymphBeautifulWishEraRemembrance>(Owner);
        relic.Activate();
        SetEventFinished(PageDescription("WISH"));
    }
}
