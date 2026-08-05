using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NChooseARelicSelection), nameof(NChooseARelicSelection._Ready))]
internal static class ChooseARelicSelectionReadyPatch
{
    private static readonly System.Reflection.FieldInfo RelicsField =
        AccessTools.Field(typeof(NChooseARelicSelection), "_relics")!;

    [HarmonyPrefix]
    private static bool Prefix(NChooseARelicSelection __instance)
    {
        if (RelicsField.GetValue(__instance) is not IReadOnlyList<RelicModel> relics
            || relics.Count <= ChooseARelicSelectionPagination.MaxRelicsPerPage)
        {
            return true;
        }

        ChooseARelicSelectionPagination.SetupReadyWithoutAnimation(__instance, relics);
        return false;
    }
}

[HarmonyPatch(typeof(NChooseARelicSelection), nameof(NChooseARelicSelection.AfterOverlayOpened))]
internal static class ChooseARelicSelectionPaginationPatch
{
    private static void Postfix(NChooseARelicSelection __instance)
    {
        ChooseARelicSelectionPagination.TryInstall(__instance);
    }
}

internal static class ChooseARelicSelectionPagination
{
    internal const int MaxRelicsPerPage = 7;

    private const float RelicXSpacing = 200f;

    private static readonly string InspectRelicScenePath =
        SceneHelper.GetScenePath(
            "screens/inspect_relic_screen/inspect_relic_screen");

    private static readonly System.Reflection.FieldInfo BannerField =
        AccessTools.Field(typeof(NChooseARelicSelection), "_banner")!;

    private static readonly System.Reflection.FieldInfo RelicRowField =
        AccessTools.Field(typeof(NChooseARelicSelection), "_relicRow")!;

    private static readonly System.Reflection.FieldInfo SkipButtonField =
        AccessTools.Field(typeof(NChooseARelicSelection), "_skipButton")!;

    private static readonly System.Reflection.MethodInfo SelectHolderMethod =
        AccessTools.Method(typeof(NChooseARelicSelection), "SelectHolder")!;

    private static readonly System.Reflection.MethodInfo OnSkipButtonReleasedMethod =
        AccessTools.Method(typeof(NChooseARelicSelection), "OnSkipButtonReleased")!;

    internal static void SetupReadyWithoutAnimation(
        NChooseARelicSelection screen,
        IReadOnlyList<RelicModel> relics)
    {
        NCommonBanner banner = screen.GetNode<NCommonBanner>("Banner");
        banner.label.SetTextAutoSize(
            new LocString("gameplay_ui", "CHOOSE_RELIC_HEADER").GetRawText());
        banner.AnimateIn();
        BannerField.SetValue(screen, banner);

        Control relicRow = screen.GetNode<Control>("RelicRow");
        RelicRowField.SetValue(screen, relicRow);

        for (int i = 0; i < relics.Count; i++)
        {
            NRelicBasicHolder holder = NRelicBasicHolder.Create(relics[i]);
            holder.Scale = Vector2.One * 2f;
            holder.Modulate = Colors.White;
            holder.Position = Vector2.Zero;
            relicRow.AddChildSafely(holder);
            holder.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ =>
                    SelectHolderMethod.Invoke(screen, [holder])));
        }

        NChoiceSelectionSkipButton skipButton =
            screen.GetNode<NChoiceSelectionSkipButton>("SkipButton");
        skipButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(button =>
                OnSkipButtonReleasedMethod.Invoke(screen, [button])));
        skipButton.AnimateIn();
        SkipButtonField.SetValue(screen, skipButton);

        List<NRelicBasicHolder> holders = relicRow
            .GetChildren()
            .OfType<NRelicBasicHolder>()
            .ToList();
        if (holders.Count == 0)
        {
            return;
        }

        NRelicBasicHolder focusHolder = holders[holders.Count / 2];
        skipButton.FocusNeighborTop = focusHolder.GetPath();
        skipButton.FocusNeighborBottom = skipButton.GetPath();
        skipButton.FocusNeighborLeft = skipButton.GetPath();
        skipButton.FocusNeighborRight = skipButton.GetPath();

        for (int i = 0; i < relicRow.GetChildCount(); i++)
        {
            Control child = relicRow.GetChild<Control>(i);
            child.FocusNeighborBottom = child.GetPath();
            child.FocusNeighborTop = child.GetPath();
            child.FocusNeighborLeft = i > 0
                ? relicRow.GetChild(i - 1).GetPath()
                : relicRow.GetChild(relicRow.GetChildCount() - 1).GetPath();
            child.FocusNeighborRight = i < relicRow.GetChildCount() - 1
                ? relicRow.GetChild(i + 1).GetPath()
                : relicRow.GetChild(0).GetPath();
        }
    }

    internal static void TryInstall(NChooseARelicSelection screen)
    {
        Control relicRow = screen.GetNode<Control>("RelicRow");
        List<NRelicBasicHolder> holders = relicRow
            .GetChildren()
            .OfType<NRelicBasicHolder>()
            .ToList();
        if (holders.Count <= MaxRelicsPerPage)
        {
            return;
        }

        if (screen.GetNodeOrNull("NymphLeftArrow") is not null)
        {
            return;
        }

        new PaginationController(
            screen,
            relicRow,
            holders,
            MaxRelicsPerPage).Install();
    }

    private sealed class PaginationController
    {
        private readonly NChooseARelicSelection _screen;
        private readonly Control _relicRow;
        private readonly IReadOnlyList<NRelicBasicHolder> _holders;
        private readonly int _maxPerPage;
        private readonly int _pageCount;

        private NGoldArrowButton? _leftButton;
        private NGoldArrowButton? _rightButton;
        private int _currentPage;

        internal PaginationController(
            NChooseARelicSelection screen,
            Control relicRow,
            IReadOnlyList<NRelicBasicHolder> holders,
            int maxPerPage)
        {
            _screen = screen;
            _relicRow = relicRow;
            _holders = holders;
            _maxPerPage = maxPerPage;
            _pageCount = (holders.Count + maxPerPage - 1) / maxPerPage;
        }

        internal void Install()
        {
            CreateArrowButtons();
            ShowPage(0);
        }

        private void CreateArrowButtons()
        {
            Control template = PreloadManager.Cache
                .GetScene(InspectRelicScenePath)
                .Instantiate<Control>(PackedScene.GenEditState.Disabled);
            try
            {
                NGoldArrowButton leftTemplate =
                    template.GetNode<NGoldArrowButton>("LeftArrow");
                NGoldArrowButton rightTemplate =
                    template.GetNode<NGoldArrowButton>("RightArrow");

                _leftButton = leftTemplate.Duplicate() as NGoldArrowButton;
                _rightButton = rightTemplate.Duplicate() as NGoldArrowButton;
                if (_leftButton is null || _rightButton is null)
                {
                    return;
                }

                _leftButton.Name = "NymphLeftArrow";
                _rightButton.Name = "NymphRightArrow";
                _screen.AddChildSafely(_leftButton);
                _screen.AddChildSafely(_rightButton);
                PositionArrows();

                _leftButton.Connect(
                    NClickableControl.SignalName.Released,
                    Callable.From<NButton>(_ => ShowPage(_currentPage - 1)));
                _rightButton.Connect(
                    NClickableControl.SignalName.Released,
                    Callable.From<NButton>(_ => ShowPage(_currentPage + 1)));
            }
            finally
            {
                template.QueueFreeSafely();
            }
        }

        private void PositionArrows()
        {
            if (_leftButton is null || _rightButton is null)
            {
                return;
            }

            float rowCenterY = _relicRow.Position.Y;
            float leftMargin = _screen.Size.X * 0.035f;
            float rightMargin = _screen.Size.X * 0.965f;

            _leftButton.Position = new Vector2(leftMargin, rowCenterY);
            _rightButton.Position = new Vector2(
                rightMargin - _rightButton.Size.X,
                rowCenterY);
        }

        private void ShowPage(int page)
        {
            _currentPage = Math.Clamp(page, 0, _pageCount - 1);
            int start = _currentPage * _maxPerPage;
            int end = Math.Min(start + _maxPerPage, _holders.Count);
            int visibleCount = end - start;
            Vector2 centerOffset =
                Vector2.Left * (visibleCount - 1) * RelicXSpacing * 0.5f;

            for (int i = 0; i < _holders.Count; i++)
            {
                NRelicBasicHolder holder = _holders[i];
                if (i >= start && i < end)
                {
                    int localIndex = i - start;
                    holder.Visible = true;
                    holder.Modulate = Colors.White;
                    holder.Position =
                        centerOffset + Vector2.Right * RelicXSpacing * localIndex;
                }
                else
                {
                    holder.Visible = false;
                }
            }

            UpdateArrowVisibility();
            UpdateFocusNeighbors(start, end);
        }

        private void UpdateArrowVisibility()
        {
            if (_leftButton is null || _rightButton is null)
            {
                return;
            }

            bool hasPrevious = _currentPage > 0;
            bool hasNext = _currentPage < _pageCount - 1;

            _leftButton.Visible = hasPrevious;
            _leftButton.MouseFilter = hasPrevious
                ? Control.MouseFilterEnum.Stop
                : Control.MouseFilterEnum.Ignore;

            _rightButton.Visible = hasNext;
            _rightButton.MouseFilter = hasNext
                ? Control.MouseFilterEnum.Stop
                : Control.MouseFilterEnum.Ignore;
        }

        private void UpdateFocusNeighbors(int start, int end)
        {
            if (_leftButton is null || _rightButton is null)
            {
                return;
            }

            List<NRelicBasicHolder> visible = [];
            for (int i = start; i < end; i++)
            {
                visible.Add(_holders[i]);
            }

            if (visible.Count == 0)
            {
                return;
            }

            for (int i = 0; i < visible.Count; i++)
            {
                NRelicBasicHolder holder = visible[i];
                holder.FocusNeighborTop = holder.GetPath();
                holder.FocusNeighborBottom = holder.GetPath();
                holder.FocusNeighborLeft = i > 0
                    ? visible[i - 1].GetPath()
                    : _leftButton.GetPath();
                holder.FocusNeighborRight = i < visible.Count - 1
                    ? visible[i + 1].GetPath()
                    : _rightButton.GetPath();
            }

            _leftButton.FocusNeighborRight = visible[0].GetPath();
            _rightButton.FocusNeighborLeft = visible[^1].GetPath();
        }
    }
}
