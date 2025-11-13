using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Core
{
    /// <summary>
    /// LMJ: Service Locator pattern for accessing managers without direct coupling
    /// Provides centralized access to game services (managers) with type safety
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// LMJ: Register a service instance
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            Type type = typeof(T);

            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} is already registered. Overwriting.");
            }

            services[type] = service;
            Debug.Log($"[ServiceLocator] Registered {type.Name}");
        }

        /// <summary>
        /// LMJ: Unregister a service instance
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            Type type = typeof(T);

            if (services.ContainsKey(type))
            {
                services.Remove(type);
                Debug.Log($"[ServiceLocator] Unregistered {type.Name}");
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} is not registered.");
            }
        }

        /// <summary>
        /// LMJ: Get a service instance by type
        /// </summary>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);

            if (services.TryGetValue(type, out object service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service of type {type.Name} not found!");
            return null;
        }

        /// <summary>
        /// LMJ: Try to get a service instance by type (returns null if not found, no error log)
        /// </summary>
        public static T TryGet<T>() where T : class
        {
            Type type = typeof(T);

            if (services.TryGetValue(type, out object service))
            {
                return service as T;
            }

            return null;
        }

        /// <summary>
        /// LMJ: Check if a service is registered
        /// </summary>
        public static bool Has<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// LMJ: Clear all registered services (useful for cleanup)
        /// </summary>
        public static void Clear()
        {
            services.Clear();
            Debug.Log("[ServiceLocator] All services cleared");
        }
    }
}
