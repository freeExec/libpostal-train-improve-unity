using System.Collections.Generic;
using LP.Model;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

namespace LP.UI
{
    public class ComponentsGroup : MonoBehaviour, IDropHandler//, IArriveComponentHandler
    {
        [SerializeField] Image _background = default;
        [SerializeField] TextMeshProUGUI _labelGroup = default;
        [SerializeField] Transform _container = default;
        [SerializeField] GameObject _dropZone = default;

        private Transform _dragZone;
        private List<ElementModel> _elements = new List<ElementModel>();
        public AddressFormatter Group { get; private set; }
        public IEnumerable<ElementModel> Elements => _elements;

        public void Setup(AddressFormatter group, Color color, Transform dragZone)
        {
            Group = group;
            _dragZone = dragZone;
            _background.color = color;
            name = _labelGroup.text = Group.ToTsvString();
        }

        public void SetupElements(IEnumerable<ElementModel> data)
        {
            _elements.Clear();
            _elements.AddRange(data);
            UpdateElements();
        }

        private void UpdateElements()
        {
            CollectionInstantiator.Update<AddressComponent, ElementModel>(_container, _elements, (view, model) =>
            {
                view.Setup(model, _dragZone);
                view.SetColorMarker(_background.color);
                view.Movable.OnBegin -= OnComponentBeginDrag;
                view.Movable.OnBegin += OnComponentBeginDrag;
                view.Movable.OnEnded -= OnComponentEndedDrag;
                view.Movable.OnEnded += OnComponentEndedDrag;
            });

            _dropZone.gameObject.SetActive(_elements.All(d => d.IsEmpty));
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != default)
            {
                //eventData.pointerDrag.transform.SetParent(transform, true);
                var dropComponent = eventData.pointerDrag.GetComponent<AddressComponent>();
                ArriveComponent(dropComponent.Element);
                dropComponent.SetEmpty();
            }
        }

        /*public void ArriveComponent(AddressComponent component)
        {
            var element = new ElementModel(Group, component.Element.Value, ElementSource.ManualUserSeparate);
            _elements.Add(element);
            UpdateElements();
        }*/

        public void ArriveComponent(ElementModel originElement)
        {
            var element = new ElementModel(Group, originElement.Value, ElementSource.ManualUserSeparate);
            _elements.Add(element);
            UpdateElements();
        }

        private void DepartureComponent(AddressComponent component)
        {
            _elements.Remove(component.Element);
            UpdateElements();
        }

        private void OnComponentBeginDrag(Movable m)
        {
            var component = m.GetComponent<AddressComponent>();
            component.Movable.OnBegin -= OnComponentBeginDrag;
            DepartureComponent(component);
        }

        private void OnComponentEndedDrag(Movable m)
        {
            var component = m.GetComponent<AddressComponent>();
            component.Movable.OnEnded -= OnComponentEndedDrag;
            if (!component.Movable.IsDropping)
                ArriveComponent(component.Element);
        }
    }
}