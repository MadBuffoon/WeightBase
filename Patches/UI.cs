using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using WeightBase.Tools;

namespace WeightBase.Patches
{
    public static class UI
    {
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private static class HideMaxStackSizeInventoryGrid
        {
            private static void Postfix(InventoryGrid __instance, Inventory ___m_inventory)
            {
                if (!WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value) return;

                int width = ___m_inventory.GetWidth();

                foreach (var allItem in ___m_inventory.GetAllItems())
                {
                    if (allItem.m_shared.m_maxStackSize <= 1) continue;
                    var e = __instance.GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y, width);
                    e.m_amount.text = Helper.FormatNumberSimpleNoDecimal((float)allItem.m_stack);

                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private static class HideMaxStackSizeHotkeyBar
        {
            private static void Postfix(HotkeyBar __instance, List<ItemDrop.ItemData> ___m_items, List<HotkeyBar.ElementData> ___m_elements)
            {
                if (!WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value) return;

                var player = Player.m_localPlayer;
                if (player == null || player.IsDead() || __instance == null || __instance.m_elements == null) return;

                Action<ItemDrop.ItemData> processItemData = itemData =>
                {
                    if (itemData.m_shared.m_maxStackSize > 1)
                    {
                        int pos = itemData.m_gridPos.x;
                        if (pos >= 0 && pos < ___m_elements.Count)
                        {
                            ___m_elements[pos].m_amount.text = Helper.FormatNumberSimpleNoDecimal(itemData.m_stack);
                        }
                    }
                };

                foreach (var itemData in ___m_items)
                {
                    processItemData(itemData);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
        private static class DisplayPLayerMaxWeight
        {
            private static bool Prefix(InventoryGui __instance, Player player)
            {
                if (!InventoryGui.IsVisible()) return true;

                var currentWeight = (float)Math.Round(player.m_inventory.m_totalWeight * 100f) / 100f;
                var maxCarryWeight = player.GetMaxCarryWeight();

                bool highlightWeight = currentWeight > maxCarryWeight && Mathf.Sin(Time.time * 10f) > 0.0;
                string color = highlightWeight ? "red" : "white";

                __instance.m_weight.text = $"<color={color}>{Helper.FormatNumberSimple(currentWeight)}</color>\n<color=white>{Helper.FormatNumberSimpleNoDecimal(maxCarryWeight)}</color>";

                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainerWeight))]
        private static class DisplayContainerMaxWeight
        {
            private static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
            {
                if (!InventoryGui.IsVisible() || ___m_currentContainer == null) return;

                float totalContainerWeight = ___m_currentContainer.m_inventory.GetTotalWeight();
                __instance.m_containerWeight.text = Helper.FormatNumberSimple(totalContainerWeight);

                if (!WeightBasePlugin.ShipMassToWeightEnabledConfig.Value) return;
                if (___m_currentContainer.m_rootObjectOverride == null ||
                    !___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>()) return; // Checks to make sure it's a SHIP!

                ZDOID shipID = ___m_currentContainer.m_nview.m_zdo.m_uid;

                if (!Ships.shipBaseMasses.ContainsKey(shipID)) return;

                float shipBaseMass = Ships.shipBaseMasses[shipID] * WeightBasePlugin.ShipMassScaleConfig.Value;
                Ship currentShip = ___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>();
                float totalPlayerWeight = currentShip.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));
                float totalShipWeight = totalContainerWeight + totalPlayerWeight;
                float weightFacter = totalShipWeight / shipBaseMass * 100f;

                bool highlightWeightFacter = weightFacter > 100f && Mathf.Sin(Time.time * 10f) > 0.0;
                string color = highlightWeightFacter ? "red" : "white";

                __instance.m_containerWeight.text = $"{Helper.FormatNumberSimple(totalShipWeight)}\n<color={color}>{Helper.FormatNumberSimpleNoDecimal(weightFacter)} %</color>";
            }
        }
    }
}