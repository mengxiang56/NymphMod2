using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

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

    [Export]
    public Vector2 DifficultyArrowCenterRatio { get; set; } = new(0.205f, 0.4f);

    [Export]
    public Vector2 DifficultyPreviousArrowOffset { get; set; } = new(-165f, 0f);

    [Export]
    public Vector2 DifficultyNextArrowOffset { get; set; } = new(110f, 0f);

    [Export]
    public Vector2 VoiceArrowCenterRatio { get; set; } = new(0.205f, 0.52f);

    [Export]
    public Vector2 VoicePreviousArrowOffset { get; set; } = new(-165f, 0f);

    [Export]
    public Vector2 VoiceNextArrowOffset { get; set; } = new(110f, 0f);

    private Node _preview = null!;
    private TextureRect _background = null!;
    private Control _skinSelector = null!;
    private Label _title = null!;
    private Label _skinName = null!;
    private Control _difficultySelector = null!;
    private Control _difficultyHoverArea = null!;
    private Label _difficultyTitle = null!;
    private Label _difficultyName = null!;
    private Control _voiceSelector = null!;
    private Control _voiceHoverArea = null!;
    private Label _voiceTitle = null!;
    private Label _voiceName = null!;
    private NCharacterSelectScreen? _characterSelectScreen;
    private NGoldArrowButton? _leftButton;
    private NGoldArrowButton? _rightButton;
    private NGoldArrowButton? _difficultyLeftButton;
    private NGoldArrowButton? _difficultyRightButton;
    private NGoldArrowButton? _voiceLeftButton;
    private NGoldArrowButton? _voiceRightButton;
    private bool _difficultyHoverVisible;
    private bool _voiceHoverVisible;

    public override void _Ready()
    {
        _preview = GetNode("SkinPreview");
        _background = GetNode<TextureRect>("Control/Icon");
        _skinSelector = GetNode<Control>("SkinSelector");
        _title = GetNode<Label>("SkinSelector/Title");
        _skinName = GetNode<Label>("SkinSelector/SkinName");
        _difficultySelector = GetNode<Control>("DifficultySelector");
        _difficultyHoverArea = GetNode<Control>(
            "DifficultySelector/HoverArea");
        _difficultyTitle = GetNode<Label>("DifficultySelector/Title");
        _difficultyName = GetNode<Label>("DifficultySelector/DifficultyName");
        _voiceSelector = GetNode<Control>("VoiceSelector");
        _voiceHoverArea = GetNode<Control>("VoiceSelector/HoverArea");
        _voiceTitle = GetNode<Label>("VoiceSelector/Title");
        _voiceName = GetNode<Label>("VoiceSelector/VoiceName");
        _characterSelectScreen = FindCharacterSelectScreen();

        _difficultyHoverArea.MouseEntered += ShowDifficultyHoverTip;
        _difficultyHoverArea.MouseExited += HideDifficultyHoverTip;
        _voiceHoverArea.MouseEntered += ShowVoiceHoverTip;
        _voiceHoverArea.MouseExited += HideVoiceHoverTip;

        CreateArrowButtons();
        ConnectCharacterSelectButtons();
        Resized += PositionArrowButtons;
        RefreshPreview();
        RefreshDifficulty();
        RefreshVoice();

        if (_characterSelectScreen?.Lobby is not null)
        {
            NymphDifficultyManager.SyncLobby(_characterSelectScreen.Lobby);
        }
    }

    private void ConnectCharacterSelectButtons()
    {
        if (_characterSelectScreen is null)
        {
            return;
        }

        NButton confirmButton =
            _characterSelectScreen.GetNode<NButton>("ConfirmButton");
        confirmButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => SetSelectorVisible(false)));

        NButton? unreadyButton =
            _characterSelectScreen.GetNodeOrNull<NButton>("UnreadyButton");
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
            _difficultyLeftButton = template.GetNode<NGoldArrowButton>("LeftArrow").Duplicate() as NGoldArrowButton;
            _difficultyRightButton = template.GetNode<NGoldArrowButton>("RightArrow").Duplicate() as NGoldArrowButton;
            _voiceLeftButton = template.GetNode<NGoldArrowButton>("LeftArrow").Duplicate() as NGoldArrowButton;
            _voiceRightButton = template.GetNode<NGoldArrowButton>("RightArrow").Duplicate() as NGoldArrowButton;
            if (_leftButton is null
                || _rightButton is null
                || _difficultyLeftButton is null
                || _difficultyRightButton is null
                || _voiceLeftButton is null
                || _voiceRightButton is null)
            {
                Entry.Logger.Warn("Unable to duplicate character skin arrow buttons.");
                return;
            }

            _leftButton.Name = "PreviousSkin";
            _rightButton.Name = "NextSkin";
            _difficultyLeftButton.Name = "PreviousDifficulty";
            _difficultyRightButton.Name = "NextDifficulty";
            _voiceLeftButton.Name = "PreviousVoice";
            _voiceRightButton.Name = "NextVoice";
            MakeArrowMaterialUnique(_leftButton);
            MakeArrowMaterialUnique(_rightButton);
            MakeArrowMaterialUnique(_difficultyLeftButton);
            MakeArrowMaterialUnique(_difficultyRightButton);
            MakeArrowMaterialUnique(_voiceLeftButton);
            MakeArrowMaterialUnique(_voiceRightButton);
            _leftButton.Scale = Vector2.One * ArrowScale;
            _rightButton.Scale = Vector2.One * ArrowScale;
            _difficultyLeftButton.Scale = Vector2.One * ArrowScale;
            _difficultyRightButton.Scale = Vector2.One * ArrowScale;
            _voiceLeftButton.Scale = Vector2.One * ArrowScale;
            _voiceRightButton.Scale = Vector2.One * ArrowScale;
            this.AddChildSafely(_leftButton);
            this.AddChildSafely(_rightButton);
            this.AddChildSafely(_difficultyLeftButton);
            this.AddChildSafely(_difficultyRightButton);
            this.AddChildSafely(_voiceLeftButton);
            this.AddChildSafely(_voiceRightButton);
            _leftButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeSkin(-1)));
            _rightButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeSkin(1)));
            _difficultyLeftButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeDifficulty(-1)));
            _difficultyRightButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeDifficulty(1)));
            _voiceLeftButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeVoice(-1)));
            _voiceRightButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeVoice(1)));
            PositionArrowButtons();
        }
        finally
        {
            template.QueueFreeSafely();
        }
    }

    private static void MakeArrowMaterialUnique(NGoldArrowButton button)
    {
        TextureRect icon = button.GetNode<TextureRect>("TextureRect");
        if (icon.Material is Material material)
        {
            icon.Material = material.Duplicate(true) as Material;
        }
    }

    private void PositionArrowButtons()
    {
        if (_leftButton is null
            || _rightButton is null
            || _difficultyLeftButton is null
            || _difficultyRightButton is null
            || _voiceLeftButton is null
            || _voiceRightButton is null)
        {
            return;
        }

        Vector2 center = Size * ArrowCenterRatio;
        _leftButton.Position = center + PreviousArrowOffset;
        _rightButton.Position = center + NextArrowOffset;
        Vector2 difficultyCenter = Size * DifficultyArrowCenterRatio;
        _difficultyLeftButton.Position =
            difficultyCenter + DifficultyPreviousArrowOffset;
        _difficultyRightButton.Position =
            difficultyCenter + DifficultyNextArrowOffset;
        Vector2 voiceCenter = Size * VoiceArrowCenterRatio;
        _voiceLeftButton.Position = voiceCenter + VoicePreviousArrowOffset;
        _voiceRightButton.Position = voiceCenter + VoiceNextArrowOffset;
        _leftButton.FocusNeighborRight = _rightButton.GetPath();
        _rightButton.FocusNeighborLeft = _leftButton.GetPath();
        _leftButton.FocusNeighborBottom = _difficultyLeftButton.GetPath();
        _rightButton.FocusNeighborBottom = _difficultyRightButton.GetPath();
        _difficultyLeftButton.FocusNeighborTop = _leftButton.GetPath();
        _difficultyRightButton.FocusNeighborTop = _rightButton.GetPath();
        _difficultyLeftButton.FocusNeighborRight =
            _difficultyRightButton.GetPath();
        _difficultyRightButton.FocusNeighborLeft =
            _difficultyLeftButton.GetPath();
        _difficultyLeftButton.FocusNeighborBottom = _voiceLeftButton.GetPath();
        _difficultyRightButton.FocusNeighborBottom = _voiceRightButton.GetPath();
        _voiceLeftButton.FocusNeighborTop = _difficultyLeftButton.GetPath();
        _voiceRightButton.FocusNeighborTop = _difficultyRightButton.GetPath();
        _voiceLeftButton.FocusNeighborRight = _voiceRightButton.GetPath();
        _voiceRightButton.FocusNeighborLeft = _voiceLeftButton.GetPath();
    }

    private void ChangeSkin(int delta)
    {
        NymphSkinManager.Select(NymphSkinManager.SelectedIndex + delta);
        RefreshPreview();
    }

    private void ChangeDifficulty(int delta)
    {
        NymphDifficultyManager.Select(
            NymphDifficultyManager.SelectedIndex + delta,
            _characterSelectScreen?.Lobby);
        RefreshDifficulty();
    }

    private void ChangeVoice(int delta)
    {
        NymphVoiceManager.Select(NymphVoiceManager.SelectedIndex + delta);
        RefreshVoice();
    }

    private void SetSelectorVisible(bool visible)
    {
        _skinSelector.Visible = visible;
        _difficultySelector.Visible = visible;
        _voiceSelector.Visible = visible;
        _preview.Set("visible", visible);
        if (_leftButton is not null)
        {
            _leftButton.Visible = visible;
        }

        if (_rightButton is not null)
        {
            _rightButton.Visible = visible;
        }

        if (_difficultyLeftButton is not null)
        {
            _difficultyLeftButton.Visible = visible;
        }

        if (_difficultyRightButton is not null)
        {
            _difficultyRightButton.Visible = visible;
        }

        if (_voiceLeftButton is not null)
        {
            _voiceLeftButton.Visible = visible;
        }

        if (_voiceRightButton is not null)
        {
            _voiceRightButton.Visible = visible;
        }

        if (!visible)
        {
            HideDifficultyHoverTip();
            HideVoiceHoverTip();
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

    private void RefreshDifficulty()
    {
        _difficultyTitle.Text = new LocString(
            "characters",
            "NYMPH_DIFFICULTY_SELECT.title").GetFormattedText();
        _difficultyName.Text = new LocString(
            "characters",
            NymphDifficultyManager.SelectedNameKey).GetFormattedText();

        if (_difficultyHoverVisible)
        {
            NHoverTipSet.Remove(_difficultyHoverArea);
            ShowDifficultyHoverTip();
        }
    }

    private void RefreshVoice()
    {
        _voiceTitle.Text = new LocString(
            "characters",
            "NYMPH_VOICE_SELECT.title").GetFormattedText();
        _voiceName.Text = new LocString(
            "characters",
            NymphVoiceManager.SelectedNameKey).GetFormattedText();

        if (_voiceHoverVisible)
        {
            NHoverTipSet.Remove(_voiceHoverArea);
            ShowVoiceHoverTip();
        }
    }

    private void ShowDifficultyHoverTip()
    {
        if (!_difficultySelector.Visible)
        {
            return;
        }

        _difficultyHoverVisible = true;
        HoverTip hoverTip = new(
            new LocString(
                "characters",
                NymphDifficultyManager.SelectedNameKey),
            new LocString(
                "characters",
                NymphDifficultyManager.SelectedDescriptionKey));
        NHoverTipSet.CreateAndShow(
            _difficultyHoverArea,
            hoverTip,
            HoverTipAlignment.Right);
    }

    private void HideDifficultyHoverTip()
    {
        _difficultyHoverVisible = false;
        NHoverTipSet.Remove(_difficultyHoverArea);
    }

    private void ShowVoiceHoverTip()
    {
        if (!_voiceSelector.Visible)
        {
            return;
        }

        _voiceHoverVisible = true;
        HoverTip hoverTip = new(
            new LocString("characters", NymphVoiceManager.SelectedNameKey),
            new LocString(
                "characters",
                NymphVoiceManager.SelectedDescriptionKey));
        NHoverTipSet.CreateAndShow(
            _voiceHoverArea,
            hoverTip,
            HoverTipAlignment.Right);
    }

    private void HideVoiceHoverTip()
    {
        _voiceHoverVisible = false;
        NHoverTipSet.Remove(_voiceHoverArea);
    }

    private NCharacterSelectScreen? FindCharacterSelectScreen()
    {
        Node? node = GetParent();
        while (node is not null)
        {
            if (node is NCharacterSelectScreen screen)
            {
                return screen;
            }

            node = node.GetParent();
        }

        return null;
    }
}
