using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    [ConfigurationCollection(typeof(ExcludeElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ExcludeElementCollection : ConfigurationElementCollection
    {
        public ExcludeElement this[int index]
        {
            get
            {
                return (ExcludeElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ExcludeElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExcludeElement)element).ToString();
        }
    }
}
