using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using HarmonyLib;
using UnityEngine;
using WeightBase.Tools;

namespace WeightBase.Patches;

public class UI
{
    
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    private static class HideMaxStackSizeInventoryGrid
    {
        private static void Postfix(InventoryGrid __instance, Inventory ___m_inventory)
        {
            if (WeightBasePlugin._itemUnlimitedStackEnabledConfig.Value)
            {
                var width = ___m_inventory.GetWidth();

                foreach (var allItem in ___m_inventory.GetAllItems())
                {
                    if (allItem.m_shared.m_maxStackSize > 1)
                    {
                        var e = __instance.GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y,
                            width);
                        e.m_amount.text = Helper.FormatNumberSimple(allItem.m_stack);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
    private static class HideMaxStackSizeHotkeyBar
    {
        //[HarmonyPriority(Priority.Last + 1)]
        private static void Postfix(HotkeyBar __instance, List<ItemDrop.ItemData> ___m_items,
            List<HotkeyBar.ElementData> ___m_elements)
        {
            if (WeightBasePlugin._itemUnlimitedStackEnabledConfig.Value)
            {
                var player = Player.m_localPlayer;
                if (player == null || player.IsDead() || __instance == null) return;
                if (__instance.m_elements != null)
                {
                    for (var j = 0; j < ___m_items.Count; j++)
                    {
                        var itemData = ___m_items[j];
                        try
                        {
                            var elementData2 = ___m_elements[itemData.m_gridPos.x];
                            if (itemData.m_shared.m_maxStackSize > 1)
                            {
                                elementData2.m_amount.text = Helper.FormatNumberSimple(itemData.m_stack);
                            }
                        }
                        catch
                        {
                            var elementData2 = __instance.m_elements[itemData.m_gridPos.x - 5];
                            if (itemData.m_shared.m_maxStackSize > 1)
                            {
                                elementData2.m_amount.text = Helper.FormatNumberSimple(itemData.m_stack);
                            }
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(InventoryGui), "UpdateInventoryWeight")]
    private static class DisplayPLayerMaxWeight
    {
        private static void Postfix(InventoryGui __instance, Player player)
        {
            var currentWeight = (float)Math.Round(player.m_inventory.m_totalWeight * 100f) / 100f;
            var MaxCarryWeight = player.GetMaxCarryWeight();

            if (currentWeight > MaxCarryWeight && Mathf.Sin(Time.time * 10f) > 0.0)
                __instance.m_weight.text = "<color=red>" + Helper.FormatNumberSimple(currentWeight) +
                                           "</color>\n<color=white>" +
                                           Helper.FormatNumberSimple(MaxCarryWeight) + "</color>";
            else
                __instance.m_weight.text = "" + Helper.FormatNumberSimple(currentWeight) + "\n<color=white>" +
                                           Helper.FormatNumberSimple(MaxCarryWeight) + "</color>";
        }
    } 
    //[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
    private static class DisplayContainerMaxWeight
    {
        private static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
        {
            //if (___m_currentContainer == null || !___m_currentContainer.transform.parent) return;
            if (___m_currentContainer == null) return;
            //var totalWeight = Mathf.CeilToInt(___m_currentContainer.GetInventory().GetTotalWeight());
           var totalWeight = ___m_currentContainer.m_inventory.m_totalWeight;

            __instance.m_containerWeight.text = Helper.FormatNumberSimple(totalWeight);

            if (!WeightBasePlugin._shipMassToWeightEnabledConfig.Value)
            {
                return;
            }

            if (___m_currentContainer.m_rootObjectOverride == null ||
                !___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>())
                return; // Checks to make sure its a SHIP!

            var shipID = ___m_currentContainer.m_nview.m_zdo.m_uid;
            var shipMass = Ships.shipBaseMasses.ContainsKey(shipID);
            var weightFacter = 0f;
            var totalShipWeight = 0f;
            if (shipMass)
            {
                var shipBaseMass = Ships.shipBaseMasses[shipID] * WeightBasePlugin._shipMassScaleConfig.Value;
                var currentShip = ___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>();
                foreach (var player in currentShip.m_players)
                {
                    totalWeight += player.m_inventory.GetTotalWeight();
                }
                weightFacter = Mathf.Floor((totalWeight / shipBaseMass) * 100f);
            }
            //}


            try
            {
                if (weightFacter > 100f && Mathf.Sin(Time.time * 10f) > 0.0)
                {
                    __instance.m_containerWeight.text = "" + Helper.FormatNumberSimple(totalWeight) +
                                                        "\n<color=red>" + weightFacter +
                                                        " %</color>";
                }
                else
                {
                    __instance.m_containerWeight.text =
                        "" + Helper.FormatNumberSimple(totalWeight) + "\n<color=white>" +
                        weightFacter + " %</color>";
                }
            }
            catch
            {
                __instance.m_containerWeight.text = "Failed";
            }
        }
    }
        
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
    private static class TotalWeightFix
    {
        private static bool Prefix(Inventory __instance)
        {
            try
            {
                var totalWeight = 0f;
                foreach (var itemData in __instance.m_inventory)
                {
                    totalWeight += itemData.GetWeight();
                }
            }
            catch (OverflowException)
            {
                __instance.m_totalWeight = float.MaxValue;
                return false;
            }

            return true;
        }
    }
}