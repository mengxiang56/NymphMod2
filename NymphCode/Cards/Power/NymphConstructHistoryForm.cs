using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphConstructHistoryForm : ModCardTemplate
{
    internal const int PortraitFrameCount = 3;
    internal const float PortraitFrameInterval = 0.075f;

    internal static readonly string[] PortraitFramePaths =
    [
        $"{Entry.ResPath}/images/cards/NymphConstructHistoryForm_1.png",
        $"{Entry.ResPath}/images/cards/NymphConstructHistoryForm_2.png",
        $"{Entry.ResPath}/images/cards/NymphConstructHistoryForm_3.png",
    ];

    private int _portraitFrameIndex;
    private float _portraitFrameTimer;
    private bool _portraitFrameForward = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: PortraitFramePaths[0],
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_power_sts2.png");

    public override IEnumerable<string> AllPortraitPaths => PortraitFramePaths;

    public override string? CustomPortraitPath => PortraitFramePaths[_portraitFrameIndex];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        DiscoveryMechanics.CreateHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    public NymphConstructHistoryForm()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ConstructHistoryFormPower? power =
            await PowerCmd.Apply<ConstructHistoryFormPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Cards.IntValue,
            Owner.Creature,
            this);
        power?.RegisterSource(IsUpgraded);
    }

    protected override void OnUpgrade()
    {
    }

    internal bool AdvancePortraitAnimation(float deltaSeconds)
    {
        _portraitFrameTimer += deltaSeconds;
        if (_portraitFrameTimer < PortraitFrameInterval)
        {
            return false;
        }

        _portraitFrameTimer = 0f;
        int previousFrame = _portraitFrameIndex;
        if (_portraitFrameForward)
        {
            _portraitFrameIndex++;
            if (_portraitFrameIndex >= PortraitFrameCount - 1)
            {
                _portraitFrameForward = false;
            }
        }
        else
        {
            _portraitFrameIndex--;
            if (_portraitFrameIndex <= 0)
            {
                _portraitFrameForward = true;
            }
        }

        return previousFrame != _portraitFrameIndex;
    }
}
