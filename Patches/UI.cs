using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
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
            int width = ___m_inventory.GetWidth();

            foreach (ItemDrop.ItemData? allItem in ___m_inventory.GetAllItems())
            {
                if (allItem.m_shared.m_maxStackSize <= 1) continue;
                InventoryGrid.Element? e = __instance.GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y,
                    width);
                e.m_amount.text = Helper.FormatNumberSimple(allItem.m_stack);
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
            Player? player = Player.m_localPlayer;
            if (player == null || player.IsDead() || __instance == null) return;
            if (__instance.m_elements == null) return;
            foreach (ItemDrop.ItemData? itemData in ___m_items)
            {
                if (itemData.m_shared.m_maxStackSize <= 1) continue;
                int pos = itemData.m_gridPos.x;
                if (pos < 0 || pos >= ___m_elements.Count) continue; // if statement to check the bounds of the array before accessing the element.
                HotkeyBar.ElementData elementData2 = ___m_elements[pos];
                elementData2.m_amount.text = Helper.FormatNumberSimple(itemData.m_stack);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
    static class DisplayPLayerMaxWeight
    {
        static void Postfix(InventoryGui __instance, Player player)
        {
            float currentWeight = (float)Math.Round(player.m_inventory.m_totalWeight * 100f) / 100f;
            float maxCarryWeight = player.GetMaxCarryWeight();

            if (currentWeight > maxCarryWeight && (double)Mathf.Sin(Time.time * 10f) > 0.0)
                __instance.m_weight.text = "<color=red>" + Helper.FormatNumberSimple(currentWeight) + "</color>\n<color=white>" +
                                           Helper.FormatNumberSimple(maxCarryWeight) + "</color>";
            else
                __instance.m_weight.text = "" + Helper.FormatNumberSimple(currentWeight) + "\n<color=white>" +
                                           Helper.FormatNumberSimple(maxCarryWeight) + "</color>";
        }
    }


    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainerWeight))]
    private static class DisplayContainerMaxWeight
    {
        private static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
        {
            //if (___m_currentContainer == null || !___m_currentContainer.transform.parent) return;
            if (___m_currentContainer == null) return;
            //var totalWeight = Mathf.CeilToInt(___m_currentContainer.GetInventory().GetTotalWeight());
            float totalWeight = ___m_currentContainer.m_inventory.GetTotalWeight();


            __instance.m_containerWeight.text = Helper.FormatNumberSimple(totalWeight);

            if (!WeightBasePlugin.ShipMassToWeightEnabledConfig.Value)
            {
                return;
            }

            if (___m_currentContainer.m_rootObjectOverride == null ||
                !___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>())
                return; // Checks to make sure its a SHIP!

            ZDOID shipID = ___m_currentContainer.m_nview.m_zdo.m_uid;
            bool shipMass = Ships.shipBaseMasses.ContainsKey(shipID);
            float weightFacter = 0f;
            float totalShipWeight = 0f;
            if (shipMass)
            {
                float shipBaseMass = Ships.shipBaseMasses[shipID] * WeightBasePlugin.ShipMassScaleConfig.Value;
                Ship? currentShip = ___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>();
                totalWeight += currentShip.m_players.Sum(playerShip => playerShip.m_inventory.GetTotalWeight());

                weightFacter = Mathf.Floor((totalWeight / shipBaseMass) * 100f);
            }


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
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
    private static class TotalWeightFix
    {
        private static bool Prefix(Inventory __instance)
        {
            try
            {
                float totalWeight = __instance.m_inventory.Sum(itemData => itemData.GetWeight());
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