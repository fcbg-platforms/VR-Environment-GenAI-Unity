using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;


namespace AiWorldGeneration.VR
{
    /// <summary>
    /// Activates a desktop VR Cheater when no headset is connect. 
    /// </summary>
    public class XRDesktopActivation : MonoBehaviour
    {

        [SerializeField, Tooltip("XR device simulator that mimics a VR headset")]
        GameObject xrDeviceSimulator;

        /// <summary>
        /// Detects whether we should start on desktop mode as no VR device is connected.
        /// </summary>
        /// <returns></returns>
        bool DesktopPlay()
        {
            // Start by checking if an XR equipement is connected
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings == null)
            {
                Debug.Log($"XRGeneralSettings is null.");
                return true;
            }

            var xrManager = xrSettings.Manager;
            if (xrManager == null)
            {
                Debug.Log($"XRManagerSettings is null.");
                return true;
            }

            var xrLoader = xrManager.activeLoader;
            if (xrLoader == null)
            {
                Debug.Log($"XRLoader is null.");
                return true;
            }

            Debug.Log($"Loaded XR Device: {xrLoader.name}");

            // An XR device is connected, now check if it is feature-complete
            var xrDisplay = xrLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            Debug.Log($"XRDisplay: {xrDisplay != null}");

            if (xrDisplay != null && xrDisplay.TryGetDisplayRefreshRate(out float refreshRate))
            {
                Debug.Log($"Refresh Rate: {refreshRate}hz");
            }

            var xrInput = xrLoader.GetLoadedSubsystem<XRInputSubsystem>();
            Debug.Log($"XRInput: {xrInput != null}");

            if (xrInput != null)
            {
                xrInput.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
                xrInput.TryRecenter();
            }

            var xrMesh = xrLoader.GetLoadedSubsystem<XRMeshSubsystem>();
            Debug.Log($"XRMesh: {xrMesh != null}");
            return false;
        }

        /// <summary>
        /// Enables the xrDeviceSimulator if no headset is connected.
        /// </summary>
        void Start()
        {
            xrDeviceSimulator.SetActive(DesktopPlay());
        }
    }
}
