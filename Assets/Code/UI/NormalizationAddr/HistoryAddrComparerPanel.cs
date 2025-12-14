using LP.Data;
using LP.Model;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI;
using UnityEngine;

namespace LP.UI.HistoryAddrComparer
{
    public class HistoryAddrComparerPanel : MonoBehaviour
    {
        [SerializeField] AddressFormatter[] _addressColumns = default;
        [SerializeField] Color matchColor;
        [SerializeField] Color normalColor;
        [SerializeField] Color differentlColor;

        [SerializeField] Transform currentContent;
        [SerializeField] Transform previousContent;

        public bool Setup(List<KeyValuePair<AddressFormatter, HashSet<string>>> current, List<KeyValuePair<AddressFormatter, HashSet<string>>> previous)
        {
            var comparerDatasEnumerable = _addressColumns.Select(addrType =>
                new ComparerData(addrType, current.Find(d => d.Key == addrType).Value, previous?.Find(d => d.Key == addrType).Value)
            ).ToList();

            CollectionInstantiator.Update<TMP_Text, ComparerData>(currentContent, comparerDatasEnumerable,
                (view, model) =>
                {
                    view.text = model.CurrentAddr?.First();
                    view.color = normalColor;
                    if (model.PrevAddr != null)
                        view.color = model.IsMatch ? matchColor : differentlColor;
                });

            CollectionInstantiator.Update<TMP_Text, ComparerData>(previousContent, comparerDatasEnumerable,
                (view, model) =>
                {
                    view.text = model.PrevAddr?.First();
                });

            return comparerDatasEnumerable.All(d => d.IsMatch);
        }

        private class ComparerData
        {
            public readonly AddressFormatter Address;
            public readonly HashSet<string> CurrentAddr;
            public readonly HashSet<string> PrevAddr;

            public bool IsMatch
            {
                get
                {
                    if (CurrentAddr != null && PrevAddr != null)
                        return CurrentAddr.Overlaps(PrevAddr);

                    if (CurrentAddr == null && PrevAddr == null)
                        return true;
                    
                    return false;
                }
            }

            public ComparerData(AddressFormatter address, HashSet<string> currentAddr, HashSet<string> prevAddr)
            {
                Address = address;
                CurrentAddr = currentAddr;
                PrevAddr = prevAddr;
            }
        }
    }
}
