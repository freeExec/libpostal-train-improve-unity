using LP.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LP.Data
{
	public static class ImproveTextTools
	{

        static (AddressFormatter AddressFormatter, string[] Replaces)[] _replacesHelperToInserSpace = new (AddressFormatter, string[])[]
        {
            ( AddressFormatter.City,         new string[] { "п.", "г.", "д.", "с.", "пос." } ),
            ( AddressFormatter.Road,         new string[] { "ул.", "пр.", "пер.", "пр-т." } ),
            ( AddressFormatter.CityDistrict, new string[] { "мкр." } ),
            ( AddressFormatter.HouseNumber,  new string[] { "д.", "лит.", "стр.", "кор.", "корп.", "вл." } ),
            ( AddressFormatter.Unit,         new string[] { "пом.", "кв.", "оф." } ),
        };

        public static List<ElementModel> InsertSpaceAndTrim(IEnumerable<ElementModel> elements)
		{
			var elementsResult = elements.ToList();

			foreach (var tuple in _replacesHelperToInserSpace)
			{
				for (int i = 0; i < elementsResult.Count; i++)
				{
					var fixElement = elementsResult[i];
					if (fixElement.Group != tuple.AddressFormatter)
						continue;

					int originalLength = fixElement.Value.Length;
					string elementValue = fixElement.Value.TrimEnd('.', ' ').Replace("«", "").Replace("»", "");
					bool isModify = originalLength > elementValue.Length;
					foreach (var replace in tuple.Replaces)
					{
						int pos = elementValue.IndexOf(replace);
						if (pos == -1)
							continue;

						int posToInsert = pos + replace.Length;
						if (posToInsert < elementValue.Length && elementValue[posToInsert] != ' ')
						{
							elementValue = elementValue.Insert(posToInsert, " ");
							isModify = true;
						}
					}
					if (isModify)
                        elementsResult[i] = new ElementModel(tuple.AddressFormatter, elementValue, ElementSource.ManualUserSeparate);
				}
			}

			return elementsResult;
		}
	}
}