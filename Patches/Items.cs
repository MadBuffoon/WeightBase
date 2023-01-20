using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WeightBase.Tools;

namespace WeightBase.Patches;

public class Items
{
    internal static readonly Dictionary<string, ItemCache> OgItemCaches = new();

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    [HarmonyPriority(Priority.Last +
                     1)] // To load last but not the last. This is to load after every mod to get the modded items.
    private static class UpdateItemsLoad
    {
        private static void Postfix(ObjectDB __instance)
        {
            WeightBasePlugin.WeightBaseLogger.LogDebug("UpdateItemsLoad Awaked");
            UpdateItemDatabase(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenRespons))]
    internal static class UpdateItemsContainer
    {
        private static void Postfix(Container __instance)
        {
            foreach (ItemDrop.ItemData? item in __instance.m_inventory.m_inventory)
            {
                string nameOfItem = Utils.GetPrefabName(item.m_dropPrefab) + ",";
                UpdateItem(item, nameOfItem);
            }

            __instance.m_inventory.UpdateTotalWeight();
            if (InventoryGui.instance == null) return;
            InventoryGui.instance.UpdateContainerWeight();
        }
    }

    internal static void UpdateItemDatabase(ObjectDB __instance)
    {
        if (!WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value && !WeightBasePlugin.ItemWeightEnabledConfig.Value)
        {
            return;
        }

        WeightBasePlugin.WeightBaseLogger.LogDebug("UpdateItemDatabase Running");
        foreach (GameObject? gameObject in __instance.m_items)
        {
            ItemDrop? item = gameObject.GetComponent<ItemDrop>();
            string nameOfItem = Utils.GetPrefabName(item.transform.root.gameObject) + ",";
            UpdateItem(item.m_itemData, nameOfItem);
        }
    }

    internal static void UpdateItem(ItemDrop.ItemData item, string itemName)
    {
        if (item?.m_shared == null)
        {
            return;
        }

        ItemDrop.ItemData.SharedData? shared = item.m_shared;
        if (!OgItemCaches.ContainsKey(shared.m_name))
        {
            OgItemCaches.Add(shared.m_name, new ItemCache(shared.m_name, shared.m_maxStackSize, shared.m_weight));
        }


        string? includeList = WeightBasePlugin.ItemIncludeListConfig.Value;
        string? excludeList = WeightBasePlugin.ItemExcludeListConfig.Value;
        string? noWeightList = WeightBasePlugin.ItemNoWeightListConfig.Value;
        if (excludeList.Contains(itemName))
        {
            shared.m_maxStackSize = OgItemCaches[shared.m_name].ItemStackOG;
            shared.m_weight = OgItemCaches[shared.m_name].ItemWeightOG;
            return;
        }

        if (includeList.Contains(itemName) || shared.m_maxStackSize > 1)
        {
            shared.m_maxStackSize = WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value
                ? 1000000
                : OgItemCaches[shared.m_name].ItemStackOG;
            /*if (itemUnlimitedStackEnabledConfig.Value)
            {
                shared.m_maxStackSize = Int32.MaxValue;
            }
            else
            {
                shared.m_maxStackSize = ogItemCaches[shared.m_name].ItemStackOG;
            }*/
            // Weight

            shared.m_weight = WeightBasePlugin.ItemWeightEnabledConfig.Value
                ? OgItemCaches[shared.m_name].ItemWeightOG * WeightBasePlugin.ItemWeightConfig.Value
                : OgItemCaches[shared.m_name].ItemWeightOG;
        }

        if (noWeightList.Contains(itemName))
        {
            shared.m_weight = 0f;
        }
    }
}