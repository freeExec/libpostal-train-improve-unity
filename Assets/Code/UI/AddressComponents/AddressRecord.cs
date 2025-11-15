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
        [SerializeField] Transform _container = default;
        [SerializeField] Transform _dragZone = default;
        [SerializeField] AddressFormatter[] _addressColumns = default;
        [SerializeField] UiRecordParams _recordParams = default;

        private Dictionary<AddressFormatter, ComponentsGroup> _groupsRecord;

        public AddressFormatter[] AddressColumns => _addressColumns;

        public IEnumerable<ElementModel> Elements => _groupsRecord.Values.SelectMany(r => r.Elements);

        public bool IsEmpty => _groupsRecord.All(r => r.Value.Elements.All(e => e.IsEmpty));

        private void Awake()
        {
            _groupsRecord = CollectionInstantiator.Update<ComponentsGroup, AddressFormatter>( _container, _addressColumns,
                (view, model) =>
            {
                var paramRecordType = _recordParams.Params[(int)model];
                view.Setup(model, paramRecordType.Color,  _dragZone);
                ((RectTransform)view.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, paramRecordType.WidthColumn);
            }).ToDictionary(r => r.Group);
        }

        public void Setup(IEnumerable<ElementModel> components)
        {
            Clear();

            foreach (var component in components.GroupBy(c => c.Group))
            {
                var group = component.First().Group;
                if (_groupsRecord.TryGetValue(group, out ComponentsGroup groupRecord))
                    groupRecord.SetupElements(component);
                else
                    Debug.LogWarning($"Group not found: {group} => {string.Join("|", component.Select(c => c.Value))}");
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