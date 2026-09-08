using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Systems;

namespace IdleTower.Map.Raid
{
    /// <summary>Выдача наград рейд-сайта (захват и т.п.).</summary>
    public static class RaidRewardsHelper
    {
        /// <summary>Награда за полный захват — всегда из текущего Site, не из сейва.</summary>
        public static void GrantCaptureRewards(ResourceSystem resources, RaidSiteConfig site)
        {
            if (resources == null || site?.CaptureRewards == null || site.CaptureRewards.Length == 0)
                return;

            resources.Add(site.CaptureRewards);
        }

        public static void GrantCaptureRewards(GameServices services, RaidSiteConfig site)
            => GrantCaptureRewards(services?.Resources, site);
    }
}
