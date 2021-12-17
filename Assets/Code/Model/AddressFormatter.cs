using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Model
{
    public enum AddressFormatter
    {
        NotSet          = -1,
        PostCode        = 0,
        State,
        StateDisctrict,
        City,
        CityDistrict,
        Road,
        HouseNumber,
        Unit,
    }

    public static class AddressFormatterHelper
    {
        private static Dictionary<AddressFormatter, string> _converter = new Dictionary<AddressFormatter, string>()
        {
            { AddressFormatter.NotSet,          "NONE_NULL" },
            { AddressFormatter.PostCode,        "index"     },
            { AddressFormatter.State,           "region"    },
            { AddressFormatter.StateDisctrict,  "district"  },
            { AddressFormatter.City,            "city"      },
            { AddressFormatter.CityDistrict,    "suburb"    },
            { AddressFormatter.Road,            "street"    },
            { AddressFormatter.HouseNumber,     "house"     },
            { AddressFormatter.Unit,            "unit"      },
        };

        // private static Dictionary<string, AddressFormatter> _reverseConverter = _converter.ToDictionary(c => c.Value, c => c.Key);
        private static Dictionary<string, AddressFormatter> _libpostalConverter = new Dictionary<string, AddressFormatter>()
        {
            { "NONE_NULL",      AddressFormatter.NotSet         },
            { "postcode",      AddressFormatter.PostCode       },
            { "state",          AddressFormatter.State          },
            { "state_district", AddressFormatter.StateDisctrict },
            { "city",           AddressFormatter.City           },
            { "city_district",  AddressFormatter.CityDistrict   },
            { "road",           AddressFormatter.Road           },
            { "house_number",   AddressFormatter.HouseNumber    },
            { "unit",           AddressFormatter.Unit           },
        };

        public static string ToTsvString(this AddressFormatter formatter) => _converter[formatter];
        public static AddressFormatter GetFormatterFromLibpostal(string component)
        {
            if (_libpostalConverter.TryGetValue(component, out AddressFormatter formatter))
                return formatter;
            UnityEngine.Debug.Log(component);
            return AddressFormatter.NotSet;
        }
    }
}
