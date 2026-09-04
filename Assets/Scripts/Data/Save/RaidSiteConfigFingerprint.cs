using System.Text;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Map;

namespace IdleTower.Data.Save
{
    /// <summary>Отпечаток релевантного RaidSiteConfig для детекта изменений баланса.</summary>
    public static class RaidSiteConfigFingerprint
    {
        public static string Compute(RaidSiteConfig site, RaidMapCellBehaviorState state)
        {
            if (site == null)
                return string.Empty;

            var sb = new StringBuilder(256);
            sb.Append((int)site.PostCaptureMode).Append('|');
            sb.Append(site.MaxCompletedRaids).Append('|');

            if (state != null && state.IsCaptured && site.PostCaptureMode == PostCaptureMode.RaidFarm)
                AppendRaidConfig(sb, site.FarmConfig);
            else
                AppendRaidConfig(sb, site.PreCapture);

            return sb.ToString();
        }

        private static void AppendRaidConfig(StringBuilder sb, RaidConfig config)
        {
            if (config == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append(config.RequiredStrength).Append('|');
            sb.Append(config.Duration.Minutes).Append(':').Append(config.Duration.Seconds).Append('|');
            AppendCosts(sb, config.RequiredUnits);
            sb.Append('|');
            AppendCosts(sb, config.Rewards);
        }

        private static void AppendCosts(StringBuilder sb, ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return;

            for (var i = 0; i < costs.Length; i++)
            {
                if (i > 0)
                    sb.Append(';');

                var cost = costs[i];
                var id = cost.Resource != null && !cost.Resource.Id.IsEmpty
                    ? cost.Resource.Id.Value
                    : "?";
                sb.Append(id).Append(':').Append(cost.Amount);
            }
        }
    }
}
