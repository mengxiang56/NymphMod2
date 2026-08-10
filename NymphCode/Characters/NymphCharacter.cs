using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace Nymph.Characters;

[RegisterCharacter]
public sealed class NymphCharacter : ModCharacterTemplate<NymphCardPool, NymphRelicPool, NymphPotionPool>
{
    internal const string CustomAttackSfxToken = "nymph:/sfx/attack";
    internal const string CustomCharacterSelectSfxToken = "nymph:/sfx/character_select";

    public static readonly Color ThemeColor = new(240f / 255f, 50f / 255f, 160f / 255f);

    private const string SceneRoot = $"{Entry.ResPath}/scenes/characters";
    private const string ImageRoot = $"{Entry.ResPath}/images/characters";
    private const string CharacterScenePath = $"{SceneRoot}/Nymph_character.tscn";
    private const string EnergyCounterScenePath = $"{SceneRoot}/Nymph_energy_counter.tscn";
    private const string MerchantScenePath = $"{SceneRoot}/Nymph_merchant.tscn";
    private const string RestSiteScenePath = $"{SceneRoot}/Nymph_rest_site.tscn";
    private const string CharacterSelectBgScenePath = $"{SceneRoot}/Nymph_character_select_bg.tscn";
    private const string CustomTransitionMaterialPath =
        $"{Entry.ResPath}/materials/transitions/Nymph_transition_mat.tres";
    private const string CompactIconScenePath = $"{SceneRoot}/Nymph_icon.tscn";

    // 角色名称颜色。
    public override Color NameColor => ThemeColor;
    // 能量图标轮廓颜色。
    public override Color EnergyLabelOutlineColor => new(0.22f, 0.03f, 0.13f);
    // 地图绘制颜色。
    public override Color MapDrawingColor => ThemeColor;

    // 人物性别（男女中立）。
    public override CharacterGender Gender => CharacterGender.Neutral;

    // 初始血量和金币。
    public override int StartingHp => 70;
    public override int StartingGold => 99;

    // CharacterAssetProfile 按类别拆分。你只写需要替换的部分，其他字段会保留回退。
    // AssetProfile 只指定模板自带的静态占位资源；没有复制的音频、拖尾、转场等资源继续从占位角色回退。
    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            // 人物模型 tscn 路径。
            VisualsPath: CharacterScenePath,
            // 能量表盘 tscn 路径。
            EnergyCounterPath: EnergyCounterScenePath,
            // 商店人物场景。
            MerchantAnimPath: MerchantScenePath,
            // 篝火休息场景。
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            // 人物头像路径。
            IconTexturePath: $"{ImageRoot}/Nymph_character_icon.png",
            // 人物头像轮廓。
            IconOutlineTexturePath: $"{ImageRoot}/Nymph_character_icon_outline.png",
            // 顶栏肖像、地图界面右上角等小图标（CharacterModel.IconPath 场景，不是 IconTexturePath）。
            IconPath: CompactIconScenePath,
            // 人物选择背景。
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            // 开始游戏时使用的角色专属过场遮罩材质。
            CharacterSelectTransitionPath: CustomTransitionMaterialPath,
            // 人物选择图标。
            CharacterSelectIconPath: $"{ImageRoot}/Nymph_character_select.png",
            // 人物选择图标-锁定状态。
            CharacterSelectLockedIconPath: $"{ImageRoot}/Nymph_character_select_locked.png",
            // 地图上的角色标记图标、表情轮盘上的角色头像。
            MapMarkerPath: $"{ImageRoot}/Nymph_map_marker.png"),
        Audio: new CharacterAudioAssetSet(
            CharacterSelectSfx: CustomCharacterSelectSfxToken,
            AttackSfx: CustomAttackSfxToken),
        Multiplayer: new CharacterMultiplayerAssetSet(
            ArmPointingTexturePath: $"{ImageRoot}/Nymph_multiplayer_hand_point.png",
            ArmRockTexturePath: $"{ImageRoot}/Nymph_multiplayer_hand_rock.png",
            ArmPaperTexturePath: $"{ImageRoot}/Nymph_multiplayer_hand_paper.png",
            ArmScissorsTexturePath: $"{ImageRoot}/Nymph_multiplayer_hand_scissors.png"));

    // 某个字段没写时，RitsuLib 会从占位角色配置里补齐。
    public override string? PlaceholderCharacterId => "ironclad";
    // 如果你的人物不需要时间线小故事，加上这句。
    public override bool RequiresEpochAndTimeline => false;
    // 攻击和施法动画延迟，以对齐动画。静态占位资源不需要延迟。
    public override float AttackAnimDelay => 0.35f;
    public override float CastAnimDelay => 0.45f;

    // 将游戏的通用动画触发器映射到此模型自带的 Spine 动画名。
    // 模型没有单独的受伤动画，因此 Hit 使用一次 Idle 后回到循环待机。
    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState attack = new("Attack");
        AnimState cast = new("Skill_2");
        AnimState hit = new("Idle");
        AnimState dead = new("Die");
        AnimState relaxed = new("Idle", isLooping: true);

        attack.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;
        relaxed.AddBranch("Idle", idle);

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }

    // 让 RitsuLib 把普通 Godot 场景转换成游戏需要的 NCreatureVisuals。
    // 自动转换人物场景，让你不需要手动挂脚本。复制即可。
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        NCreatureVisuals? visuals =
            RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
                CharacterScenePath);
        if (visuals is not null)
        {
            NymphSkinManager.ApplyCombatSkin(visuals);
        }

        return visuals;
    }

    // 攻击建筑师的攻击特效列表。
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
