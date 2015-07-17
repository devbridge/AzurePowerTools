using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    [ConfigurationCollection(typeof(ExcludeUrlElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ExcludeUrlElementCollection : ConfigurationElementCollection
    {
        public ExcludeUrlElement this[int index]
        {
            get
            {
                return (ExcludeUrlElement)BaseGet(index);
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
            return new ExcludeUrlElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExcludeUrlElement)element).Url;
        }

    }
}
