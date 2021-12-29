using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LP.UI
{
    public class Movable : MonoBehaviour, //IPointerClickHandler,
        //IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        private Canvas _parentCanvas;
        private RectTransform _rect;
        private LayoutElement _layoutElement;
        private CanvasGroup _canvasGroup;

        private Transform _originParent;
        private Transform _dragZone;

        public event Action<Movable> OnBegin = delegate (Movable m) { };
        public event Action<Movable> OnEnded = delegate (Movable m) { };

        public ComponentsGroup FromComponentGroup { get; private set; }
        public bool IsDropping { get; set; }

        private void Start()
        {
            _rect = (RectTransform)transform;
            _parentCanvas = GetComponentInParent<Canvas>();

            _canvasGroup = GetComponent<CanvasGroup>();

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                enabled = false;
        }

        public void Setup(Transform dragZone)
        {
            _dragZone = dragZone;
        }

        /*public void OnPointerClick(PointerEventData eventData)
        {
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }
        */
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            //var go = Instantiate(gameObject, transform.position, Quaternion.identity, _dragZone);
            //((RectTransform)go.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            FromComponentGroup = GetComponentInParent<ComponentsGroup>();
            _originParent = transform.parent;
            transform.SetParent(_dragZone, true);


            if (_originParent.childCount == 0)
            {
                var go = Instantiate(gameObject, transform.position, Quaternion.identity, _originParent);
                go.name = "AddressComponet(reserve)";
                go.SetActive(false);
            }

            _canvasGroup.blocksRaycasts = false;
            _layoutElement.ignoreLayout = true;

            IsDropping = false;

            OnBegin(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.SetParent(_originParent, true);

            /*if (!IsDropping)
            {
                FromComponentGroup.ArriveComponent(GetComponent<AddressComponent>());
            }*/

            _originParent = null;
            FromComponentGroup = null;

            _canvasGroup.blocksRaycasts = true;
            _layoutElement.ignoreLayout = false;

            gameObject.SetActive(false);

            OnEnded(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rect.anchoredPosition += eventData.delta / (_parentCanvas != default ? _parentCanvas.scaleFactor : 1);
        }
    }
}