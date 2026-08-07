using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Saves;
using Nymph;
using Nymph.Characters;

namespace Nymph.Ftue;

internal static class NymphThoughtFtueTrigger
{
    private static bool _showScheduled;

    public static void ScheduleShow(Player player)
    {
        if (SaveManager.Instance.SeenFtue(NymphThoughtFtue.Id)
            || player.Character is not NymphCharacter
            || _showScheduled)
        {
            return;
        }

        _showScheduled = true;
        TaskHelper.RunSafely(ShowWhenReady(player));
    }

    private static async Task ShowWhenReady(Player player)
    {
        try
        {
            if (SaveManager.Instance.SeenFtue(NymphThoughtFtue.Id))
            {
                return;
            }

            await Cmd.CustomScaledWait(0.5f, 1f);

            bool sawCombatRulesFtue = false;
            for (int i = 0; i < 600; i++)
            {
                if (IsCombatRulesFtueVisible())
                {
                    sawCombatRulesFtue = true;
                }
                else if (sawCombatRulesFtue || i > 5)
                {
                    break;
                }

                await Cmd.Wait(0.1f);
            }

            if (!sawCombatRulesFtue
                && SaveManager.Instance.SeenFtue(NCombatRulesFtue.id))
            {
                await Cmd.CustomScaledWait(3.0f, 1f);
            }

            if (SaveManager.Instance.SeenFtue(NymphThoughtFtue.Id)
                || NModalContainer.Instance is null
                || IsNymphThoughtFtueVisible()
                || NModalContainer.Instance.OpenModal is not null)
            {
                return;
            }

            NymphThoughtFtue? ftue = NymphThoughtFtue.Create(player);
            if (ftue is null)
            {
                return;
            }

            NModalContainer.Instance.Add(ftue, showBackstop: true);
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"Failed to show Nymph thought FTUE: {ex}");
        }
        finally
        {
            _showScheduled = false;
        }
    }

    private static bool IsCombatRulesFtueVisible()
    {
        NModalContainer? modal = NModalContainer.Instance;
        if (modal is null)
        {
            return false;
        }

        foreach (Node child in modal.GetChildren())
        {
            if (child is NCombatRulesFtue)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNymphThoughtFtueVisible()
    {
        NModalContainer? modal = NModalContainer.Instance;
        if (modal is null)
        {
            return false;
        }

        foreach (Node child in modal.GetChildren())
        {
            if (child is NymphThoughtFtue)
            {
                return true;
            }
        }

        return false;
    }
}
