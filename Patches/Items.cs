using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WeightBase.Tools;

namespace WeightBase.Patches
{
    public class Items
    {
        internal static readonly Dictionary<string, ItemCache> OgItemCaches = new();

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPriority(Priority.Last + 1)]
        private static class UpdateItemsLoad
        {
            private static void Postfix(ObjectDB __instance)
            {
                WeightBasePlugin.WeightBaseLogger.LogDebug("UpdateItemsLoad Awaked");
                Util.UpdateItemDatabase(__instance);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenRespons))]
        internal static class UpdateItemsContainer
        {
            private static void Postfix(Container __instance)
            {
                Util.UpdateContainerItems(__instance);
                __instance.m_inventory.UpdateTotalWeight();
                Util.UpdateContainerWeightInGui();
            }
        }
    }
}