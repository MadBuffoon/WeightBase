using System.Collections.Generic;
using HarmonyLib;
using WeightBase.Tools;

namespace WeightBase.Patches;

public class Items
{
    
    
    internal static readonly Dictionary<string, ItemCache> ogItemCaches = new();
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    [HarmonyPriority(Priority.Last +
                     1)] // To load last but not the last. This is to load after every mod to get the modded items.
    private static class UpdateItemsLoad
    {
        private static void Postfix(ObjectDB __instance)
        {
            WeightBasePlugin.WeightBaseLogger.LogInfo("UpdateItemsLoad Awaked");
            UpdateItemDatabase(__instance);
        }
    }
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenRespons))]
    internal static class UpdateItemsContainer
    {
        private static void Postfix(Container __instance)
        {
            foreach (var item in __instance.m_inventory.m_inventory)
            {
                var nameOfItem = Utils.GetPrefabName(item.m_dropPrefab) + ",";
                UpdateItem(item, nameOfItem);
            }

            __instance.m_inventory.UpdateTotalWeight();
            if (InventoryGui.instance == null) return;
            InventoryGui.instance.UpdateContainerWeight();
        }
    }

    internal static void UpdateItemDatabase(ObjectDB __instance)
    {
        if (!WeightBasePlugin._itemUnlimitedStackEnabledConfig.Value && !WeightBasePlugin._itemWeightEnabledConfig.Value)
        {
            return;
        }

        WeightBasePlugin.WeightBaseLogger.LogInfo("UpdateItemDatabase Running");
        foreach (var gameObject in __instance.m_items)
        {
            var item = gameObject.GetComponent<ItemDrop>();
            var nameOfItem = Utils.GetPrefabName(item.transform.root.gameObject) + ",";
            UpdateItem(item.m_itemData, nameOfItem);
        }

        
        
    }

    internal static void UpdateItem(ItemDrop.ItemData item, string itemName)
    {
        if (item == null || item.m_shared == null)
        {
            return;
        }

        var shared = item.m_shared;
        if (!ogItemCaches.ContainsKey(shared.m_name))
        {
            ogItemCaches.Add(shared.m_name, new ItemCache(shared.m_name, shared.m_maxStackSize, shared.m_weight));
        }


        var includeList = WeightBasePlugin._itemIncludeListConfig.Value;
        var excludeList = WeightBasePlugin._itemExcludeListConfig.Value;
        if (excludeList.Contains(itemName))
        {
            shared.m_maxStackSize = ogItemCaches[shared.m_name].ItemStackOG;
            shared.m_weight = ogItemCaches[shared.m_name].ItemWeightOG;
            return;
        }
        if (includeList.Contains(itemName) || shared.m_maxStackSize > 1)
        {
            shared.m_maxStackSize = WeightBasePlugin._itemUnlimitedStackEnabledConfig.Value
                ? 1000000
                : ogItemCaches[shared.m_name].ItemStackOG;
            /*if (itemUnlimitedStackEnabledConfig.Value)
            {
                shared.m_maxStackSize = Int32.MaxValue;
            }
            else
            {
                shared.m_maxStackSize = ogItemCaches[shared.m_name].ItemStackOG;
            }*/
            // Weight
            
            shared.m_weight = WeightBasePlugin._itemWeightEnabledConfig.Value
                ? ogItemCaches[shared.m_name].ItemWeightOG * WeightBasePlugin._itemWeightConfig.Value
                : ogItemCaches[shared.m_name].ItemWeightOG;
           
        }

        
    }
}