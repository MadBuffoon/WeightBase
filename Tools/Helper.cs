using System;
using UnityEngine;
using WeightBase.Patches;

namespace WeightBase.Tools
{
    public static class Helper
    {
        private static string[] suffixes = {"", "k", "m", "b"};

        internal static string FormatNumberSimple(float number)
        {
            CalculateShortNumberAndSuffix(number, out double shortNumber, out string suffix);
            return $"{shortNumber:N2}{suffix}";
        }

        internal static string FormatNumberSimpleNoDecimal(float number)
        {
            CalculateShortNumberAndSuffix(number, out double shortNumber, out string suffix);
            return $"{shortNumber:N0}{suffix}";
        }

        private static void CalculateShortNumberAndSuffix(float number, out double shortNumber, out string suffix)
        {
            int mag = Math.Max(0, (int)(Math.Log10(Math.Abs(number) + 0.001) / 3));
            mag = Math.Min(suffixes.Length - 1, mag);
            double divisor = Math.Pow(10, mag * 3);

            shortNumber = number / divisor;
            suffix = suffixes[mag];
        }

        public static void ApplyWeightFactor(Ship __instance, Rigidbody ___m_body, float weightFacter, float containerWeight, float playersTotalWeight, float shipBaseMass)
        {
            weightFacter *= 2f;
            if (weightFacter > 0.9f)
            {
                weightFacter = 1f;
            }

            var fixedDeltaTime = Time.fixedDeltaTime;

            ApplySailForce(__instance, ___m_body, weightFacter);
            ApplyRudderForce(__instance, ___m_body, weightFacter, fixedDeltaTime);
            ApplyVisualWeight(__instance, ___m_body, weightFacter, containerWeight, playersTotalWeight, shipBaseMass);
        }

        public static void ApplySailForce(Ship __instance, Rigidbody ___m_body, float weightFacter)
        {
            if (__instance.m_speed is Ship.Speed.Half or Ship.Speed.Full)
            {
                var worldCenterOfMass = __instance.m_body.worldCenterOfMass;
                var force = __instance.m_sailForce * -1.0f * weightFacter;
                ___m_body.AddForceAtPosition(force, worldCenterOfMass + __instance.transform.up * __instance.m_sailForceOffset, ForceMode.VelocityChange);
            }
        }

        public static void ApplyRudderForce(Ship __instance, Rigidbody ___m_body, float weightFacter, float fixedDeltaTime)
        {
            if (__instance.m_speed is Ship.Speed.Back or Ship.Speed.Slow)
            {
                var transform = __instance.transform;
                var position = transform.position + transform.forward * __instance.m_stearForceOffset;
                var force = Vector3.zero;

                switch (__instance.m_speed)
                {
                    case Ship.Speed.Back:
                        force += transform.forward * __instance.m_backwardForce * (1f - Mathf.Abs(__instance.m_rudderValue * weightFacter));
                        break;
                    case Ship.Speed.Slow:
                        force += -transform.forward * __instance.m_backwardForce * (1f - Mathf.Abs(__instance.m_rudderValue * weightFacter));
                        break;
                }

                ___m_body.AddForceAtPosition(force * fixedDeltaTime, position, ForceMode.VelocityChange);
            }
        }

        public static void ApplyVisualWeight(Ship __instance, Rigidbody ___m_body, float weightFacter, float containerWeight, float playersTotalWeight, float shipBaseMass)
        {
            if (!WeightBasePlugin.ShipMassWeightLookEnableConfig.Value)
                return;

            var weightPercent = (containerWeight + playersTotalWeight) / shipBaseMass - 1;
            var weightForce = Mathf.Clamp(weightPercent, 0.0f, 0.5f);

            if (weightFacter >= 1.5f && WeightBasePlugin.ShipMassSinkEnableConfig.Value)
            {
                weightForce = 2f;
            }

            ___m_body.AddForceAtPosition(Vector3.down * weightForce, ___m_body.worldCenterOfMass, ForceMode.VelocityChange);
        }

        internal static void UpdateItemDatabase(ObjectDB __instance)
        {
            if (WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value || WeightBasePlugin.ItemWeightEnabledConfig.Value)
            {
                WeightBasePlugin.WeightBaseLogger.LogDebug("UpdateItemDatabase Running");
                foreach (GameObject gameObject in __instance.m_items)
                {
                    ItemDrop item = gameObject.GetComponent<ItemDrop>();
                    string itemName = Utils.GetPrefabName(item.transform.root.gameObject) + ",";
                    UpdateItem(item.m_itemData, itemName);
                }
            }
        }

        internal static void UpdateContainerItems(Container __instance)
        {
            foreach (ItemDrop.ItemData item in __instance.m_inventory.m_inventory)
            {
                string itemName = Utils.GetPrefabName(item.m_dropPrefab) + ",";
                UpdateItem(item, itemName);
            }
        }

        internal static void UpdateItem(ItemDrop.ItemData item, string itemName)
        {
            if (item?.m_shared == null)
                return;

            ItemDrop.ItemData.SharedData shared = item.m_shared;
            if (!Items.OgItemCaches.ContainsKey(shared.m_name))
                Items.OgItemCaches.Add(shared.m_name, new ItemCache(shared.m_name, shared.m_maxStackSize, shared.m_weight));

            string includeList = WeightBasePlugin.ItemIncludeListConfig.Value;
            string excludeList = WeightBasePlugin.ItemExcludeListConfig.Value;
            string noWeightList = WeightBasePlugin.ItemNoWeightListConfig.Value;

            if (excludeList.Contains(itemName))
            {
                ResetItemStatsToOriginal(shared);
                return;
            }

            if (includeList.Contains(itemName) || shared.m_maxStackSize > 1)
                UpdateItemStats(shared);

            if (noWeightList.Contains(itemName))
                shared.m_weight = 0f;
        }

        internal static void UpdateItemStats(ItemDrop.ItemData.SharedData shared)
        {
            shared.m_maxStackSize = WeightBasePlugin.ItemUnlimitedStackEnabledConfig.Value
                ? 1000000
                : Items.OgItemCaches[shared.m_name].ItemStackOG;

            shared.m_weight = WeightBasePlugin.ItemWeightEnabledConfig.Value
                ? Items.OgItemCaches[shared.m_name].ItemWeightOG * WeightBasePlugin.ItemWeightConfig.Value
                : Items.OgItemCaches[shared.m_name].ItemWeightOG;
        }

        internal static void ResetItemStatsToOriginal(ItemDrop.ItemData.SharedData shared)
        {
            if (!Items.OgItemCaches.ContainsKey(shared.m_name))
                return;
            shared.m_maxStackSize = Items.OgItemCaches[shared.m_name].ItemStackOG;
            shared.m_weight = Items.OgItemCaches[shared.m_name].ItemWeightOG;
        }

        internal static void UpdateContainerWeightInGui()
        {
            if (InventoryGui.instance != null)
                InventoryGui.instance.UpdateContainerWeight();
        }
    }
}