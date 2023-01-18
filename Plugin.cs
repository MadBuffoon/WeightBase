using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using WeightBase.Patches;
using WeightBase.Tools;

namespace WeightBase;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class WeightBasePlugin : BaseUnityPlugin
{
    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    internal const string ModName = "WeightBase";
    internal const string ModVersion = "1.0.4";
    internal const string Author = "MadBuffoon";
    private const string ModGUID = Author + "." + ModName;
    private static readonly string ConfigFileName = ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    internal static string ConnectionError = "";

    public static readonly ManualLogSource WeightBaseLogger =
        BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static ManualLogSource logger;

    private readonly Harmony _harmony = new(ModGUID);


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
        _itemUnlimitedStackEnabledConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        _itemWeightEnabledConfig = config("2 - Items", "2.2 Weight Reduction", true,
            "Should item weight Reduction be enabled? Will need to restart game/server!");
        _itemWeightEnabledConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        _itemWeightConfig = config("2 - Items", "2.3 Item Weight", 1.0f,
            new ConfigDescription(
                "How much an item weighs. 1 is normal weight and 2 being 2x the normal weight then 0.5 is half normal weight. ",
                new AcceptableValueRange<float>(0f, 2f)));
        _itemWeightConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();

        _itemIncludeListConfig = config("2 - Items", "2.4 Include List", "DragonEgg,CryptKey,Wishbone,",
            "Items to include that don't stack already.\nYou must add a comma at the end.\nExample: DragonEgg,CryptKey,Wishbone,");
        _itemIncludeListConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        _itemExcludeListConfig = config("2 - Items", "2.5 Exclude List", string.Empty,
            "Items to Exclude items from Stack/Weight Change.\nYou must add a comma at the end.\nExample: DragonEgg,CryptKey,Wishbone,");
        _itemExcludeListConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();

        _shipMassToWeightEnabledConfig = config("3 - Ship Weight", "3.1 Weight Matters", true,
            "Should weight in the cargo matter?");
        _shipMassScaleConfig = config("3 - Ship Weight", "3.2 Weight Capacity Scale", 2f,
            new ConfigDescription(
                "This scales the total weight the ship can carry.",
                new AcceptableValueRange<float>(1f, 20f)));
        _shipMassWeightLookEnableConfig = config("3 - Ship Weight", "3.3 Got Weight?", false,
            "Should the ship show that it's over weight?");
        _shipMassSinkEnableConfig = config("3 - Ship Weight", "3.4 Sinking", false,
            "Should weight in the cargo sink your ship?");

        // 3 - ShipKarveCargoIncrease
        _shipKarveCargoIncreaseEnabledConfig = config("4 - Karve Ship", "3.1 Enabled", false,
            "Should Karve cargo hold size be increased?");
        _shipKarveCargoIncreaseColumnsConfig = config("4 - Karve Ship",
            "4.2 INV Width/Colums", 2,
            new ConfigDescription("Number of columns for the Karve cargo hold.\nDefault 2.",
                new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
        _shipKarveCargoIncreaseRowsConfig = config("4 - Karve Ship",
            "4.3 INV Height/Rows", 2,
            new ConfigDescription("Number of rows for the Karve cargo hold.\nDefault 2.",
                new AcceptableValueList<int>(1, 2, 3, 4)));
        /*shipKarveCargoWeightLimitConfig =
            config("3 - Karve Ship", "3.4 Weight Limit", 1200, "Weight limit for the Karve");
        _ = ConfigSync.AddConfigEntry(shipKarveCargoWeightLimitConfig);*/

        // 4 - Viking
        _shipvikingCargoIncreaseEnabledConfig = config("5 - Long Ship", "5.1 Enabled", false,
            "Should viking cargo hold size be increased?");
        _shipvikingCargoIncreaseColumnsConfig = config("5 - Long Ship",
            "5.2 INV Width/Colums", 6,
            new ConfigDescription("Number of columns for the Long cargo hold.\nDefault 6.",
                new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
        _shipvikingCargoIncreaseRowsConfig = config("5 - Long Ship",
            "5.3 INV Height/Rows", 3,
            new ConfigDescription("Number of rows for the viking cargo hold.\nDefault 3.",
                new AcceptableValueList<int>(1, 2, 3, 4)));
        /*shipvikingCargoWeightLimitConfig = config("4 - Viking Ship", "4.4 Weight Limit", 4200,
            "Weight limit for the Longship");
        _ = ConfigSync.AddConfigEntry(shipvikingCargoWeightLimitConfig);*/

        // 5 - Custom Ships
        _shipCustomCargoIncreaseEnabledConfig = config("6 - Custom Ship", "6.1 Enabled", false,
            "Should Custom cargo hold size be increased?");
        _shipCustomCargoIncreaseColumnsConfig = config("6 - Custom Ship",
            "6.2 INV Width/Colums", 5,
            new ConfigDescription("Number of columns for the Custom cargo hold.",
                new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
        _shipCustomCargoIncreaseRowsConfig = config("6 - Custom Ship",
            "6.3 INV Height/Rows", 3,
            new ConfigDescription("Number of rows for the Custom cargo hold.",
                new AcceptableValueList<int>(1, 2, 3, 4)));

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


        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();
        

    }

    private void OnDestroy()
    {
        Config.Save();
    }




    private void _ItemUpdateChange_SettingChange()
    {
        if (!ObjectDB.instance) return;
        WeightBaseLogger.LogInfo("_ItemUpdateChange_SettingChange Joined");
       Items.UpdateItemDatabase(ObjectDB.instance);
        var itemDrops= Resources.FindObjectsOfTypeAll<ItemDrop>();
        foreach (var item in itemDrops)
        {
            
            var nameOfItem = Utils.GetPrefabName(item.gameObject) + ",";
            Items.UpdateItem(item.m_itemData, nameOfItem);
            
        }
        
        if (!Player.m_localPlayer) return;
        foreach (var i in Player.m_localPlayer.m_inventory.m_inventory)
        {
            var nameOfItem = Utils.GetPrefabName(i.m_dropPrefab) + ",";
            Items. UpdateItem(i, nameOfItem);
        }
        Player.m_localPlayer.m_inventory.UpdateTotalWeight();
        
        if (InventoryGui.instance == null) return;
        InventoryGui.instance.UpdateInventoryWeight(Player.m_localPlayer);
        if (!InventoryGui.instance.m_currentContainer) return;
        foreach (var i in InventoryGui.instance.m_currentContainer.m_inventory.m_inventory)
        {
            var nameOfItem = Utils.GetPrefabName(i.m_dropPrefab) + ",";
            Items.UpdateItem(i, nameOfItem);
        }
        InventoryGui.instance.m_currentContainer.m_inventory.UpdateTotalWeight();
        InventoryGui.instance.UpdateContainerWeight();
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

    internal static ConfigEntry<Toggle> _serverConfigLocked = null!;

    internal static ConfigEntry<bool> _itemUnlimitedStackEnabledConfig = null!;
    internal static ConfigEntry<bool> _itemWeightEnabledConfig = null!;
    internal static ConfigEntry<float> _itemWeightConfig = null!;
    internal static ConfigEntry<string> _itemIncludeListConfig = null!;
    internal static ConfigEntry<string> _itemExcludeListConfig = null!;

    internal static ConfigEntry<bool> _shipKarveCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> _shipKarveCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> _shipKarveCargoIncreaseRowsConfig = null!;

    internal static ConfigEntry<bool> _shipvikingCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> _shipvikingCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> _shipvikingCargoIncreaseRowsConfig = null!;

    internal static ConfigEntry<bool> _shipCustomCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> _shipCustomCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> _shipCustomCargoIncreaseRowsConfig = null!;

    internal static ConfigEntry<bool> _shipMassToWeightEnabledConfig = null!; // was containerWeightLimitEnabledConfig
    internal static ConfigEntry<float> _shipMassScaleConfig = null!;
    internal static ConfigEntry<bool> _shipMassWeightLookEnableConfig = null!;
    internal static ConfigEntry<bool> _shipMassSinkEnableConfig = null!;



    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        var configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
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

    private class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value)
        {
            return value;
        }

        public override bool IsValid(object value)
        {
            return true;
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }
    }

    #endregion
}