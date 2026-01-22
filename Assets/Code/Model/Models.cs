using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Model
{
    public class ElementModel
    {
        public readonly static ElementModel EmptyElement = new ElementModel(AddressFormatter.NotSet);

        public readonly AddressFormatter Group;
        public readonly string Value;

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public ElementModel(AddressFormatter group)
        {
            this.Group = group;
        }

        public ElementModel(AddressFormatter group, string value)
        {
            this.Group = group;
            this.Value = value;
        }

        public override string ToString()
        {
            return IsEmpty ? "<Empty>" : $"{Group}:{Value}";
        }
    }

    public class ElementModelMatchComparer : IEqualityComparer<ElementModel>
    {
        public bool Equals(ElementModel x, ElementModel y)
        {
            return x.Group == y.Group && x.Value == y.Value;
        }

        public int GetHashCode(ElementModel obj)
        {
            return obj.Group.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }
}
