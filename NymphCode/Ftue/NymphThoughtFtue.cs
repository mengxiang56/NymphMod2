using System;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Models;
using Nymph.Mechanics;
using Nymph.Relics;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace Nymph.Ftue;

public sealed partial class NymphThoughtFtue : NFtue
{
    public const string Id = "nymph_thought_ftue";

    private const int TotalPages = 3;
    private const string ThoughtCounterAttachmentId = "ThoughtCounter";

    /// <summary>
    /// 弹窗面板尺寸（只改背景/布局范围，不会放大文字和按钮）。
    /// 增大 PopupWidth / PopupHeight 即可放大显示区域；不要用 Control.Scale。
    /// </summary>
    private const float PopupWidth = 750f;
    private const float PopupHeight = 750f;
    /// <summary>弹窗底边距屏幕底部的距离（越大越靠上）。</summary>
    private const float PopupBottomMargin = 200f;
    /// <summary>水平偏移（正值向右，负值向左；0 为屏幕居中）。</summary>
    private const float PopupHorizontalOffset = -300f;

    private static readonly string[] BodyKeys =
    [
        "NYMPH_THOUGHT_FTUE_BODY_1",
        "NYMPH_THOUGHT_FTUE_BODY_2",
        "NYMPH_THOUGHT_FTUE_BODY_3",
    ];

    private static readonly string ScenePath =
        $"{Entry.ResPath}/scenes/ftue/nymph_thought_ftue.tscn";

    private static readonly string VerticalPopupScenePath =
        SceneHelper.GetScenePath("ui/vertical_popup");

    private int _currentPage = 1;
    private NVerticalPopup? _popup;
    private NPopupYesNoButton? _confirmButton;
    private Player? _player;
    private NThoughtCounter? _thoughtCounter;
    private int _thoughtCounterDefaultZIndex;

    public static NymphThoughtFtue? Create(Player player)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NymphThoughtFtue ftue = PreloadManager.Cache
            .GetScene(ScenePath)
            .Instantiate<NymphThoughtFtue>(PackedScene.GenEditState.Disabled);
        ftue._player = player;
        return ftue;
    }

    public override void _Ready()
    {
        _popup = PreloadManager.Cache
            .GetScene(VerticalPopupScenePath)
            .Instantiate<NVerticalPopup>(PackedScene.GenEditState.Disabled);
        ConfigurePopupPosition(_popup);
        AddChild(_popup);

        _popup.HideNoButton();

        _confirmButton = _popup.YesButton;
        _confirmButton.Visible = true;
        _confirmButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From((Action<NButton>)OnConfirmPressed));

        CallDeferred(MethodName.HighlightThoughtCounter);
        ShowPage(1);
    }

    public override void _ExitTree()
    {
        RestoreThoughtCounterHighlight();
        base._ExitTree();
    }

    private static void ConfigurePopupPosition(NVerticalPopup popup)
    {
        // 底部居中，避免挡住左下角思绪值 UI。
        popup.SetAnchorsPreset(LayoutPreset.CenterBottom);
        popup.AnchorLeft = 0.5f;
        popup.AnchorTop = 1f;
        popup.AnchorRight = 0.5f;
        popup.AnchorBottom = 1f;
        popup.OffsetLeft = -PopupWidth * 0.5f + PopupHorizontalOffset;
        popup.OffsetRight = PopupWidth * 0.5f + PopupHorizontalOffset;
        popup.OffsetBottom = -PopupBottomMargin;
        popup.OffsetTop = popup.OffsetBottom - PopupHeight;
        popup.GrowHorizontal = GrowDirection.Begin;
        popup.GrowVertical = GrowDirection.Begin;
    }

    private void HighlightThoughtCounter()
    {
        if (_thoughtCounter is not null)
        {
            return;
        }

        NCombatUi? ui = NCombatRoom.Instance?.Ui;
        if (ui is null)
        {
            return;
        }

        ModNodeAttachmentRegistry registry =
            ModNodeAttachmentRegistry.For(Entry.ModId);
        NThoughtCounter? counter = null;
        if (registry.TryGetAttached(
                ui,
                ThoughtCounterAttachmentId,
                out NThoughtCounter attached))
        {
            counter = attached;
        }
        else
        {
            counter = ui.FindChild(ThoughtCounterAttachmentId, recursive: true, owned: false)
                as NThoughtCounter;
        }

        if (counter is null)
        {
            return;
        }

        _thoughtCounter = counter;
        _thoughtCounterDefaultZIndex = counter.ZIndex;
        counter.ZIndex = _thoughtCounterDefaultZIndex + 1;
    }

    private void RestoreThoughtCounterHighlight()
    {
        if (_thoughtCounter is null)
        {
            return;
        }

        _thoughtCounter.ZIndex = _thoughtCounterDefaultZIndex;
        _thoughtCounter = null;
    }

    private void ShowPage(int page)
    {
        _currentPage = page;

        LocString title = new("ftues", "NYMPH_THOUGHT_FTUE_HEADER");
        LocString body = new("ftues", BodyKeys[page - 1]);
        _popup!.SetText(title, body);

        string confirmKey = page >= TotalPages
            ? "NYMPH_THOUGHT_FTUE_DONE"
            : "NYMPH_THOUGHT_FTUE_CONFIRM";
        _confirmButton!.SetText(
            new LocString("ftues", confirmKey).GetFormattedText());
    }

    private void OnConfirmPressed(NButton _)
    {
        if (_currentPage >= TotalPages)
        {
            Complete();
            return;
        }

        ShowPage(_currentPage + 1);
    }

    private void Complete()
    {
        RestoreThoughtCounterHighlight();
        SaveManager.Instance.MarkFtueAsComplete(Id);
        CloseFtue();

        Player? player = _player;
        if (player is null)
        {
            return;
        }

        TaskHelper.RunSafely(OfferDeferredInspiration(player));
    }

    private static async Task OfferDeferredInspiration(Player player)
    {
        if (!RunManager.Instance.IsSingleplayerOrFakeMultiplayer)
        {
            return;
        }

        if (player.PlayerCombatState?.TurnNumber != 1
            || player.Creature.CombatState is not ICombatState combatState)
        {
            return;
        }

        await Cmd.CustomScaledWait(0.15f, 1f);

        foreach (RelicModel relic in player.Relics)
        {
            if (relic is NymphRelic nymphRelic)
            {
                nymphRelic.Flash();
                break;
            }
        }

        await InspirationMechanics.OfferAtCombatStart(
            player,
            new BlockingPlayerChoiceContext(),
            combatState);
    }
}
