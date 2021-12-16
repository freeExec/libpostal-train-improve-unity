using LP.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LP.UI
{
    public class AddressComponent : MonoBehaviour, IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] TextMeshProUGUI _label = default;

        private Canvas _parentCanvas;
        private RectTransform _rect;
        private LayoutElement _layoutElement;
        private CanvasGroup _canvasGroup;
        private Transform _dragZone;

        private Transform _originParent;

        public ElementModel Element { get; private set; }

        private void Start()
        {
            _rect = (RectTransform)transform;
            _parentCanvas = GetComponentInParent<Canvas>();

            _canvasGroup = GetComponent<CanvasGroup>();

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                enabled = false;
        }

        public void Setup(ElementModel element, Transform dragZone)
        {
            if (element.IsEmpty)
                gameObject.SetActive(false);

            Element = element;
            _label.text = element.Value;
            _dragZone = dragZone;
        }

        public void SetEmpty()
        {
            Setup(ElementModel.EmptyElement, default);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //throw new System.NotImplementedException();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //throw new System.NotImplementedException();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //throw new System.NotImplementedException();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            //throw new System.NotImplementedException();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = false;
            _layoutElement.ignoreLayout = true;
            _originParent = transform.parent;
            transform.SetParent(_dragZone, true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.SetParent(_originParent, true);

            _originParent = null;

            _canvasGroup.blocksRaycasts = true;
            _layoutElement.ignoreLayout = false;

        }

        public void OnDrag(PointerEventData eventData)
        {
            _rect.anchoredPosition += eventData.delta / (_parentCanvas != default ? _parentCanvas.scaleFactor : 1);
        }
    }
}