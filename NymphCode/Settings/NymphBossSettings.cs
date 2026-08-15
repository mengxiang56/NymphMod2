using STS2RitsuLib.Settings;

namespace Nymph.Settings;

[ModSettingsPage(Entry.ModId, "nymph_gameplay", Title = "妮芙", Description = "妮芙的游戏内容设置。", ModDisplayName = "妮芙")]
[ModSettingsSection("boss", Title = "额外首领", Description = "控制额外章节与首领战。")]
public static class NymphBossSettings
{
    [ModSettingsToggle("enable_quilon_boss", "boss", Label = "开启奎隆BOSS战", Description = "单人标准模式中，在第三层后追加休息处、商店和奎隆首领战。多人模式不会启用。")]
    [ModSettingsBinding(Source = ModSettingsReflectionBindingSource.Profile, DataKey = "enable_quilon_boss")]
    public static bool EnableQuilonBoss { get; set; }
}
