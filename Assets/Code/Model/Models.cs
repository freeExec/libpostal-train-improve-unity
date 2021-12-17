using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Model
{
    public enum ElementSource
    {
        Unknown = -1,
        RawOpenData  = 0,
        PreparePythonScript,
        Libpostal,
        ManualUserSeparate,
    }

    public class ElementModel
    {
        public readonly static ElementModel EmptyElement = new ElementModel(AddressFormatter.NotSet);

        public readonly AddressFormatter Group;
        public readonly string Value;
        public readonly ElementSource Source;

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public ElementModel(AddressFormatter group)
        {
            this.Group = group;
        }

        public ElementModel(AddressFormatter group, string value, ElementSource source)
        {
            this.Group = group;
            this.Value = value;
            this.Source = source;
        }

        public override string ToString()
        {
            return IsEmpty ? "<Empty>" : $"{Group}:{Value}";
        }
    }
}
