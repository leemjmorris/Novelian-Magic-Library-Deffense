using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsCat
{
    public static class VolumeUtils
    {
        public static T GetComponent<T>() where T : VolumeComponent
        {
            if (VolumeManager.instance == null || VolumeManager.instance.stack == null)
                return null;

            return VolumeManager.instance.stack.GetComponent<T>();

            // return VolumeManager.instance?.stack?.GetComponent<T>();
        }

        public static void SetComponentActive<T>(bool active) where T : VolumeComponent
        {
            var volumes = GameObject.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var volume in volumes)
                SetComponentActive<T>(volume, active);
        }

        public static void SetComponentActive<T>(Volume volume, bool active) where T : VolumeComponent
        {
            if (volume == null)
            {
                Debug.LogError("Volume component is null.");
                return;
            }

            var volumeProfile = volume.profile;
            if (volumeProfile.TryGet(out T component))
            {
                if (component.active != active)
                    component.active = active;
            }
        }
    }
}