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
        public static string ToTsvString(this AddressFormatter formatter) => _converter[formatter];
    }
}
