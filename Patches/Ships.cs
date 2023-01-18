using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace WeightBase.Patches;

public class Ships
{
    
    internal static readonly Dictionary<ZDOID, float> shipBaseMasses = new();

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
            var pLayerTotalWeight=Player.m_localPlayer.m_inventory.GetTotalWeight();
            __instance.GetComponentInChildren<Container>().m_inventory.m_totalWeight -= pLayerTotalWeight;
        }
    }
    
    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    private static class UpdateShipCargoSize
    {
        private static void Postfix(Ship __instance)
        {
            if (WeightBasePlugin._shipKarveCargoIncreaseEnabledConfig.Value)
            {
                if (__instance.name.ToLower().Contains("karve"))
                {
                    var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                    if (container != null)
                    {
                        container.m_width = Math.Min(WeightBasePlugin._shipKarveCargoIncreaseColumnsConfig.Value, 6);
                        container.m_height = Math.Min(WeightBasePlugin._shipKarveCargoIncreaseRowsConfig.Value, 3);
                    }
                }
            }

            if (WeightBasePlugin._shipvikingCargoIncreaseEnabledConfig.Value)
            {
                if (__instance.name.ToLower().Contains("vikingship"))
                {
                    var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                    if (container != null)
                    {
                        container.m_width = Math.Min(WeightBasePlugin._shipvikingCargoIncreaseColumnsConfig.Value, 8);
                        container.m_height = Math.Min(WeightBasePlugin._shipvikingCargoIncreaseRowsConfig.Value, 4);
                    }
                }
            }


            if (WeightBasePlugin._shipCustomCargoIncreaseEnabledConfig.Value)
            {
                var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
                if (container != null)
                {
                    container.m_width = Math.Min(WeightBasePlugin._shipCustomCargoIncreaseColumnsConfig.Value, 8);
                    container.m_height = Math.Min(WeightBasePlugin._shipCustomCargoIncreaseRowsConfig.Value, 4);
                }
            }
        }
    }  
    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    private static class GetShipMass
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
        }
    }
        [HarmonyPatch(typeof(Ship), nameof(Ship.FixedUpdate))]
    private static class ApplyShipWeightForce
    {
        private static void Postfix(Ship __instance, Rigidbody ___m_body)
        {
            // TODO: Add drag to ship if overweight
            if (!WeightBasePlugin._shipMassToWeightEnabledConfig.Value) return;


            if (!__instance.m_nview.IsValid()) return;

            var container = __instance.gameObject.transform.GetComponentInChildren<Container>();
            if (!container) return;

            var shipID = container.m_nview.m_zdo.m_uid;
            if (!shipBaseMasses.ContainsKey(shipID))
            {
                shipBaseMasses.Add(shipID, __instance.m_body.mass);
            }

            var shipBaseMass = shipBaseMasses[shipID] * WeightBasePlugin._shipMassScaleConfig.Value;
            var containerWeight = container.GetInventory().GetTotalWeight();
            var playersTotalWeight =
                __instance.m_players.Sum(player => (float)Math.Round(player.m_inventory.m_totalWeight));


            /*float weightFacter = Mathf.Round( (containerWeight + playersTotalWeight) / shipBaseMass);
            if (weightFacter < 1)
            {
                weightFacter = Helper.FlipNumber(Helper.NumberRange(weightFacter, 0f, 0.5f, 0f, 1f));
            }*/

            var weightFacter = (Mathf.Floor((containerWeight + playersTotalWeight) / shipBaseMass * 100f) / 100f) - 1f;
            if (weightFacter > 0f)
            {
                weightFacter *= 2f;
                if (weightFacter > 0.9f) weightFacter = 1f;
                
                var fixedDeltaTime = Time.fixedDeltaTime;
                //Sail
                //__instance.m_sailForce *= weightFacter;
                /*if (__instance.m_speed == Ship.Speed.Half || __instance.m_speed == Ship.Speed.Full)
                {
                    Vector3 worldCenterOfMass = __instance.m_body.worldCenterOfMass;
                    float sailSize = 0.0f;
                    if (__instance.m_speed == Ship.Speed.Full)
                        sailSize = 1f;
                    else if (__instance.m_speed == Ship.Speed.Half)
                        sailSize = 0.5f;
                    var force = __instance.GetSailForce((sailSize * shipSpeed), fixedDeltaTime);
                    ___m_body.AddForceAtPosition(force * -1.0f,
                        worldCenterOfMass + __instance.transform.up * __instance.m_sailForceOffset,
                        ForceMode.VelocityChange);
                }*/
                
                if (__instance.m_speed == Ship.Speed.Half || __instance.m_speed == Ship.Speed.Full)
                {
                    Vector3 worldCenterOfMass = __instance.m_body.worldCenterOfMass;
                    var force = (__instance.m_sailForce * -1.0f) * weightFacter;
                    ___m_body.AddForceAtPosition( force,
                        worldCenterOfMass + __instance.transform.up * __instance.m_sailForceOffset,
                        ForceMode.VelocityChange);
                }
                // Rudder
           if (__instance.m_speed == Ship.Speed.Back || __instance.m_speed == Ship.Speed.Slow)
                {
                    var position = __instance.transform.position +
                                   __instance.transform.forward * __instance.m_stearForceOffset;
                    var zero = Vector3.zero;
                    var num14 = __instance.m_speed == Ship.Speed.Back ? 1f : -1f;
                    zero += __instance.transform.forward * __instance.m_backwardForce *
                            (__instance.m_rudderValue * num14) * weightFacter;
                    ___m_body.AddForceAtPosition(zero * fixedDeltaTime, position, ForceMode.VelocityChange);
                }


                // Makes the ship look like its got some weight
                if (!WeightBasePlugin._shipMassWeightLookEnableConfig.Value) return;
                var weightPercent = (containerWeight + playersTotalWeight) / shipBaseMass - 1;
                var weightForce = Mathf.Clamp(weightPercent, 0.0f, 0.5f);
                if (weightFacter >= 1.5f && WeightBasePlugin._shipMassSinkEnableConfig.Value)
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