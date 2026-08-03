using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;

namespace IdleTower.Core
{
    public static class ResourceTextFormat
    {
        private const string AffordableColor = "#1B7A3D";
        private const string MissingColor = "#E74C3C";

        public static string FormatCosts(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return string.Empty;

            var parts = new List<string>(costs.Length);
            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                parts.Add($"{GetResourceName(cost.Resource)} x{cost.Amount}");
            }

            return string.Join(", ", parts);
        }

        /// <summary>TMP rich text: «Дерево 2/5» зелёным или красным.</summary>
        public static string FormatCostsWithBalance(
            ResourceCost[] costs,
            Func<ResourceDefinition, int> getAmount)
        {
            if (costs == null || costs.Length == 0 || getAmount == null)
                return string.Empty;

            var parts = new List<string>(costs.Length);
            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                var have = getAmount(cost.Resource);
                var need = cost.Amount;
                var color = have >= need ? AffordableColor : MissingColor;
                var name = GetResourceName(cost.Resource);
                parts.Add($"<color={color}>{name} {have}/{need}</color>");
            }

            return string.Join(", ", parts);
        }

        public static string FormatCycle(ResourceCost[] input, ResourceCost[] output)
        {
            var inputLabel = FormatCosts(input);
            var outputLabel = FormatCosts(output);

            if (!string.IsNullOrEmpty(inputLabel) && !string.IsNullOrEmpty(outputLabel))
                return $"{inputLabel} → {outputLabel}";

            return outputLabel;
        }

        public static string FormatDuration(GameDuration duration)
        {
            if (duration.TotalSeconds <= 0f)
                return string.Empty;

            if (duration.Minutes > 0 && duration.Seconds > 0)
                return $"{duration.Minutes} мин {duration.Seconds} с";

            if (duration.Minutes > 0)
                return $"{duration.Minutes} мин";

            return $"{duration.Seconds} с";
        }

        public static string FormatElapsedSeconds(float totalSeconds)
        {
            if (totalSeconds < 0f)
                totalSeconds = 0f;

            var total = (long)totalSeconds;
            return FormatElapsedSeconds(total);
        }

        public static string FormatElapsedSeconds(long totalSeconds)
        {
            if (totalSeconds < 0)
                totalSeconds = 0;

            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            if (hours > 0 && minutes > 0)
                return $"{hours} ч {minutes} мин";

            if (hours > 0)
                return $"{hours} ч";

            if (minutes > 0 && seconds > 0)
                return $"{minutes} мин {seconds} с";

            if (minutes > 0)
                return $"{minutes} мин";

            return $"{seconds} с";
        }

        public static string FormatModeDetail(ResourceCost[] input, ResourceCost[] output, GameDuration duration)
        {
            var cycle = FormatCycle(input, output);
            var time = FormatDuration(duration);

            if (string.IsNullOrEmpty(cycle))
                return time;

            if (string.IsNullOrEmpty(time))
                return cycle;

            return $"{cycle} · {time}";
        }

        public static string FormatModeDetailWithBalance(
            ResourceCost[] input,
            ResourceCost[] output,
            GameDuration duration,
            Func<ResourceDefinition, int> getAmount)
        {
            var inputLabel = FormatCostsWithBalance(input, getAmount);
            var outputLabel = FormatCosts(output);
            string cycle;

            if (!string.IsNullOrEmpty(inputLabel) && !string.IsNullOrEmpty(outputLabel))
                cycle = $"{inputLabel} → {outputLabel}";
            else if (!string.IsNullOrEmpty(inputLabel))
                cycle = inputLabel;
            else
                cycle = outputLabel;

            var time = FormatDuration(duration);

            if (string.IsNullOrEmpty(cycle))
                return time;

            if (string.IsNullOrEmpty(time))
                return cycle;

            return $"{cycle} · {time}";
        }

        private static string GetResourceName(ResourceDefinition resource)
        {
            return string.IsNullOrEmpty(resource.DisplayName)
                ? resource.name
                : resource.DisplayName;
        }
    }
}
