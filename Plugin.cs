using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
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
    internal const string ModName = "WeightBase";
    internal const string ModVersion = "1.0.8";
    internal const string Author = "MadBuffoon";
    private const string ModGUID = Author + "." + ModName;
    private const string ConfigFileName = ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    internal static string ConnectionError = "";

    public static readonly ManualLogSource WeightBaseLogger =
        BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    private readonly Harmony _harmony = new(ModGUID);
    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Start()
    {
        // 1 - General
        ServerConfigLocked = config("01 - General", "1 Lock Configuration", Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(ServerConfigLocked);

        /*DebugLoggingConfig = config("1 - General", "1.2 DeBug Logging", false,
            "This turns on console debug msgs.");
        _ = ConfigSync.AddConfigEntry(DebugLoggingConfig);*/

        ItemUnlimitedStackEnabledConfig = config("02 - Items", "1 Remove Stack Limit", true,
            "Should item stack size limit be removed? Will need to restart game/server!");
        ItemUnlimitedStackEnabledConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        ItemWeightEnabledConfig = config("02 - Items", "2 Weight Reduction", true,
            "Should item weight Reduction be enabled? Will need to restart game/server!");
        ItemWeightEnabledConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        ItemWeightConfig = config("02 - Items", "3 Item Weight", 1.0f,
            new ConfigDescription(
                "How much an item weighs. 1 is normal weight and 2 being 2x the normal weight then 0.5 is half normal weight. ",
                new AcceptableValueRange<float>(0f, 2f)));
        ItemWeightConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();

        ItemIncludeListConfig = config("02 - Items", "4 Include List", "DragonEgg,CryptKey,Wishbone,",
            "Items to include that don't stack already.\nYou must add a comma at the end.\nExample: DragonEgg,CryptKey,Wishbone,");
        ItemIncludeListConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        ItemExcludeListConfig = config("02 - Items", "5 Exclude List", string.Empty,
            "Items to Exclude items from Stack/Weight Change.\nYou must add a comma at the end.\nExample: DragonEgg,CryptKey,Wishbone,");
        ItemExcludeListConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();
        ItemNoWeightListConfig = config("02 - Items", "6 No Weight List", "Coins,",
            "Items to have the stack change but have no weight.\nYou must add a comma at the end.\nExample: DragonEgg,CryptKey,Wishbone,");
        ItemNoWeightListConfig.SettingChanged += (_, _) => _ItemUpdateChange_SettingChange();

        ShipMassToWeightEnabledConfig = config("03 - Ship Weight", "1 Weight Matters", true,
            "Should weight in the cargo matter?");
        ShipMassScaleConfig = config("03 - Ship Weight", "2 Weight Capacity Scale", 2f,
            new ConfigDescription(
                "This scales the total weight the ship can carry.",
                new AcceptableValueRange<float>(1f, 20f)));
        ShipMassWeightLookEnableConfig = config("03 - Ship Weight", "3 Got Weight?", false,
            "Should the ship show that it's over weight?");
        ShipMassSinkEnableConfig = config("03 - Ship Weight", "4 Sinking", false,
            "Should weight in the cargo sink your ship?");

        // 3 - ShipKarveCargoIncrease
        KarveCargoIncreaseEnabledConfig = config("04 - Karve Ship", "1 Enabled", false,
            "Should Karve cargo hold size be increased?");
        KarveCargoIncreaseColumnsConfig = config("04 - Karve Ship",
            "2 INV Width/Colums", 2,
            new ConfigDescription("Number of columns for the Karve cargo hold.\nDefault 2.",
                new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
        KarveCargoIncreaseRowsConfig = config("04 - Karve Ship",
            "3 INV Height/Rows", 2,
            new ConfigDescription("Number of rows for the Karve cargo hold.\nDefault 2.",
                new AcceptableValueList<int>(1, 2, 3, 4)));

        // 4 - Viking
        vikingCargoIncreaseEnabledConfig = config("05 - Long Ship", "1 Enabled", false,
            "Should viking cargo hold size be increased?");
        vikingCargoIncreaseColumnsConfig = config("05 - Long Ship",
            "2 INV Width/Colums", 6,
            new ConfigDescription("Number of columns for the Long cargo hold.\nDefault 6.",
                new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
        vikingCargoIncreaseRowsConfig = config("05 - Long Ship",
            "3 INV Height/Rows", 3,
            new ConfigDescription("Number of rows for the viking cargo hold.\nDefault 3.",
                new AcceptableValueList<int>(1, 2, 3, 4)));

        // 99 - Other Ships
        string name99 = "99 - Other Ships";
        ShipCustomCargoIncreaseEnabledConfig = config(name99, "1 Enabled", false,
            "Should Custom cargo hold size be increased?");
        ShipCustomCargoIncreaseColumnsConfig = config(name99,
            "2 INV Width/Colums", 5,
            new ConfigDescription("Number of columns for the Custom cargo hold.",
                new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
        ShipCustomCargoIncreaseRowsConfig = config(name99,
            "3 INV Height/Rows", 3,
            new ConfigDescription("Number of rows for the Custom cargo hold.",
                new AcceptableValueList<int>(1, 2, 3, 4)));
        
        if (Chainloader.PluginInfos.ContainsKey("marlthon.OdinShip"))
        {
            //WeightBaseLogger.LogWarning("Loaded");
            // CargoShip Ships
            string name0 = "10 - CargoShip";
            CargoShipCargoIncreaseEnabledConfig = config(name0, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            CargoShipCargoIncreaseColumnsConfig = config(name0,
                "2 INV Width/Colums", 8,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            CargoShipCargoIncreaseRowsConfig = config(name0,
                "3 INV Height/Rows", 4,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));

            // Skuldelev Ships
            string Name1 = "11 - Skuldelev";
            SkuldelevCargoIncreaseEnabledConfig = config(Name1, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            SkuldelevCargoIncreaseColumnsConfig = config(Name1,
                "2 INV Width/Colums", 7,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            SkuldelevCargoIncreaseRowsConfig = config(Name1,
                "3 INV Height/Rows", 4,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
            
            // LittleBoat Ships
            string Name2 = "12 - LittleBoat";
            LittleBoatCargoIncreaseEnabledConfig = config(Name2, "1 Enabled", false,
                    "Should Custom cargo hold size be increased?");
            LittleBoatCargoIncreaseColumnsConfig = config(Name2,
                "2 INV Width/Colums", 2,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            LittleBoatCargoIncreaseRowsConfig = config(Name2,
                "3 INV Height/Rows", 2,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));

            // MercantShip Ships
            string Name3 = "13 - MercantShip";
            MercantShipCargoIncreaseEnabledConfig = config(Name3, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            MercantShipCargoIncreaseColumnsConfig = config(Name3,
                "2 INV Width/Colums", 6,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            MercantShipCargoIncreaseRowsConfig = config(Name3,
                "3 INV Height/Rows", 3,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));

            // BigCargoShip Ships
            string Name4 = "14 - BigCargoShip";
            BigCargoShipCargoIncreaseEnabledConfig = config(Name4, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            BigCargoShipCargoIncreaseColumnsConfig = config(Name4,
                "2 INV Width/Colums", 8,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            BigCargoShipCargoIncreaseRowsConfig = config(Name4,
                "3 INV Height/Rows", 4,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));

            // FishingBoat Ships
            string Name5 = "15 - FishingBoat";
            FishingBoatCargoIncreaseEnabledConfig = config(Name5, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            FishingBoatCargoIncreaseColumnsConfig = config(Name5,
                "2 INV Width/Colums", 2,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            FishingBoatCargoIncreaseRowsConfig = config(Name5,
                "3 INV Height/Rows", 2,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));

            // FishingCanoe Ships
            string Name6 = "16 - FishingCanoe";
            FishingCanoeCargoIncreaseEnabledConfig = config(Name6, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            FishingCanoeCargoIncreaseColumnsConfig = config(Name6,
                "2 INV Width/Colums", 2,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            FishingCanoeCargoIncreaseRowsConfig = config(Name6,
                "3 INV Height/Rows", 1,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));

            // WarShip Ships
            string Name7 = "17 - WarShip";
            WarShipCargoIncreaseEnabledConfig = config(Name7, "1 Enabled", false,
                "Should Custom cargo hold size be increased?");
            WarShipCargoIncreaseColumnsConfig = config(Name7,
                "2 INV Width/Colums", 6,
                new ConfigDescription("Number of columns for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4, 5, 6, 7, 8)));
            WarShipCargoIncreaseRowsConfig = config(Name7,
                "3 INV Height/Rows", 4,
                new ConfigDescription("Number of rows for the Custom cargo hold.",
                    new AcceptableValueList<int>(1, 2, 3, 4)));
        }
        // End of Config Settings


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

    internal static ConfigEntry<Toggle> ServerConfigLocked = null!;

    internal static ConfigEntry<bool> ItemUnlimitedStackEnabledConfig = null!;
    internal static ConfigEntry<bool> ItemWeightEnabledConfig = null!;
    internal static ConfigEntry<float> ItemWeightConfig = null!;
    internal static ConfigEntry<string> ItemIncludeListConfig = null!;
    internal static ConfigEntry<string> ItemExcludeListConfig = null!;
    internal static ConfigEntry<string> ItemNoWeightListConfig = null!;
    

    internal static ConfigEntry<bool> ShipMassToWeightEnabledConfig = null!;
    internal static ConfigEntry<float> ShipMassScaleConfig = null!;
    internal static ConfigEntry<bool> ShipMassWeightLookEnableConfig = null!;
    internal static ConfigEntry<bool> ShipMassSinkEnableConfig = null!;

    internal static ConfigEntry<bool> KarveCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> KarveCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> KarveCargoIncreaseRowsConfig = null!;

    internal static ConfigEntry<bool> vikingCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> vikingCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> vikingCargoIncreaseRowsConfig = null!;
    
    internal static ConfigEntry<bool> ShipCustomCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> ShipCustomCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> ShipCustomCargoIncreaseRowsConfig = null!;
    
    
    internal static ConfigEntry<bool> CargoShipCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> CargoShipCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> CargoShipCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> SkuldelevCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> SkuldelevCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> SkuldelevCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> LittleBoatCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> LittleBoatCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> LittleBoatCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> MercantShipCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> MercantShipCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> MercantShipCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> BigCargoShipCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> BigCargoShipCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> BigCargoShipCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> FishingBoatCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> FishingBoatCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> FishingBoatCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> FishingCanoeCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> FishingCanoeCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> FishingCanoeCargoIncreaseRowsConfig = null!;
        
    internal static ConfigEntry<bool> WarShipCargoIncreaseEnabledConfig = null!;
    internal static ConfigEntry<int> WarShipCargoIncreaseColumnsConfig = null!;
    internal static ConfigEntry<int> WarShipCargoIncreaseRowsConfig = null!;



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