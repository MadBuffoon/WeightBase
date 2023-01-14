namespace WeightBase.Tools;

public class ItemCache
{
   public string ItemName;
   public int ItemStackOG;
   public float ItemWeightOG;
   public ItemCache(string _itemName, int _itemStack, float _itemWeight)
   {
       ItemName = _itemName;
       ItemStackOG = _itemStack;
       ItemWeightOG = _itemWeight;
   }
      
}