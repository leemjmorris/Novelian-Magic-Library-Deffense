using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GraphicsCat
{
    public static class PrefUtils
    {
        public static void Set<T>(string key, T value)
        {
            InternalSet<T>(key, value);
        }

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            return InternalGet<T>(key, defaultValue);
        }

        private static void InternalSet<T>(string key, T value)
        {
#if UNITY_EDITOR
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    EditorPrefs.SetInt(key, (int)(object)value);
                    break;
                case TypeCode.Single:
                    EditorPrefs.SetFloat(key, (float)(object)value);
                    break;
                case TypeCode.String:
                    EditorPrefs.SetString(key, (string)(object)value);
                    break;
                case TypeCode.Boolean:
                    EditorPrefs.SetBool(key, (bool)(object)value);
                    break;
            }
#else
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Int32:
                PlayerPrefs.SetInt(key, (int)(object)value);
                break;
            case TypeCode.Single:
                PlayerPrefs.SetFloat(key, (float)(object)value);
                break;
            case TypeCode.String:
                PlayerPrefs.SetString(key, (string)(object)value);
                break;
            case TypeCode.Boolean:
                PlayerPrefs.SetInt(key, (bool)(object)value ? 1 : 0);
                break;
        }
        PlayerPrefs.Save();
#endif
        }

        private static T InternalGet<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

#if UNITY_EDITOR
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    return (T)(object)EditorPrefs.GetInt(key, (int)(object)defaultValue);
                case TypeCode.Single:
                    return (T)(object)EditorPrefs.GetFloat(key, (float)(object)defaultValue);
                case TypeCode.String:
                    return (T)(object)EditorPrefs.GetString(key, (string)(object)defaultValue);
                case TypeCode.Boolean:
                    return (T)(object)EditorPrefs.GetBool(key, (bool)(object)defaultValue);
                default:
                    return defaultValue;
            }
#else
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Int32:
                return (T)(object)PlayerPrefs.GetInt(key, (int)(object)defaultValue);
            case TypeCode.Single:
                return (T)(object)PlayerPrefs.GetFloat(key, (float)(object)defaultValue);
            case TypeCode.String:
                return (T)(object)PlayerPrefs.GetString(key, (string)(object)defaultValue);
            case TypeCode.Boolean:
                return (T)(object)(PlayerPrefs.GetInt(key, (bool)(object)defaultValue ? 1 : 0) == 1);
            default:
                return defaultValue;
        }
#endif
        }
    }
}