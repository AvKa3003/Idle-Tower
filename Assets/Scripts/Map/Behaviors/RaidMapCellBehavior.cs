using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using UnityEngine;

namespace IdleTower.Map.Behaviors
{
    [CreateAssetMenu(fileName = "RaidMapCell", menuName = "IdleTower/Map Cell Behavior/Raid")]
    public class RaidMapCellBehavior : MapCellBehaviorBase
    {
        [SerializeField] private RaidConfig preCapture = new();
        [SerializeField] [Min(1)] private int maxCompletedRaids = 1;
        [SerializeField] private PostCaptureMode postCaptureMode = PostCaptureMode.Dead;

        [Header("PostCapture — RaidFarm (этап E)")]
        [SerializeField] private RaidConfig farmConfig = new();

        [Header("PostCapture — Passive (этап F)")]
        [SerializeField] private GameDuration passiveInterval;
        [SerializeField] private ResourceCost[] passiveRewards = System.Array.Empty<ResourceCost>();

        [Header("Визуал")]
        [SerializeField] private Sprite capturedSprite;

        public RaidConfig PreCapture => preCapture;
        public int MaxCompletedRaids => maxCompletedRaids;
        public PostCaptureMode PostCaptureMode => postCaptureMode;
        public RaidConfig FarmConfig => farmConfig;
        public GameDuration PassiveInterval => passiveInterval;
        public ResourceCost[] PassiveRewards => passiveRewards;
        public Sprite CapturedSprite => capturedSprite;

        /// <summary>До захвата не expander; после — через ShouldRevealNeighbors(runtime).</summary>
        public override bool RevealsNeighborsWhenInteractive => false;

        public override bool ShouldRevealNeighbors(MapCellRuntime runtime)
            => runtime?.BehaviorState is RaidMapCellBehaviorState state && state.IsCaptured;

        public override MapCellClickResult OnClicked(MapCellBehaviorContext context)
            => new(MapCellClickAction.OpenRaid);

        public override MapCellRuntimeState CreateDefaultState()
            => new RaidMapCellBehaviorState();

        public override Sprite GetDisplaySprite(MapCellRuntime runtime)
        {
            if (runtime?.BehaviorState is RaidMapCellBehaviorState state
                && state.IsCaptured
                && capturedSprite != null)
            {
                return capturedSprite;
            }

            return null;
        }

        public RaidConfig GetActiveRaidConfig(RaidMapCellBehaviorState state)
        {
            if (state == null || !state.IsCaptured)
                return preCapture;

            // RaidFarm / Passive — этапы E/F.
            return null;
        }
    }
}
