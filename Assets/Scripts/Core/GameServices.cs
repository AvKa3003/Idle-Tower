using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;

namespace IdleTower.Core
{
    public class GameServices
    {
        public GameBalanceConfig Balance { get; }
        public BuildingTreeConfig BuildingTree { get; }
        public ResourceWallet Wallet { get; }
        public TowerState Tower { get; }
        public GameTickSystem TickSystem { get; }

        public GameServices(GameBalanceConfig balance, BuildingTreeConfig buildingTree)
        {
            Balance = balance;
            BuildingTree = buildingTree;
            Wallet = new ResourceWallet();
            Tower = new TowerState();
            TickSystem = new GameTickSystem(this);
        }

        public void InitializeNewGame()
        {
            Wallet.Clear();
            Tower.ResetWithEmptyBuildSlot();

            if (Balance?.StartingResources != null)
            {
                foreach (var entry in Balance.StartingResources)
                {
                    if (entry.Resource != null && entry.Amount > 0)
                        Wallet.Add(entry.Resource, entry.Amount);
                }
            }
        }
    }
}
