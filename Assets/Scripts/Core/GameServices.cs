using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Systems;

namespace IdleTower.Core
{
    public class GameServices
    {
        public GameBalanceConfig Balance { get; }
        public BuildingTreeConfig BuildingTree { get; }
        public MapConfig MapConfig { get; }
        public ResourceWallet Wallet { get; }
        public TowerState Tower { get; }
        public MapState MapState => Map.State;
        public GameTickSystem TickSystem { get; }

        public ResourceSystem Resources { get; }
        public UnlockTreeSystem UnlockTree { get; }
        public BuildingSystem Building { get; }
        public RoomBehaviorSystem RoomBehaviors { get; }
        public ProductionSystem Production { get; }
        public MapSystem Map { get; }
        public SaveSystem Save { get; }
        public OfflineSimulationSystem Offline { get; }

        public GameServices(
            GameBalanceConfig balance,
            BuildingTreeConfig buildingTree,
            MapConfig mapConfig)
        {
            Balance = balance;
            BuildingTree = buildingTree;
            MapConfig = mapConfig;
            Wallet = new ResourceWallet();

            Tower = new TowerState();
            TickSystem = new GameTickSystem(this);

            Resources = new ResourceSystem(this);
            UnlockTree = new UnlockTreeSystem(this);
            RoomBehaviors = new RoomBehaviorSystem(this);
            Production = new ProductionSystem(this);
            Building = new BuildingSystem(this);
            Map = new MapSystem(this);
            Save = new SaveSystem(this);
            Offline = new OfflineSimulationSystem(this);

            TickSystem.RegisterTickable(RoomBehaviors);
            TickSystem.RegisterTickable(Map);
        }

        public void InitializeNewGame()
        {
            Wallet.Clear();
            Tower.ResetWithEmptyRoom();
            Map.ReloadFromConfig();
            TickSystem.RestoreFromSave(0, 0f);
            Resources.ApplyStartingResources();
        }
    }
}
