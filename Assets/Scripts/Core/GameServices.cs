using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Systems;

namespace IdleTower.Core
{
    public class GameServices
    {
        public GameBalanceConfig Balance { get; }
        public BuildingTreeConfig BuildingTree { get; }
        public ResourceWallet Wallet { get; }
        public TowerState Tower { get; }
        public GameTickSystem TickSystem { get; }

        public ResourceSystem Resources { get; }
        public UnlockTreeSystem UnlockTree { get; }
        public BuildingSystem Building { get; }
        public RoomBehaviorSystem RoomBehaviors { get; }
        public ProductionSystem Production { get; }
        public SaveSystem Save { get; }

        public GameServices(GameBalanceConfig balance, BuildingTreeConfig buildingTree)
        {
            Balance = balance;
            BuildingTree = buildingTree;
            Wallet = new ResourceWallet();

            Tower = new TowerState();
            TickSystem = new GameTickSystem(this);

            Resources = new ResourceSystem(this);
            UnlockTree = new UnlockTreeSystem(this);
            RoomBehaviors = new RoomBehaviorSystem(this);
            Production = new ProductionSystem(this);
            Building = new BuildingSystem(this);
            Save = new SaveSystem(this);

            TickSystem.RegisterTickable(RoomBehaviors);
        }

        public void InitializeNewGame()
        {
            Wallet.Clear();
            Tower.ResetWithEmptyRoom();
            TickSystem.RestoreFromSave(0, 0f);
            Resources.ApplyStartingResources();
        }
    }
}
