using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace Nymph.Characters;

public sealed partial class NymphSkinSelectBackground : Control
{
    private static readonly string InspectRelicScenePath =
        SceneHelper.GetScenePath("screens/inspect_relic_screen/inspect_relic_screen");

    [Export(PropertyHint.Range, "0.2,1.5,0.05")]
    public float ArrowScale { get; set; } = 0.55f;

    [Export]
    public Vector2 ArrowCenterRatio { get; set; } = new(0.205f, 0.84f);

    [Export]
    public Vector2 PreviousArrowOffset { get; set; } = new(-165f, 0f);

    [Export]
    public Vector2 NextArrowOffset { get; set; } = new(110f, 0f);

    private Node _preview = null!;
    private TextureRect _background = null!;
    private Control _skinSelector = null!;
    private Label _title = null!;
    private Label _skinName = null!;
    private NGoldArrowButton? _leftButton;
    private NGoldArrowButton? _rightButton;

    public override void _Ready()
    {
        _preview = GetNode("SkinPreview");
        _background = GetNode<TextureRect>("Control/Icon");
        _skinSelector = GetNode<Control>("SkinSelector");
        _title = GetNode<Label>("SkinSelector/Title");
        _skinName = GetNode<Label>("SkinSelector/SkinName");

        CreateArrowButtons();
        ConnectCharacterSelectButtons();
        Resized += PositionArrowButtons;
        RefreshPreview();
    }

    private void ConnectCharacterSelectButtons()
    {
        Node? screen = GetParent();
        while (screen is not null && screen.GetNodeOrNull<NButton>("ConfirmButton") is null)
        {
            screen = screen.GetParent();
        }

        if (screen is null)
        {
            return;
        }

        NButton confirmButton = screen.GetNode<NButton>("ConfirmButton");
        confirmButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => SetSelectorVisible(false)));

        NButton? unreadyButton = screen.GetNodeOrNull<NButton>("UnreadyButton");
        unreadyButton?.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => SetSelectorVisible(true)));
    }

    private void CreateArrowButtons()
    {
        PackedScene? scene = ResourceLoader.Load<PackedScene>(
            InspectRelicScenePath,
            null,
            ResourceLoader.CacheMode.Reuse);
        Control? template = scene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        if (template is null)
        {
            Entry.Logger.Warn("Unable to load character skin arrow buttons.");
            return;
        }

        try
        {
            _leftButton = template.GetNode<NGoldArrowButton>("LeftArrow").Duplicate() as NGoldArrowButton;
            _rightButton = template.GetNode<NGoldArrowButton>("RightArrow").Duplicate() as NGoldArrowButton;
            if (_leftButton is null || _rightButton is null)
            {
                Entry.Logger.Warn("Unable to duplicate character skin arrow buttons.");
                return;
            }

            _leftButton.Name = "PreviousSkin";
            _rightButton.Name = "NextSkin";
            _leftButton.Scale = Vector2.One * ArrowScale;
            _rightButton.Scale = Vector2.One * ArrowScale;
            this.AddChildSafely(_leftButton);
            this.AddChildSafely(_rightButton);
            _leftButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeSkin(-1)));
            _rightButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeSkin(1)));
            PositionArrowButtons();
        }
        finally
        {
            template.QueueFreeSafely();
        }
    }

    private void PositionArrowButtons()
    {
        if (_leftButton is null || _rightButton is null)
        {
            return;
        }

        Vector2 center = Size * ArrowCenterRatio;
        _leftButton.Position = center + PreviousArrowOffset;
        _rightButton.Position = center + NextArrowOffset;
        _leftButton.FocusNeighborRight = _rightButton.GetPath();
        _rightButton.FocusNeighborLeft = _leftButton.GetPath();
    }

    private void ChangeSkin(int delta)
    {
        NymphSkinManager.Select(NymphSkinManager.SelectedIndex + delta);
        RefreshPreview();
    }

    private void SetSelectorVisible(bool visible)
    {
        _skinSelector.Visible = visible;
        _preview.Set("visible", visible);
        if (_leftButton is not null)
        {
            _leftButton.Visible = visible;
        }

        if (_rightButton is not null)
        {
            _rightButton.Visible = visible;
        }
    }

    private void RefreshPreview()
    {
        _title.Text = new LocString("characters", "NYMPH_SKIN_SELECT.title")
            .GetFormattedText();
        _skinName.Text = new LocString("characters", NymphSkinManager.SelectedNameKey)
            .GetFormattedText();

        Texture2D? background = ResourceLoader.Load<Texture2D>(
            NymphSkinManager.SelectedBackgroundPath,
            null,
            ResourceLoader.CacheMode.Reuse);
        if (background is not null)
        {
            _background.Texture = background;
        }

        if (!NymphSkinManager.ApplyCombatSkinToSprite(_preview))
        {
            return;
        }

        MegaSprite sprite = new(_preview);
        this.RunWhenSpineReady(sprite, animationState =>
            animationState.SetAnimation("Idle", true));
    }
}
