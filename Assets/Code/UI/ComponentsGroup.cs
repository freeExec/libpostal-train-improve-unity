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
    public class ComponentsGroup : MonoBehaviour, IDropHandler
    {
        [SerializeField] Image _background = default;
        [SerializeField] TextMeshProUGUI _labelGroup = default;
        [SerializeField] Transform _container = default;
        [SerializeField] GameObject _dropZone = default;

        private Transform _dragZone;
        private List<ElementModel> elements;
        public AddressFormatter Group { get; private set; }

        public void Setup(AddressFormatter group, Color color, Transform dragZone)
        {
            Group = group;
            _dragZone = dragZone;
            _background.color = color;
            name = _labelGroup.text = Group.ToTsvString();            
        }

        public void SetupElements(IEnumerable<ElementModel> data)
        {
            elements = data.ToList();

            CollectionInstantiator.Update<AddressComponent, ElementModel>(_container, elements, (view, model) =>
            {
                view.Setup(model, _dragZone);
            });

            _dropZone.gameObject.SetActive(data.All(d => d.IsEmpty));
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("DROP droper");
            if (eventData.pointerDrag != default)
            {
                //((RectTransform)eventData.pointerDrag.transform).anchoredPosition = ((RectTransform)transform).anchoredPosition;
                //eventData.pointerDrag.transform.SetParent(transform, true);
                var dropComponent = eventData.pointerDrag.GetComponent<AddressComponent>();
                ArriveComponent(dropComponent);
                dropComponent.SetEmpty();
            }
        }

        private void ArriveComponent(AddressComponent component)
        {
            elements.Add(component.Element);
            SetupElements(elements);
        }

        public void DepartureComponent(AddressComponent component)
        {
            elements.Remove(component.Element);
            SetupElements(elements);
        }
    }
}