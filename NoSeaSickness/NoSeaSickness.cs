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
        public static bool doInitialCameraSetup = true;
        public static Vector3 lastCameraPos = Vector3.zero;

        public static bool userInput = false; // Did the user move the camera?

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> maxDistanceConfig;
    }

    [BepInPlugin(Constants.modGUID, "No Sea Sickness", "0.1.2")]
    [BepInProcess("valheim.exe")]
    public class NoSeaSickness : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(Constants.modGUID);

        void Awake()
        {
            Configuration.modEnabled = Config.Bind("General", "Enable static camera", true, "Disallow vertical camera movement except when the mouse/controller is moved. Takes effect immediately.");
            Configuration.maxDistanceConfig = Config.Bind("General", "Max camera distance while sailing", 20f, "Max camera distance while sailing. Game default 6, recommended 20 or higher. Takes effect immediately.");

            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        class GetCameraPosition_Patch
        {
            static void Postfix(float dt, ref Vector3 pos)
            {
                // pos here always refers to the game's preferred position, i.e. one that bobs up and down while sailing.
                // It's always created from scratch prior to this Postfix getting called, so if we changed it the last frame
                // that now has no meaning; it will jump back to the game's preferred position anyway.

                if (!Configuration.modEnabled.Value || !Configuration.isSailing)
                    return;

                if (Configuration.isSailing)
                {
                    if (Configuration.doInitialCameraSetup)
                    {
                        // Fix camera getting stuck in the wrong position until player input
                        Configuration.lastCameraPos = pos;
                        Configuration.doInitialCameraSetup = false;
                        return;
                    }

                    if (!Configuration.userInput)
                    {
                        // The mouse/controller did not move -- so the camera shouldn't, either.
                        pos.y = Configuration.lastCameraPos.y;
                    }
                }

                Configuration.lastCameraPos = pos;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
        class SetMouseLook_Patch
        {
            static bool Prefix(Vector2 mouseLook)
            {
                // Despite the name "SetMouseLook", this also accounts for controller input (the caller adds them together and sends them here).
                Configuration.userInput = mouseLook.y != 0 || Input.GetAxis("Mouse ScrollWheel") != 0f;
                return true;
            }
        }

        // Increase the max zoom-out level to reduce the angular size of the ship bobbing
        [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
        class UpdateCamera_Patch
        {
            static void Postfix(ref float ___m_maxDistanceBoat)
            {
                ___m_maxDistanceBoat = Configuration.maxDistanceConfig.Value;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.StartShipControl))]
        class StartShipControl_Patch
        {
            static void Postfix(ShipControlls shipControl)
            {
                // Debug.Log("*** Static Sailing Camera: Started sailing ***");
                Configuration.isSailing = true;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.StopShipControl))]
        class StopShipControl_Patch
        {
            static void Postfix()
            {
                // Debug.Log("*** Static Sailing Camera: Stopped sailing ***");
                Configuration.isSailing = false;
            }
        }
        
    }
}
