using System.Reflection;

namespace GraphicsCat
{
    public class ReflectionFlags
    {
        // Instance member flags
        public const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;
        public const BindingFlags InstanceAnyAccess = InstancePublic | BindingFlags.NonPublic;

        // Static member flags
        public const BindingFlags StaticPublic = BindingFlags.Static | BindingFlags.Public;
        public const BindingFlags StaticAnyAccess = StaticPublic | BindingFlags.NonPublic;
    }
}
