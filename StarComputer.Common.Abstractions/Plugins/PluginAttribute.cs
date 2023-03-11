namespace StarComputer.Common.Abstractions.Plugins
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        public PluginAttribute(string domain)
        {
            Domain = (PluginDomain)domain;
        }


        public PluginDomain Domain { get; }


        public static implicit operator bool(PluginAttribute? attr)
        {
            return attr is not null;
        }
    }
}
