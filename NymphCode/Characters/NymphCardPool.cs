using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace Nymph.Characters;

public sealed class NymphCardPool : TypeListCardPoolModel
{
    // RitsuLib 0.4.x 已弃用旧的运行时 RGB 材质辅助函数。
    // 纵向原型先使用默认卡框材质，后续再通过 Godot 资源提供正式卡框。
    private static readonly Material? PoolFrameTintMaterial =
        MaterialUtils.CreateUnmodulatedHsvShaderMaterial();

    // 这两个值是卡池的稳定标识，不是玩家看到的角色名称。
    public override string Title => "Nymph";
    public override string EnergyColorName => "Nymph";

    // 卡牌大图和文本中的能量图标都从 Mod 的 PCK 读取。
    public override string? BigEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_big.png";
    public override string? TextEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_text.png";

    public override Color DeckEntryCardColor => NymphCharacter.ThemeColor;
    public override Color EnergyOutlineColor => new(0.22f, 0.03f, 0.13f);
    public override Material? PoolFrameMaterial => PoolFrameTintMaterial;

    // 这是角色专属卡池，不是无色卡池。
    public override bool IsColorless => false;
}
