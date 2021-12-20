using LP.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;


namespace LP.UI
{
    public class AddressRecord : MonoBehaviour
    {
        [SerializeField] Color[] _groupColors;
        [SerializeField] Transform _container = default;
        [SerializeField] Transform _dragZone = default;

        private AddressFormatter[] _addressColumns = new AddressFormatter[]
        {
            AddressFormatter.PostCode,
            AddressFormatter.State,
            AddressFormatter.StateDisctrict,
            AddressFormatter.City,
            AddressFormatter.CityDistrict,
            AddressFormatter.Road,
            AddressFormatter.HouseNumber,
            AddressFormatter.Unit,
        };

        private Dictionary<AddressFormatter, ComponentsGroup> _groupsRecord;

        public AddressFormatter[] AddressColumns => _addressColumns;

        public IEnumerable<ElementModel> Elements => _groupsRecord.Values.SelectMany(r => r.Elements);

        private void Awake()
        {
            //var groups = ((AddressFormatter[])Enum.GetValues(typeof(AddressFormatter))).Take(_groupColors.Length);

            _groupsRecord = CollectionInstantiator.Update<ComponentsGroup, AddressFormatter>( _container, _addressColumns,
                (view, model) =>
            {
                view.Setup(model, _groupColors[(int)model],  _dragZone);
            }).ToDictionary(r => r.Group);
        }

        public void Setup(IEnumerable<ElementModel> components)
        {
            foreach (var component in components)
            {
                _groupsRecord[component.Group].SetupElements(Enumerable.Empty<ElementModel>().Append(component));
            }
        }

        public void Clear()
        {
            foreach(var record in _groupsRecord.Values)
            {
                record.SetupElements(Enumerable.Empty<ElementModel>());
            }
        }

        public IEnumerable<ElementModel> GetElementByGroup(AddressFormatter group) => _groupsRecord[group].Elements;
    }
}