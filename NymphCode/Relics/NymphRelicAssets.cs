using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

internal static class NymphRelicAssets
{
    internal static RelicAssetProfile FromFile(string fileName) => new(
        IconPath: $"{Entry.ResPath}/images/relics/{fileName}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{fileName}_outline.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{fileName}.png");
}
