using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using WeightBase.Tools;

namespace WeightBase.Patches;

public static class UI
{
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    private static class HideMaxStackSizeInventoryGrid
    {
        private static void Postfix(InventoryGrid __instance, Inventory ___m_inventory)
        {
            if (!WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value) return;
            var width = ___m_inventory.GetWidth();

            foreach (var allItem in ___m_inventory.GetAllItems())
            {
                if (allItem.m_shared.m_maxStackSize <= 1) continue;
                var e = __instance.GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y,
                    width);
                e.m_amount.text = Helper.FormatNumberSimpleNoDecimal(allItem.m_stack);
            }
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    private static class HideMaxStackSizeHotkeyBar
    {
        //[HarmonyPriority(Priority.Last + 1)]
        private static void Postfix(HotkeyBar __instance, List<ItemDrop.ItemData> ___m_items,
            List<HotkeyBar.ElementData> ___m_elements)
        {
            if (!WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value) return;
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead() || __instance == null) return;
            if (__instance.m_elements == null) return;
            foreach (var itemData in ___m_items)
            {
                if (itemData.m_shared.m_maxStackSize <= 1) continue;
                int pos = itemData.m_gridPos.x;
                //int pos =  Chainloader.PluginInfos.ContainsKey("odinplusqol.OdinsExtendedInventory") ? itemData.m_gridPos.x - 5 : itemData.m_gridPos.x;
                if (pos < 0 || pos >= ___m_elements.Count)
                    continue; // if statement to check the bounds of the array before accessing the element.
                var elementData2 =  ___m_elements[pos];
                elementData2.m_amount.text = Helper.FormatNumberSimpleNoDecimal(itemData.m_stack);
            }

            if (Chainloader.PluginInfos.ContainsKey("odinplusqol.OdinsExtendedInventory"))
            {
                foreach (var itemData in ___m_items)
                {
                    if (itemData.m_shared.m_maxStackSize <= 1) continue;
                    int pos = itemData.m_gridPos.x - 5;
                    if (pos < 0 || pos >= ___m_elements.Count)
                        continue; // if statement to check the bounds of the array before accessing the element.
                    var elementData2 = ___m_elements[pos];
                    elementData2.m_amount.text = Helper.FormatNumberSimpleNoDecimal(itemData.m_stack);
                }
            }
            
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
    private static class DisplayPLayerMaxWeight
    {
        private static void Postfix(InventoryGui __instance, Player player)
        {
            if (!InventoryGui.IsVisible()) return;
            var currentWeight = (float)Math.Round(player.m_inventory.m_totalWeight * 100f) / 100f;
            var maxCarryWeight = player.GetMaxCarryWeight();

            if (currentWeight > maxCarryWeight && Mathf.Sin(Time.time * 10f) > 0.0)
                __instance.m_weight.text = $"<color=red>{Helper.FormatNumberSimple(currentWeight)}</color>\n<color=white>{Helper.FormatNumberSimpleNoDecimal(maxCarryWeight)}</color>";
            else
                __instance.m_weight.text = $"{Helper.FormatNumberSimple(currentWeight)}\n<color=white>{Helper.FormatNumberSimpleNoDecimal(maxCarryWeight)}</color>";
        }
    }


    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainerWeight))]
    private static class DisplayContainerMaxWeight
    {
        private static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
        {
            if (!InventoryGui.IsVisible()) return;
            //if (___m_currentContainer == null || !___m_currentContainer.transform.parent) return;
            if (___m_currentContainer == null) return;
            float totalContainerWeight = ___m_currentContainer.m_inventory.GetTotalWeight();


            __instance.m_containerWeight.text = Helper.FormatNumberSimple(totalContainerWeight);

            if (!WeightBasePlugin.ShipMassToWeightEnabledConfig.Value)
            {
                return;
            }

            if (___m_currentContainer.m_rootObjectOverride == null ||
                !___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>())
                return; // Checks to make sure its a SHIP!

            ZDOID shipID = ___m_currentContainer.m_nview.m_zdo.m_uid;
            bool shipMass = Ships.shipBaseMasses.ContainsKey(shipID);
            if (!shipMass) return;

            float shipBaseMass = Ships.shipBaseMasses[shipID] * WeightBasePlugin.ShipMassScaleConfig.Value;
            Ship currentShip = ___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>();
            float totalPlayerWeight =
                currentShip.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));
            float totalShipWeight = totalContainerWeight + totalPlayerWeight;
            float weightFacter = (totalContainerWeight + totalPlayerWeight) / shipBaseMass * 100f;


            if (weightFacter > 100f && Mathf.Sin(Time.time * 10f) > 0.0)
            {
                __instance.m_containerWeight.text = $"{Helper.FormatNumberSimple(totalShipWeight)}\n<color=red>{Helper.FormatNumberSimpleNoDecimal(weightFacter)} %</color>";
            }
            else
            {
                __instance.m_containerWeight.text = $"{Helper.FormatNumberSimple(totalShipWeight)}\n<color=white>{Helper.FormatNumberSimpleNoDecimal(weightFacter)} %</color>";
            }
        }
    }

    /*[HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
    private static class TotalWeightFix
    {
        private static bool Prefix(Inventory __instance)
        {
            try
            {
                var totalWeight = __instance.m_inventory.Sum(itemData => itemData.GetWeight());
            }
            catch (OverflowException)
            {
                __instance.m_totalWeight = float.MaxValue;
                return false;
            }

            return true;
        }
    }*/
}