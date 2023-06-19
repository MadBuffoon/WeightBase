namespace WeightBase.Tools
{
    public class ItemCache
    {
        public readonly string ItemName;
        public readonly int ItemStackOG;
        public readonly float ItemWeightOG;

        public ItemCache(string itemName, int itemStack, float itemWeight)
        {
            ItemName = itemName;
            ItemStackOG = itemStack;
            ItemWeightOG = itemWeight;
        }
    }
}