using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NoSeaSickness
{
    public static class Constants
    {
        public const string modGUID = "org.exscape.NoSeaSickness";
    }

    public static class Configuration
    {
        public static bool isSailing = false;
        public static Vector3 lastCameraPos = Vector3.zero;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> maxDistanceConfig;
    }

    [BepInPlugin(Constants.modGUID, "No Sea Sickness", "0.1.0")]
    [BepInProcess("valheim.exe")]
    public class NoSeaSickness : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(Constants.modGUID);

        void Awake()
        {
            Configuration.modEnabled = Config.Bind("General", "Enable static camera", true, "Disallow vertical camera movement except when the mouse is moved. Takes effect immediately.");
            Configuration.maxDistanceConfig = Config.Bind("General", "Max camera distance while sailing", 20f, "Max camera distance while sailing. Game default 6, recommended 20 or higher. Takes effect immediately.");

            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        class GetCameraPosition_Patch
        {
            static void Postfix(ref Vector3 pos)
            {
                if (!Configuration.modEnabled.Value || !Configuration.isSailing)
                    return;

                if (Configuration.isSailing) 
                {
                    if (Math.Abs(Input.GetAxis("Mouse Y")) < 0.025 && Math.Abs(Input.GetAxis("Mouse ScrollWheel")) == 0)
                    {
                        // Looks like 0.05 is the smallest possible value we can get (according to docs and in practice), but better play it safe than to test for equality here.
                        // The mouse did not move -- so the camera shouldn't, either.
                        // However, if the user scrolled, let the position update.
                        pos.y = Configuration.lastCameraPos.y;

                        // TODO: CONTROLLER SUPPORT!!
                    }
                }

                Configuration.lastCameraPos = pos;
            }
        }

        // Increase the max zoom-out level to reduce the angular size of the boat bobbing
        [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
        class UpdateCamera_Patch
        {
            static void Prefix(ref float ___m_maxDistanceBoat)
            {
                ___m_maxDistanceBoat = Configuration.maxDistanceConfig.Value;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.StartShipControl))]
        class StartShipControl_Patch
        {
            static void Prefix(ShipControlls shipControl)
            {
                // Debug.Log("*** Static Sailing Camera: Started sailing ***");
                Configuration.isSailing = true;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.StopShipControl))]
        class StopShipControl_Patch
        {
            static void Prefix()
            {
                // Debug.Log("*** Static Sailing Camera: Stopped sailing ***");
                Configuration.isSailing = false;
            }
        }
        
    }
}
