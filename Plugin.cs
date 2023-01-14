using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using System.Collections.Generic;

namespace WeightBase
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WeightBasePlugin : BaseUnityPlugin
    {
        internal const string ModName = "WeightBase";
        internal const string ModVersion = "1.0.1";
        internal const string Author = "MadBuffoon";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        internal static string ConnectionError = "";

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource WeightBaseLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public static ManualLogSource logger;


        public void Awake()
        {
            // 1 - General
            _serverConfigLocked = config("1 - General", "1.1 Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            DebugLoggingConfig = config("1 - General", "1.2 DeBug Logging", false,
                "This turns on console debug msgs.");
            _ = ConfigSync.AddConfigEntry(DebugLoggingConfig);

            itemUnlimitedStackEnabledConfig = config("2 - Settings", "2.1 Remove Stack Limit", true,
                "Should item stack size limit be removed? Will need to restart game/server!");
            _ = ConfigSync.AddConfigEntry(itemUnlimitedStackEnabledConfig);
            itemWeightEnabledConfig = config("2 - Settings", "2.2 Weight Reduction", true,
                "Should item weight Reduction be enabled? Will need to restart game/server!");
            _ = ConfigSync.AddConfigEntry(itemWeightEnabledConfig);
            itemWeightConfig = config("2 - Settings", "2.3 Item Weight", 1.0f,
                new ConfigDescription(
                    "How much an item weighs. 1 is normal weight and 2 being 2x the normal weight then 0.5 is half normal weight. ",
                    new AcceptableValueRange<float>(0f, 2f)));
            _ = ConfigSync.AddConfigEntry(itemWeightConfig);
            containerWeightLimitEnabledConfig = config("2 - Settings", "2.3 Ships have weight limit", true,
                "Should containers have weight limits?");
            _ = ConfigSync.AddConfigEntry(containerWeightLimitEnabledConfig);

            // 3 - ShipKarveCargoIncrease
            shipKarveCargoIncreaseEnabledConfig = config("3 - Karve Ship", "3.1 Enabled", true,
                "Should Karve cargo hold size be increased?");
            _ = ConfigSync.AddConfigEntry(shipKarveCargoIncreaseEnabledConfig);
            shipKarveCargoIncreaseColumnsConfig = config("3 - Karve Ship",
                "3.2 INV Width/Colums", 3,
                new ConfigDescription("Number of columns for the Karve cargo hold. Max of 6.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            _ = ConfigSync.AddConfigEntry(shipKarveCargoIncreaseColumnsConfig);
            shipKarveCargoIncreaseRowsConfig = config("3 - Karve Ship",
                "3.3 INV Height/Rows", 2,
                new ConfigDescription("Number of rows for the Karve cargo hold. Max of 3.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            _ = ConfigSync.AddConfigEntry(shipKarveCargoIncreaseRowsConfig);
            shipKarveCargoWeightLimitConfig =
                config("3 - Karve Ship", "3.4 Weight Limit", 1200, "Weight limit for the Karve");
            _ = ConfigSync.AddConfigEntry(shipKarveCargoWeightLimitConfig);

            // 4 - Viking
            shipvikingCargoIncreaseEnabledConfig = config("4 - Viking Ship", "4.1 Enabled", true,
                "Should viking cargo hold size be increased?");
            _ = ConfigSync.AddConfigEntry(shipvikingCargoIncreaseEnabledConfig);
            shipvikingCargoIncreaseColumnsConfig = config("4 - Viking Ship",
                "4.2 INV Width/Colums", 5,
                new ConfigDescription("Number of columns for the viking cargo hold. Max of 8.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            _ = ConfigSync.AddConfigEntry(shipvikingCargoIncreaseColumnsConfig);
            shipvikingCargoIncreaseRowsConfig = config("4 - Viking Ship",
                "4.3 INV Height/Rows", 3,
                new ConfigDescription("Number of rows for the viking cargo hold. Max of 4.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            _ = ConfigSync.AddConfigEntry(shipvikingCargoIncreaseRowsConfig);
            shipvikingCargoWeightLimitConfig = config("4 - Viking Ship", "4.4 Weight Limit", 4200,
                "Weight limit for the Longship");
            _ = ConfigSync.AddConfigEntry(shipvikingCargoWeightLimitConfig);

            // 5 - Custom Ships
            shipCustomCargoIncreaseEnabledConfig = config("5 - Custom Ship", "5.1 Enabled", true,
                "Should Custom cargo hold size be increased?");
            _ = ConfigSync.AddConfigEntry(shipCustomCargoIncreaseEnabledConfig);
            shipCustomCargoIncreaseColumnsConfig = config("5 - Custom Ship",
                "5.2 INV Width/Colums", 5,
                new ConfigDescription("Number of columns for the Custom cargo hold. Max of 8.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            _ = ConfigSync.AddConfigEntry(shipCustomCargoIncreaseColumnsConfig);
            shipCustomCargoIncreaseRowsConfig = config("5 - Custom Ship",
                "5.3 INV Height/Rows", 3,
                new ConfigDescription("Number of rows for the Custom cargo hold. Max of 4.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            _ = ConfigSync.AddConfigEntry(shipCustomCargoIncreaseRowsConfig);
            CustomShipWeightLimitConfig = config("5 - Custom Ship", "5.4 Weight Limit", 4200,
                "Weight limit for the Custom Ships");
            _ = ConfigSync.AddConfigEntry(CustomShipWeightLimitConfig);

            /*
            woodChestWeightLimitConfig = config("5 - ContainerWeightLimit", "wood chest weight limit", 1000,
                "Weight limit for the Wood Chest"
            );
            _ = ConfigSync.AddConfigEntry(woodChestWeightLimitConfig);
            personalChestWeightLimitConfig = config("5 - ContainerWeightLimit", "personal chest weight limit", 1000,
                "Weight limit for the Personal Chest"
            );
            _ = ConfigSync.AddConfigEntry(personalChestWeightLimitConfig);
            reinforcedChestWeightLimitConfig = config("5 - ContainerWeightLimit", "reinforced chest weight limit", 2000,
                "Weight limit for the Reinforced Chest"
            );
            _ = ConfigSync.AddConfigEntry(reinforcedChestWeightLimitConfig);
            blackMetalChestWeightLimitConfig = config("5 - ContainerWeightLimit", "blackmetal chest weight limit", 4000,
                "Weight limit for the Black Metal Chest"
            );
            _ = ConfigSync.AddConfigEntry(blackMetalChestWeightLimitConfig);
            CustomChestWeightLimitConfig = config("5 - ContainerWeightLimit", "Custom chest weight limit", 4000,
                "Weight limit for the Custom Chest"
            );
            _ = ConfigSync.AddConfigEntry(CustomChestWeightLimitConfig);
            */
            // End of Config Settings

            logger = Logger;


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();

            /*
            if (Player.m_localPlayer != null)
            {
                foreach (var item in Player.m_localPlayer.GetInventory().m_inventory)
                {
                    var updateItem = ObjectDB.instance.GetItemPrefab(item.m_dropPrefab?.name);
                    item.m_shared = updateItem.GetComponent<ItemDrop>().m_itemData.m_shared;
                }
            }*/
        }


        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        static class UpdateItemsLoad
        {
            static void Postfix(ObjectDB __instance)
            {
                if (itemUnlimitedStackEnabledConfig.Value || itemWeightEnabledConfig.Value)
                {
                    foreach (ItemDrop.ItemData.ItemType type in (ItemDrop.ItemData.ItemType[])Enum.GetValues(
                                 typeof(ItemDrop.ItemData.ItemType)))
                    {
                        foreach (ItemDrop item in __instance.GetAllItems(type, ""))
                        {
                            if (item.m_itemData.m_shared.m_name.StartsWith("$item_"))
                            {
                                if (itemUnlimitedStackEnabledConfig.Value &&   item.m_itemData.m_shared.m_maxStackSize > 1)
                                {
                                    item.m_itemData.m_shared.m_maxStackSize = Int32.MaxValue;
                                };
                                if (itemWeightEnabledConfig.Value)
                                {
                                    item.m_itemData.m_shared.m_weight *= itemWeightConfig.Value;
                                }
                                
                            }
                        }
                    }
                }
            }
        }
        

        [HarmonyPatch(typeof(Ship), "Awake")]
        static class UpdateShipCargoSize
        {
            static void Postfix(Ship __instance)
            {
                if (shipKarveCargoIncreaseEnabledConfig.Value)
                {
                    if (__instance.name.ToLower().Contains("karve"))
                    {
                        Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                        if (container != null)
                        {

                            container.m_width = Math.Min(shipKarveCargoIncreaseColumnsConfig.Value, 6);
                            container.m_height = Math.Min(shipKarveCargoIncreaseRowsConfig.Value, 3);
                        }
                    }
                }

                if (shipvikingCargoIncreaseEnabledConfig.Value)
                {
                    if (__instance.name.ToLower().Contains("vikingship"))
                    {
                        Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                        if (container != null)
                        {

                            container.m_width = Math.Min(shipvikingCargoIncreaseColumnsConfig.Value, 8);
                            container.m_height = Math.Min(shipvikingCargoIncreaseRowsConfig.Value, 4);
                        }
                    }
                }

                if (shipCustomCargoIncreaseEnabledConfig.Value)
                {
                    Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                    if (container != null)
                    {

                        container.m_width = Math.Min(shipvikingCargoIncreaseColumnsConfig.Value, 8);
                        container.m_height = Math.Min(shipCustomCargoIncreaseRowsConfig.Value, 4);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
        static class HideMaxStackSizeInventoryGrid
        {
            static void Postfix(InventoryGrid __instance, Inventory ___m_inventory)
            {
                if (itemUnlimitedStackEnabledConfig.Value)
                {
                    int width = ___m_inventory.GetWidth();

                    foreach (ItemDrop.ItemData allItem in ___m_inventory.GetAllItems())
                    {
                        if (allItem.m_shared.m_maxStackSize > 1)
                        {
                            InventoryGrid.Element e = __instance.GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y,
                                width);
                            e.m_amount.text = allItem.m_stack.ToString();
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        static class HideMaxStackSizeHotkeyBar
        {
            //[HarmonyPriority(Priority.Last)]
            static void Postfix(HotkeyBar __instance, List<ItemDrop.ItemData> ___m_items,
                List<HotkeyBar.ElementData> ___m_elements)
            {
                if (itemUnlimitedStackEnabledConfig.Value)
                {
                    var player = Player.m_localPlayer;
                    if (player == null || player.IsDead() || __instance == null) return;
                    if (__instance.m_elements != null)
                    {
                        for (int j = 0; j < ___m_items.Count; j++)
                        {
                            ItemDrop.ItemData itemData = ___m_items[j];
                            try
                            {
                                HotkeyBar.ElementData elementData2 = ___m_elements[itemData.m_gridPos.x];
                                if (itemData.m_shared.m_maxStackSize > 1)
                                {
                                    elementData2.m_amount.text = itemData.m_stack.ToString();
                                }
                            }
                            catch
                            {
                                HotkeyBar.ElementData elementData2 = __instance.m_elements[itemData.m_gridPos.x - 5];
                                if (itemData.m_shared.m_maxStackSize > 1)
                                {
                                    elementData2.m_amount.text = itemData.m_stack.ToString();
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateContainerWeight")]
        static class DisplayContainerMaxWeight
        {
            static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
            {
                if (containerWeightLimitEnabledConfig.Value)
                {
                    if (___m_currentContainer == null || !__instance.m_currentContainer.transform.parent)
                        return;

                    float maxWeight;

                    /*
                    if (!__instance.m_currentContainer.transform.parent)
                    {
                        maxWeight = getContainerMaxWeight(___m_currentContainer.m_inventory.m_name);
                    }
                    else
                    {*/
                    maxWeight = getShipMaxWeight(___m_currentContainer.transform.parent.name);
                    //}

                    if (maxWeight > 0)
                    {
                        int totalWeight = Mathf.CeilToInt(___m_currentContainer.GetInventory().GetTotalWeight());

                        if (totalWeight > maxWeight && (double)Mathf.Sin(Time.time * 10f) > 0.0)
                            __instance.m_containerWeight.text = "<color=red>" + totalWeight.ToString() +
                                                                "</color>\n<color=white>" + maxWeight.ToString() +
                                                                "</color>";
                        else
                            __instance.m_containerWeight.text = "" + totalWeight.ToString() + "\n<color=white>" +
                                                                maxWeight.ToString() + "</color>";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateInventoryWeight")]
        static class DisplayPLayerMaxWeight
        {
            static void Postfix(InventoryGui __instance, Player player)
            {
                if (containerWeightLimitEnabledConfig.Value)
                {
                    float currentwight = (float)Math.Round(player.m_inventory.m_totalWeight);
                    float MaxCarryWeight = player.GetMaxCarryWeight();

                    if (currentwight > MaxCarryWeight && (double)Mathf.Sin(Time.time * 10f) > 0.0)
                        __instance.m_weight.text = "<color=red>" + currentwight.ToString() + "</color>\n<color=white>" +
                                                   MaxCarryWeight.ToString() + "</color>";
                    else
                        __instance.m_weight.text = "" + currentwight.ToString() + "\n<color=white>" +
                                                   MaxCarryWeight.ToString() + "</color>";
                }

                /*if (containerWeightLimitEnabledConfig.Value)
                {
                    float currentwight = (float)Math.Round(player.m_inventory.m_totalWeight);
                    float MaxCarryWeight = player.GetMaxCarryWeight();

                    if (currentwight > MaxCarryWeight && (double)Mathf.Sin(Time.time * 10f) > 0.0)
                        __instance.m_weight.text = "<color=red>" + currentwight.ToString() + "</color>\n<color=white>" +
                                                   MaxCarryWeight.ToString() + "</color>";
                    else
                        __instance.m_weight.text = "" + currentwight.ToString() + "\n<color=white>" +
                                                   MaxCarryWeight.ToString() + "</color>";
                }*/
            }
        }


        [HarmonyPatch(typeof(Ship), "FixedUpdate")]
        static class ApplyShipWeightForce
        {
            static void Postfix(Ship __instance, Rigidbody ___m_body)
            {
                // TODO: Add drag to ship if overweight
                if (containerWeightLimitEnabledConfig.Value)
                {

                    Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                    if (container != null)
                    {
                        float maxWeight = getShipMaxWeight(__instance.name);

                        float containerWeight = container.GetInventory().GetTotalWeight();

                        if (containerWeight > maxWeight)
                        {
                            float weightForce = (containerWeight - maxWeight) / maxWeight;
                            ___m_body.AddForceAtPosition(Vector3.down * weightForce * 5, ___m_body.worldCenterOfMass,
                                (ForceMode)2);
                        }
                    }
                }
            }
        }

        /*
        static float getContainerMaxWeight(string name)
        {
            switch (name)
            {
                case "$piece_chestwood":rn woodChestWeightLimitConfig.Value;
                case "$piece_chest":
                    return reinforcedChestWeightLimitConfig.Value;
                case "$piece_chestprivate":
                    return personalChestWeightLimitConfig.Value;
                case "$piece_chestblackmetal":
                    return blackMetalChestWeightLimitConfig.Value;
            }
            return CustomChestWeightLimitConfig.Value;
        }
        */


        static float getShipMaxWeight(string name)
        {
            if (name.ToLower().Contains("karve"))
            {
                return shipKarveCargoWeightLimitConfig.Value;
            }

            if (name.ToLower().Contains("vikingship"))
            {

                return shipvikingCargoWeightLimitConfig.Value;
            }

            // unlimited weight for custom ship mods
            return CustomShipWeightLimitConfig.Value;
        }


        private void OnDestroy()
        {
            Config.Save();
        }


        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                WeightBaseLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                WeightBaseLogger.LogError($"There was an issue loading your {ConfigFileName}");
                WeightBaseLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        private static ConfigEntry<bool> DebugLoggingConfig = null!;

        private static ConfigEntry<bool> itemUnlimitedStackEnabledConfig = null!;
        private static ConfigEntry<bool> itemWeightEnabledConfig = null!;
        private static ConfigEntry<float> itemWeightConfig = null!;

        private static ConfigEntry<bool> shipKarveCargoIncreaseEnabledConfig = null!;
        private static ConfigEntry<int> shipKarveCargoIncreaseColumnsConfig = null!;
        private static ConfigEntry<int> shipKarveCargoIncreaseRowsConfig = null!;

        private static ConfigEntry<bool> shipvikingCargoIncreaseEnabledConfig = null!;
        private static ConfigEntry<int> shipvikingCargoIncreaseColumnsConfig = null!;
        private static ConfigEntry<int> shipvikingCargoIncreaseRowsConfig = null!;

        private static ConfigEntry<bool> shipCustomCargoIncreaseEnabledConfig = null!;
        private static ConfigEntry<int> shipCustomCargoIncreaseColumnsConfig = null!;
        private static ConfigEntry<int> shipCustomCargoIncreaseRowsConfig = null!;

        private static ConfigEntry<bool> containerWeightLimitEnabledConfig = null!;
        private static ConfigEntry<int> shipKarveCargoWeightLimitConfig = null!;
        private static ConfigEntry<int> shipvikingCargoWeightLimitConfig = null!;
        private static ConfigEntry<int> CustomShipWeightLimitConfig = null!;

        /*
        private static ConfigEntry<int> woodChestWeightLimitConfig = null!;
        private static ConfigEntry<int> personalChestWeightLimitConfig = null!;
        private static ConfigEntry<int> reinforcedChestWeightLimitConfig = null!;
        private static ConfigEntry<int> blackMetalChestWeightLimitConfig = null!;
        private static ConfigEntry<int> CustomChestWeightLimitConfig = null!;
        */
        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion
    }
}