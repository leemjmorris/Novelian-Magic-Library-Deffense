
using System;
using System.Reflection;

namespace GraphicsCat
{
    public class AssemblyUtils
    {
        public static Type GetTypeByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = GetTypeByName(assembly, name);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        } 

        public static Type GetTypeByName(Assembly assembly, string name)
        {
            if (assembly == null)
                return null;

            // for full name
            var type = assembly.GetType(name);
            if (type != null)
                return type;

            // for short name
            var types = assembly.GetTypes();
            foreach (var t in types)
            {
                if (t.Name == name)
                    return t;
            }

            return null;
        }
    }
}
