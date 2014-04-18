using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    [ConfigurationCollection(typeof(ExcludeVerbElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ExcludeVerbElementCollection : ConfigurationElementCollection
    {
        public ExcludeVerbElement this[int index]
        {
            get
            {
                return (ExcludeVerbElement)BaseGet(index);
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
            return new ExcludeVerbElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExcludeVerbElement)element).Verb;
        }

    }
}
