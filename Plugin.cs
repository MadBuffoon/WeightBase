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
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine.SceneManagement;
using WeightBase.Tools;

namespace WeightBase
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WeightBasePlugin : BaseUnityPlugin
    {
        internal const string ModName = "WeightBase";
        internal const string ModVersion = "1.0.3";
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

            /*DebugLoggingConfig = config("1 - General", "1.2 DeBug Logging", false,
                "This turns on console debug msgs.");
            _ = ConfigSync.AddConfigEntry(DebugLoggingConfig);*/

            _itemUnlimitedStackEnabledConfig = config("2 - Items", "2.1 Remove Stack Limit", true,
                "Should item stack size limit be removed? Will need to restart game/server!");
            _ = ConfigSync.AddConfigEntry(_itemUnlimitedStackEnabledConfig);
            _itemWeightEnabledConfig = config("2 - Items", "2.2 Weight Reduction", true,
                "Should item weight Reduction be enabled? Will need to restart game/server!");
            _ = ConfigSync.AddConfigEntry(_itemWeightEnabledConfig);
            _itemWeightConfig = config("2 - Items", "2.3 Item Weight", 1.0f,
                new ConfigDescription(
                    "How much an item weighs. 1 is normal weight and 2 being 2x the normal weight then 0.5 is half normal weight. ",
                    new AcceptableValueRange<float>(0f, 2f)));
            _ = ConfigSync.AddConfigEntry(_itemWeightConfig);
            
           _shipMassToWeightEnabledConfig = config("3 - Ship Weight", "3.1 Weight Matters", true,
                "Should weight in the cargo matter?");
            _ = ConfigSync.AddConfigEntry(_shipMassToWeightEnabledConfig);
            _shipMassScaleConfig = config("3 - Ship Weight", "3.2 Weight Capacity Scale", 2f,
                new ConfigDescription(
                    "This scales the total weight the ship can carry.",
                    new AcceptableValueRange<float>(1f, 20f)));
            _ = ConfigSync.AddConfigEntry(_shipMassScaleConfig);
            _shipMassWeightLookEnableConfig = config("3 - Ship Weight", "3.3 Got Weight?", false,
                "Should the ship show that it's over weight?");
            _ = ConfigSync.AddConfigEntry(_shipMassWeightLookEnableConfig);
            _shipMassSinkEnableConfig = config("3 - Ship Weight", "3.4 Sinking", false,
                "Should weight in the cargo sink your ship?");
            _ = ConfigSync.AddConfigEntry(_shipMassSinkEnableConfig);

            // 3 - ShipKarveCargoIncrease
            _shipKarveCargoIncreaseEnabledConfig = config("4 - Karve Ship", "3.1 Enabled", false,
                "Should Karve cargo hold size be increased?");
            _ = ConfigSync.AddConfigEntry(_shipKarveCargoIncreaseEnabledConfig);
            _shipKarveCargoIncreaseColumnsConfig = config("4 - Karve Ship",
                "4.2 INV Width/Colums", 2,
                new ConfigDescription("Number of columns for the Karve cargo hold.\nDefault 2.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            _ = ConfigSync.AddConfigEntry(_shipKarveCargoIncreaseColumnsConfig);
            _shipKarveCargoIncreaseRowsConfig = config("4 - Karve Ship",
                "4.3 INV Height/Rows", 2,
                new ConfigDescription("Number of rows for the Karve cargo hold.\nDefault 2.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            _ = ConfigSync.AddConfigEntry(_shipKarveCargoIncreaseRowsConfig);
            /*shipKarveCargoWeightLimitConfig =
                config("3 - Karve Ship", "3.4 Weight Limit", 1200, "Weight limit for the Karve");
            _ = ConfigSync.AddConfigEntry(shipKarveCargoWeightLimitConfig);*/

            // 4 - Viking
            _shipvikingCargoIncreaseEnabledConfig = config("5 - Long Ship", "5.1 Enabled", false,
                "Should viking cargo hold size be increased?");
            _ = ConfigSync.AddConfigEntry(_shipvikingCargoIncreaseEnabledConfig);
            _shipvikingCargoIncreaseColumnsConfig = config("5 - Long Ship",
                "5.2 INV Width/Colums", 6,
                new ConfigDescription("Number of columns for the Long cargo hold.\nDefault 6.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            _ = ConfigSync.AddConfigEntry(_shipvikingCargoIncreaseColumnsConfig);
            _shipvikingCargoIncreaseRowsConfig = config("5 - Long Ship",
                "5.3 INV Height/Rows", 3,
                new ConfigDescription("Number of rows for the viking cargo hold.\nDefault 3.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            _ = ConfigSync.AddConfigEntry(_shipvikingCargoIncreaseRowsConfig);
            /*shipvikingCargoWeightLimitConfig = config("4 - Viking Ship", "4.4 Weight Limit", 4200,
                "Weight limit for the Longship");
            _ = ConfigSync.AddConfigEntry(shipvikingCargoWeightLimitConfig);*/

            // 5 - Custom Ships
            _shipCustomCargoIncreaseEnabledConfig = config("6 - Custom Ship", "6.1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            _ = ConfigSync.AddConfigEntry(_shipCustomCargoIncreaseEnabledConfig);
            _shipCustomCargoIncreaseColumnsConfig = config("6 - Custom Ship",
                "6.2 INV Width/Colums", 5,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            _ = ConfigSync.AddConfigEntry(_shipCustomCargoIncreaseColumnsConfig);
            _shipCustomCargoIncreaseRowsConfig = config("6 - Custom Ship",
                "6.3 INV Height/Rows", 3,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            _ = ConfigSync.AddConfigEntry(_shipCustomCargoIncreaseRowsConfig);
            /*CustomShipWeightLimitConfig = config("5 - Custom Ship", "5.4 Weight Limit", 4200,
                "Weight limit for the Custom Ships");
            _ = ConfigSync.AddConfigEntry(CustomShipWeightLimitConfig);*/

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


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
        static class TotalWeightFix
        {
            static bool Prefix(Inventory __instance)
            {
                try
                {
                    float totalWeight = 0f;
                    foreach (ItemDrop.ItemData itemData in __instance.m_inventory)
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

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPriority(Priority.Last + 1)] // To load last but not the last. This is to load after every mod to get the modded items.
        static class UpdateItemsLoad
        {
            static void Postfix(ObjectDB __instance)
            {
                UpdateItemDatabase(__instance);
            }
        }

        static void UpdateItemDatabase(ObjectDB __instance)
        {

            if (!_itemUnlimitedStackEnabledConfig.Value && !_itemWeightEnabledConfig.Value)
            {
                return;
            }

            foreach (GameObject gameObject in __instance.m_items)
            {
                ItemDrop item = gameObject.GetComponent<ItemDrop>();

                if (item == null || item.m_itemData == null || item.m_itemData.m_shared == null)
                {
                    continue;
                }

                var shared = item.m_itemData.m_shared;
                if (!ogItemCaches.ContainsKey(shared.m_name))
                {
                    ogItemCaches.Add(shared.m_name, new ItemCache(shared.m_name, shared.m_maxStackSize, shared.m_weight));
                }
                
                // Stack Size
                if (shared.m_maxStackSize > 1)
                {
                    shared.m_maxStackSize = _itemUnlimitedStackEnabledConfig.Value ? 1000000: ogItemCaches[shared.m_name].ItemStackOG;
                    /*if (itemUnlimitedStackEnabledConfig.Value)
                    {
                        shared.m_maxStackSize = Int32.MaxValue;
                    }
                    else
                    {
                        shared.m_maxStackSize = ogItemCaches[shared.m_name].ItemStackOG;
                    }*/
                }

                // Weight
                if (_itemWeightEnabledConfig.Value)
                {
                    shared.m_weight = ogItemCaches[shared.m_name].ItemWeightOG * _itemWeightConfig.Value;
                }
                else
                {
                    shared.m_weight = ogItemCaches[shared.m_name].ItemWeightOG;
                }
                
            }
        }

        [HarmonyPatch(typeof(Ship), "Awake")]
        static class UpdateShipCargoSize
        {
            static void Postfix(Ship __instance)
            {
                if (_shipKarveCargoIncreaseEnabledConfig.Value)
                {
                    if (__instance.name.ToLower().Contains("karve"))
                    {
                        Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                        if (container != null)
                        {
                            container.m_width = Math.Min(_shipKarveCargoIncreaseColumnsConfig.Value, 6);
                            container.m_height = Math.Min(_shipKarveCargoIncreaseRowsConfig.Value, 3);
                        }
                    }
                }

                if (_shipvikingCargoIncreaseEnabledConfig.Value)
                {
                    if (__instance.name.ToLower().Contains("vikingship"))
                    {
                        Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                        if (container != null)
                        {
                            container.m_width = Math.Min(_shipvikingCargoIncreaseColumnsConfig.Value, 8);
                            container.m_height = Math.Min(_shipvikingCargoIncreaseRowsConfig.Value, 4);
                        }
                    }
                }
                

                if (_shipCustomCargoIncreaseEnabledConfig.Value)
                {
                    Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                    if (container != null)
                    {
                        container.m_width = Math.Min(_shipvikingCargoIncreaseColumnsConfig.Value, 8);
                        container.m_height = Math.Min(_shipCustomCargoIncreaseRowsConfig.Value, 4);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
        static class HideMaxStackSizeInventoryGrid
        {
            static void Postfix(InventoryGrid __instance, Inventory ___m_inventory)
            {
                if (_itemUnlimitedStackEnabledConfig.Value)
                {
                    int width = ___m_inventory.GetWidth();

                    foreach (ItemDrop.ItemData allItem in ___m_inventory.GetAllItems())
                    {
                        if (allItem.m_shared.m_maxStackSize > 1)
                        {
                            InventoryGrid.Element e = __instance.GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y,
                                width);
                            e.m_amount.text = Helper.FormatNumberSimple(allItem.m_stack);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        static class HideMaxStackSizeHotkeyBar
        {
            //[HarmonyPriority(Priority.Last + 1)]
            static void Postfix(HotkeyBar __instance, List<ItemDrop.ItemData> ___m_items,
                List<HotkeyBar.ElementData> ___m_elements)
            {
                if (_itemUnlimitedStackEnabledConfig.Value)
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
                                    elementData2.m_amount.text = Helper.FormatNumberSimple(itemData.m_stack);
                                }
                            }
                            catch
                            {
                                HotkeyBar.ElementData elementData2 = __instance.m_elements[itemData.m_gridPos.x - 5];
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
        static class DisplayPLayerMaxWeight
        {
            static void Postfix(InventoryGui __instance, Player player)
            {
                
                float currentWeight = (float)Math.Round(player.m_inventory.m_totalWeight * 100f) / 100f;
                float MaxCarryWeight = player.GetMaxCarryWeight();

                if (currentWeight > MaxCarryWeight && (double)Mathf.Sin(Time.time * 10f) > 0.0)
                    __instance.m_weight.text = "<color=red>" + Helper.FormatNumberSimple(currentWeight) + "</color>\n<color=white>" +
                                               Helper.FormatNumberSimple(MaxCarryWeight) + "</color>";
                else
                    __instance.m_weight.text = "" + Helper.FormatNumberSimple(currentWeight) + "\n<color=white>" +
                                               Helper.FormatNumberSimple(MaxCarryWeight) + "</color>";
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "UpdateContainerWeight")]
        static class DisplayContainerMaxWeight
        {
            static void Postfix(InventoryGui __instance, Container ___m_currentContainer)
            {
                

                //if (___m_currentContainer == null || !___m_currentContainer.transform.parent) return;
                if (___m_currentContainer == null) return;
                int totalWeight = Mathf.CeilToInt(___m_currentContainer.GetInventory().GetTotalWeight());
                
                __instance.m_containerWeight.text = Helper.FormatNumberSimple(totalWeight);
                
                if (!_shipMassToWeightEnabledConfig.Value)
                {
                    return;
                }
                if (___m_currentContainer.m_rootObjectOverride == null || !___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>()) return; // Checks to make sure its a SHIP!
                
                    var shipID = ___m_currentContainer.m_nview.m_zdo.m_uid;
                    var shipMass = shipBaseMasses.ContainsKey(shipID);
                    var weightFacter = 0f;
                    if (shipMass)
                    {
                        float shipBaseMass = shipBaseMasses[shipID] * _shipMassScaleConfig.Value;
                        var currentShip = ___m_currentContainer.m_rootObjectOverride.gameObject.GetComponent<Ship>();
                        if (currentShip == null) return;
                        var playersTotalWeight = currentShip.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));

                        weightFacter = Mathf.Floor((totalWeight + playersTotalWeight) / shipBaseMass * 100f);
                    }
                    //}


                    try
                    {



                        if (weightFacter > 100f && (double)Mathf.Sin(Time.time * 10f) > 0.0)
                        {
                            __instance.m_containerWeight.text = "" + Helper.FormatNumberSimple(totalWeight).ToString() +
                                                                "\n<color=red>" + weightFacter.ToString() +
                                                                " %</color>";
                        }
                        else
                        {
                            __instance.m_containerWeight.text = "" + Helper.FormatNumberSimple(totalWeight).ToString() + "\n<color=white>" +
                                                                weightFacter.ToString() + " %</color>";
                        }
                    }
                    catch
                    {
                        __instance.m_containerWeight.text = "Failed";

                    }
                
            }
        }


        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        static class GetShipMass
        {
            static void Postfix(Ship __instance)
            {
                
                Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                if (!container) return;
                if (!container.m_nview) return;
                
                var shipID = container.m_nview.m_zdo.m_uid;
                if (!shipBaseMasses.ContainsKey(shipID))
                {
                    shipBaseMasses.Add(shipID, __instance.m_body.mass);
                }
                
            }
        }

        [HarmonyPatch(typeof(Ship), nameof(Ship.FixedUpdate))]
        static class ApplyShipWeightForce
        {
            static void Postfix(Ship __instance, Rigidbody ___m_body)
            {
                // TODO: Add drag to ship if overweight
                if (!_shipMassToWeightEnabledConfig.Value) return;
                
                
                
                if (!__instance.m_nview.IsValid()) return;

                Container container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                if (!container) return;
                
                var shipID = container.m_nview.m_zdo.m_uid;
                if (!shipBaseMasses.ContainsKey(shipID))
                {
                    shipBaseMasses.Add(shipID, __instance.m_body.mass);
                }

                float shipBaseMass = shipBaseMasses[shipID] * _shipMassScaleConfig.Value;
                float containerWeight = container.GetInventory().GetTotalWeight();
                float playersTotalWeight = __instance.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));


               /*float weightFacter = Mathf.Round( (containerWeight + playersTotalWeight) / shipBaseMass);
               if (weightFacter < 1)
               {
                   weightFacter = Helper.FlipNumber(Helper.NumberRange(weightFacter, 0f, 0.5f, 0f, 1f));
               }*/

               float weightFacter = shipBaseMass / (containerWeight + playersTotalWeight);
               if (weightFacter <= 1f)
               {
                   //Sail
                   __instance.m_sailForce *= weightFacter;

                   // Rudder
                  /*if (__instance.m_speed == Ship.Speed.Slow)
                  {
                      Vector3 position = __instance.transform.position + __instance.transform.forward * __instance.m_stearForceOffset;
                      float fixedDeltaTime = Time.fixedDeltaTime;
                      Vector3 zero = Vector3.zero;
                      zero += __instance.transform.forward * __instance.m_backwardForce *
                              (1f - Mathf.Abs(__instance.m_rudderValue * (1f - weightFacter)));
                      ___m_body.AddForceAtPosition((zero * -1f) * fixedDeltaTime, position, ForceMode.VelocityChange);
                  }*/

                  
                  if (__instance.m_speed == Ship.Speed.Back || __instance.m_speed == Ship.Speed.Slow)
                  {
                      Vector3 position = __instance.transform.position + __instance.transform.forward * __instance.m_stearForceOffset;
                      Vector3 zero = Vector3.zero;
                      float fixedDeltaTime = Time.fixedDeltaTime;
                      float num14 = __instance.m_speed == Ship.Speed.Back ? 1f : -1f;
                      zero += __instance.transform.forward * __instance.m_backwardForce *
                              (1f - Mathf.Abs(__instance.m_rudderValue * (1f - weightFacter)));
                      ___m_body.AddForceAtPosition((zero * num14) * fixedDeltaTime, position, ForceMode.VelocityChange);
                  }
                  
                  
                  // Makes the ship look like its got some weight
                  if (!_shipMassWeightLookEnableConfig.Value) return;
                  float weightPercent = ((containerWeight + playersTotalWeight) / shipBaseMass) - 1;
                  float weightForce = Mathf.Clamp(weightPercent,0.0f, 0.5f);
                  if (weightFacter <= 0.49f && _shipMassSinkEnableConfig.Value)
                  {
                      weightForce = 2f;
                  }
                  ___m_body.AddForceAtPosition(Vector3.down * weightForce, ___m_body.worldCenterOfMass, ForceMode.VelocityChange);
               }


                /*if (containerWeight > maxWeight)
                {
                    float weightForce = (containerWeight - maxWeight) / maxWeight;
                    //___m_body.AddForceAtPosition(Vector3.down * weightForce * 5, ___m_body.worldCenterOfMass, ForceMode.VelocityChange);

                }*/
            }
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

        private static ConfigEntry<bool> _itemUnlimitedStackEnabledConfig = null!;
        private static ConfigEntry<bool> _itemWeightEnabledConfig = null!;
        private static ConfigEntry<float> _itemWeightConfig = null!;

        private static ConfigEntry<bool> _shipKarveCargoIncreaseEnabledConfig = null!;
        private static ConfigEntry<int> _shipKarveCargoIncreaseColumnsConfig = null!;
        private static ConfigEntry<int> _shipKarveCargoIncreaseRowsConfig = null!;

        private static ConfigEntry<bool> _shipvikingCargoIncreaseEnabledConfig = null!;
        private static ConfigEntry<int> _shipvikingCargoIncreaseColumnsConfig = null!;
        private static ConfigEntry<int> _shipvikingCargoIncreaseRowsConfig = null!;

        private static ConfigEntry<bool> _shipCustomCargoIncreaseEnabledConfig = null!;
        private static ConfigEntry<int> _shipCustomCargoIncreaseColumnsConfig = null!;
        private static ConfigEntry<int> _shipCustomCargoIncreaseRowsConfig = null!;
        
       private static ConfigEntry<bool> _shipMassToWeightEnabledConfig = null!; // was containerWeightLimitEnabledConfig
       private static ConfigEntry<float> _shipMassScaleConfig = null!;
       private static ConfigEntry<bool> _shipMassWeightLookEnableConfig = null!;
       private static ConfigEntry<bool> _shipMassSinkEnableConfig = null!;
       

        private static Dictionary<string, ItemCache> ogItemCaches = new Dictionary<string, ItemCache>();
        private static Dictionary<ZDOID, float> shipBaseMasses = new Dictionary<ZDOID, float>();

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