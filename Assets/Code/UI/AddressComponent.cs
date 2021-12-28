using LP.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LP.UI
{
    public class AddressComponent : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TextMeshProUGUI _label = default;
        [SerializeField] Movable _movable = default;
        [SerializeField] Image _colorMarker = default;


        public ElementModel Element { get; private set; }
        public Movable Movable => _movable;


        public void Setup(ElementModel element, Transform dragZone)
        {
            if (element.IsEmpty)
                gameObject.SetActive(false);

            Element = element;
            _label.text = element.Value;

            _movable.Setup(dragZone);
        }

        public void SetEmpty()
        {
            Element = default;
            SetColorMarker(Color.white);
            Setup(ElementModel.EmptyElement, default);
        }

        public void SetColorMarker(Color color)
        {
            _colorMarker.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                //GetComponentInParent<SegregateWindow>()
            }
        }
    }
}