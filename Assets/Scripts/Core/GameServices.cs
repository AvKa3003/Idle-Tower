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
            Building = new BuildingSystem(this);

            TickSystem.RegisterTickable(RoomBehaviors);
        }

        public void InitializeNewGame()
        {
            Wallet.Clear();
            Tower.ResetWithEmptyBuildSlot();
            Resources.ApplyStartingResources();
        }
    }
}
