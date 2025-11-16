using LibPostalNet;
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
        Country,
        State,
        StateDisctrict,
        City,
        CityDistrict,
        Road,
        HouseNumber,
        Level,
        Unit,
        Category,
        CountryRegion,
    }

    public static class AddressFormatterHelper
    {
        private static Dictionary<AddressFormatter, string> _converter = new Dictionary<AddressFormatter, string>()
        {
            { AddressFormatter.NotSet,          "NONE_NULL"     },
            { AddressFormatter.PostCode,        "index"         },
            { AddressFormatter.Country,         "country"       },
            { AddressFormatter.CountryRegion,   "country_region"       },
            { AddressFormatter.State,           "region"        },
            { AddressFormatter.StateDisctrict,  "district"      },
            { AddressFormatter.City,            "city"          },
            { AddressFormatter.CityDistrict,    "suburb"        },
            { AddressFormatter.Road,            "street"        },
            { AddressFormatter.HouseNumber,     "house_number"  },
            { AddressFormatter.Level,           "level"         },
            { AddressFormatter.Unit,            "unit"          },
            { AddressFormatter.Category,        "category"      },
        };

        // private static Dictionary<string, AddressFormatter> _reverseConverter = _converter.ToDictionary(c => c.Value, c => c.Key);
        private static Dictionary<string, AddressFormatter> _libpostalConverter = new Dictionary<string, AddressFormatter>()
        {
            { "NONE_NULL",      AddressFormatter.NotSet         },
            { "postcode",       AddressFormatter.PostCode       },
            { "country",        AddressFormatter.Country        },
            { "country_region", AddressFormatter.Country        },
            { "state",          AddressFormatter.State          },
            { "state_district", AddressFormatter.StateDisctrict },
            { "city",           AddressFormatter.City           },
            { "city_district",  AddressFormatter.CityDistrict   },
            { "suburb",         AddressFormatter.CityDistrict   },
            { "road",           AddressFormatter.Road           },
            { "house_number",   AddressFormatter.HouseNumber    },
            { "level",          AddressFormatter.Level          },
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

        public static ushort ToLibpostalAddress(this AddressFormatter formatter)
        {
            return formatter switch
            {
                AddressFormatter.PostCode => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_POSTAL_CODE,

                AddressFormatter.Country => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_TOPONYM,
                AddressFormatter.State => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_TOPONYM,
                AddressFormatter.StateDisctrict => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_TOPONYM,
                AddressFormatter.City => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_TOPONYM,
                AddressFormatter.CityDistrict => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_TOPONYM,

                AddressFormatter.Road => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_STREET,
                AddressFormatter.HouseNumber => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_HOUSE_NUMBER,
                AddressFormatter.Level => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_LEVEL,
                AddressFormatter.Unit => LibpostalNormalizeOptions.LIBPOSTAL_ADDRESS_UNIT,

                _ => throw new NotImplementedException(formatter.ToString())
            };
        }

        public static AddressFormatter[] HeaderToAddress(string header)
        {
            //index	region	district	city	suburb	street	house_number	unit    category
            var helperReverce = Enum.GetValues(typeof(AddressFormatter)).Cast<AddressFormatter>().ToDictionary(af => af.ToTsvString());
            var h2a = header.Split('\t').Select(c => helperReverce[c]).ToArray();
            return h2a;
        }
    }
}
