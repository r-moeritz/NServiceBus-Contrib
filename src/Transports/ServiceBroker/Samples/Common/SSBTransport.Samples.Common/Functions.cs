using System;

namespace SSBTransport.Samples.Common
{
    public static class Functions
    {
        public static bool IsEvent(this Type t)
        {
            return t.Namespace != null
                   && t.Namespace.EndsWith(".Events")
                   && t.Name.EndsWith("Event");
        }
    }
}
