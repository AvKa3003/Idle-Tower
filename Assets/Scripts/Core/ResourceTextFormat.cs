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
            var lines = CollectCostLines(costs);
            return string.Join(", ", lines);
        }

        /// <summary>TMP rich text: «Дерево 2/5» зелёным или красным.</summary>
        public static string FormatCostsWithBalance(
            ResourceCost[] costs,
            Func<ResourceDefinition, int> getAmount)
        {
            var lines = CollectCostLinesWithBalance(costs, getAmount);
            return string.Join("\n", lines);
        }

        public static string FormatCycle(ResourceCost[] input, ResourceCost[] output)
        {
            var inputLines = CollectCostLines(input);
            var outputLines = CollectCostLines(output);
            return JoinCycleLines(inputLines, outputLines);
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

            return $"{cycle}\n{time}";
        }

        public static string FormatModeDetailWithBalance(
            ResourceCost[] input,
            ResourceCost[] output,
            GameDuration duration,
            Func<ResourceDefinition, int> getAmount)
        {
            var cycle = JoinCycleLines(
                CollectCostLinesWithBalance(input, getAmount),
                CollectCostLines(output));
            var time = FormatDuration(duration);

            if (string.IsNullOrEmpty(cycle))
                return time;

            if (string.IsNullOrEmpty(time))
                return cycle;

            return $"{cycle}\n{time}";
        }

        private static string JoinCycleLines(List<string> inputLines, List<string> outputLines)
        {
            if (inputLines == null)
                inputLines = new List<string>();
            if (outputLines == null)
                outputLines = new List<string>();

            var lines = new List<string>(inputLines.Count + outputLines.Count);

            if (inputLines.Count > 0)
            {
                for (var i = 0; i < inputLines.Count; i++)
                {
                    var isLastInput = i == inputLines.Count - 1;
                    if (isLastInput && outputLines.Count > 0)
                        lines.Add($"{inputLines[i]} →");
                    else
                        lines.Add(inputLines[i]);
                }

                lines.AddRange(outputLines);
            }
            else
            {
                lines.AddRange(outputLines);
            }

            return string.Join("\n", lines);
        }

        private static List<string> CollectCostLines(ResourceCost[] costs)
        {
            var lines = new List<string>();
            if (costs == null)
                return lines;

            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                lines.Add($"{GetResourceName(cost.Resource)} x{cost.Amount}");
            }

            return lines;
        }

        private static List<string> CollectCostLinesWithBalance(
            ResourceCost[] costs,
            Func<ResourceDefinition, int> getAmount)
        {
            var lines = new List<string>();
            if (costs == null || getAmount == null)
                return lines;

            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                var have = getAmount(cost.Resource);
                var need = cost.Amount;
                var color = have >= need ? AffordableColor : MissingColor;
                var name = GetResourceName(cost.Resource);
                lines.Add($"<color={color}>{name} {have}/{need}</color>");
            }

            return lines;
        }

        private static string GetResourceName(ResourceDefinition resource)
        {
            return string.IsNullOrEmpty(resource.DisplayName)
                ? resource.name
                : resource.DisplayName;
        }
    }
}
