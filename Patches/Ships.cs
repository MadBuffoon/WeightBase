using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace WeightBase.Patches;

public class Ships
{
    internal static readonly Dictionary<ZDOID, float> shipBaseMasses = new();
    /*
       [HarmonyPatch(typeof(Ship), nameof(Ship.OnTriggerEnter))]
       private static class ShipWeightAdd
       {
           private static void Postfix(Ship __instance, Collider collider)
           {
   
               Player component = collider.GetComponent<Player>();
               if (!(bool) (UnityEngine.Object) component) return;
               var pLayerTotalWeight=Player.m_localPlayer.m_inventory.GetTotalWeight();
               __instance.GetComponentInChildren<Container>().m_inventory.m_totalWeight += pLayerTotalWeight;
   
               
           }
       }
      
       [HarmonyPatch(typeof(Ship), nameof(Ship.OnTriggerExit))]
       private static class ShipWeightRemove
       {
           private static void Postfix(Ship __instance, Collider collider)
           {
               Player component = collider.GetComponent<Player>();
               if (!(bool) (UnityEngine.Object) component) return;
               float pLayerTotalWeight=Player.m_localPlayer.m_inventory.GetTotalWeight();
               __instance.GetComponentInChildren<Container>().m_inventory.m_totalWeight -= pLayerTotalWeight;
           }
       }*/


    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    private static class ApplyShipWeightForce
    {
        private static void Postfix(Ship __instance, Rigidbody ___m_body)
        {
            if (!WeightBasePlugin.ShipMassToWeightEnabledConfig.Value || !__instance.HasPlayerOnboard() || !__instance.m_nview.IsValid())
                return;

            var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
            if (container == null)
                return;

            var shipID = container.m_nview.m_zdo.m_uid;

            if (!shipBaseMasses.ContainsKey(shipID))
            {
                shipBaseMasses.Add(shipID, __instance.m_body.mass);
            }

            var shipBaseMass = shipBaseMasses[shipID] * WeightBasePlugin.ShipMassScaleConfig.Value;
            container.m_inventory.UpdateTotalWeight();
            var containerWeight = container.GetInventory().GetTotalWeight();
            var playersTotalWeight = __instance.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));

            var weightFacter = Mathf.Floor((containerWeight + playersTotalWeight) / shipBaseMass * 100f) / 100f - 1f;

            if (weightFacter > 0f)
            {
                Util.ApplyWeightFactor(__instance, ___m_body, weightFacter, containerWeight, playersTotalWeight, shipBaseMass);
            }
        }
    }
}