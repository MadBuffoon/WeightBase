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

    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    private static class UpdateShipCargoSize
    {
        private static void Postfix(Ship __instance)
        {
            var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
            if (!container) return;
            if (!container.m_nview) return;
            var shipID = container.m_nview.m_zdo.m_uid;
            if (!shipBaseMasses.ContainsKey(shipID))
            {
                shipBaseMasses.Add(shipID, __instance.m_body.mass);
            }

            if (WeightBasePlugin.ShipKarveCargoIncreaseEnabledConfig.Value)
            {
                if (__instance.name.ToLower().Contains("karve"))
                {
                    container.m_width = Math.Min(WeightBasePlugin.ShipKarveCargoIncreaseColumnsConfig.Value, 8);
                    container.m_height = Math.Min(WeightBasePlugin.ShipKarveCargoIncreaseRowsConfig.Value, 4);
                }
            }

            if (WeightBasePlugin.ShipvikingCargoIncreaseEnabledConfig.Value)
            {
                if (__instance.name.ToLower().Contains("vikingship"))
                {
                    container.m_width = Math.Min(WeightBasePlugin.ShipvikingCargoIncreaseColumnsConfig.Value, 8);
                    container.m_height = Math.Min(WeightBasePlugin.ShipvikingCargoIncreaseRowsConfig.Value, 4);
                }
            }


            if (WeightBasePlugin.ShipCustomCargoIncreaseEnabledConfig.Value)
            {
                container.m_width = Math.Min(WeightBasePlugin.ShipCustomCargoIncreaseColumnsConfig.Value, 8);
                container.m_height = Math.Min(WeightBasePlugin.ShipCustomCargoIncreaseRowsConfig.Value, 4);
            }
        }
    }

    [HarmonyPatch(typeof(Ship), nameof(Ship.FixedUpdate))]
    private static class ApplyShipWeightForce
    {
        private static void Postfix(Ship __instance, Rigidbody ___m_body)
        {
            // TODO: Add drag to ship if overweight
            if (!WeightBasePlugin.ShipMassToWeightEnabledConfig.Value) return;


            if (!__instance.m_nview.IsValid()) return;

            var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
            if (!container) return;

            var shipID = container.m_nview.m_zdo.m_uid;
            if (!shipBaseMasses.ContainsKey(shipID))
            {
                shipBaseMasses.Add(shipID, __instance.m_body.mass);
            }

            var shipBaseMass = shipBaseMasses[shipID] * WeightBasePlugin.ShipMassScaleConfig.Value;
            container.m_inventory.UpdateTotalWeight();
            var containerWeight = container.GetInventory().GetTotalWeight();
            var playersTotalWeight =
                __instance.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));

            /*float weightFacter = Mathf.Round( (containerWeight + playersTotalWeight) / shipBaseMass);
            if (weightFacter < 1)
            {
                weightFacter = Helper.FlipNumber(Helper.NumberRange(weightFacter, 0f, 0.5f, 0f, 1f));
            }*/

            var weightFacter = Mathf.Floor((containerWeight + playersTotalWeight) / shipBaseMass * 100f) / 100f - 1f;
            if (weightFacter > 0f)
            {
                weightFacter *= 2f;
                if (weightFacter > 0.9f)
                {
                    weightFacter = 1f;
                    float rudderForce = __instance.m_rudderValue;
                }

                var fixedDeltaTime = Time.fixedDeltaTime;

                //Sail
                if (__instance.m_speed is Ship.Speed.Half or Ship.Speed.Full)
                {
                    Vector3 worldCenterOfMass = __instance.m_body.worldCenterOfMass;
                    Vector3 force = __instance.m_sailForce * -1.0f * weightFacter;
                    ___m_body.AddForceAtPosition(force,
                        worldCenterOfMass + __instance.transform.up * __instance.m_sailForceOffset,
                        ForceMode.VelocityChange);
                }

                // Rudder
                if (__instance.m_speed is Ship.Speed.Back or Ship.Speed.Slow)
                {
                    Transform transform = __instance.transform;
                    Vector3 position = transform.position + transform.forward * __instance.m_stearForceOffset;
                    Vector3 force = Vector3.zero;
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


                // Makes the ship look like its got some weight
                if (!WeightBasePlugin.ShipMassWeightLookEnableConfig.Value) return;
                var weightPercent = (containerWeight + playersTotalWeight) / shipBaseMass - 1;
                var weightForce = Mathf.Clamp(weightPercent, 0.0f, 0.5f);
                if (weightFacter >= 1.5f && WeightBasePlugin.ShipMassSinkEnableConfig.Value)
                {
                    weightForce = 2f;
                }

                ___m_body.AddForceAtPosition(Vector3.down * weightForce, ___m_body.worldCenterOfMass,
                    ForceMode.VelocityChange);
            }


            /*if (containerWeight > maxWeight)
            {
                float weightForce = (containerWeight - maxWeight) / maxWeight;
                //___m_body.AddForceAtPosition(Vector3.down * weightForce * 5, ___m_body.worldCenterOfMass, ForceMode.VelocityChange);

            }*/
        }
    }
}