using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GraphicsCat
{
    public class Logger
    {
        public static void Log(string msg, Object context = null)
        {
            Debug.Log(AddCallerTag(msg), context);
        }

        public static void LogWarning(string msg, Object context = null)
        {
            Debug.LogWarning(AddCallerTag(msg));
        }

        public static void LogError(string msg, Object context = null)
        {
            Debug.LogError(AddCallerTag(msg));
        }

        public static string AddCallerTag(string msg)
        {
            var methodInfo = new StackTrace().GetFrame(2).GetMethod();
            var tag = "[" + methodInfo.ReflectedType.Name + "]";
            return tag + " " + msg;
        }
    }
}

