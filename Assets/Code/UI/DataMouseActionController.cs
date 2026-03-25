using LP.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class DataMouseActionController : MonoBehaviour
    {
        public enum MouseButton
        {
            Left = 0,
            Middle = 2,
            Right = 1,
            LeftUp = 3,
            LeftDown = 4,
        }

        public enum MouseAction
        {
            None,
            Delete,
            AddToTarget,
            ReplaceOnTarget,
        }

        [Serializable]
        public struct MouseActionSetting
        {
            public MouseButton MouseButton;
            public MouseAction MouseAction;
        }

        [SerializeField] private GraphicRaycaster raycaster;
        [SerializeField] private AddressRecord targetRecord;

        [SerializeField] private MouseActionSetting[] mouseActionSettings;

        List<RaycastResult> results = new List<RaycastResult>();

        void Start()
        {

        }

        
        void Update()
        {
            foreach (var item in mouseActionSettings)
            {
                if (Input.GetMouseButtonDown((int)item.MouseButton))
                {
                    var eventSystem = EventSystem.current;

                    PointerEventData pointerData = new PointerEventData(eventSystem);
                    pointerData.position = Input.mousePosition;

                    results.Clear();
                    raycaster.Raycast(pointerData, results);

                    foreach (RaycastResult result in results)
                    {
                        var addressComponent = result.gameObject.GetComponent<AddressComponent>();
                        if (addressComponent != null)
                        {
                            Debug.Log($"AddressComponent: '{addressComponent.Element}' {item.MouseAction}");

                            switch (item.MouseAction)
                            {
                                case MouseAction.Delete:
                                    OnDeleteAction(addressComponent);
                                    break;
                                case MouseAction.AddToTarget:
                                    OnAddToTargetAction(addressComponent);
                                    break;
                                case MouseAction.ReplaceOnTarget:
                                    OnReplaceOnTargetAction(addressComponent);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void OnDeleteAction(AddressComponent addressComponent)
        {
            var group = addressComponent.GetComponentInParent<ComponentsGroup>();
            group.DepartureComponent(addressComponent);
        }

        private void OnAddToTargetAction(AddressComponent addressComponent)
        {
            OnDeleteAction(addressComponent);

            var targetGroup = targetRecord.GetComponentsGroup(addressComponent.Element.Group);
            targetGroup.ArriveComponent(addressComponent.Element);
        }

        private void OnReplaceOnTargetAction(AddressComponent addressComponent)
        {
            OnDeleteAction(addressComponent);

            var targetGroup = targetRecord.GetComponentsGroup(addressComponent.Element.Group);
            targetGroup.SetupElements(new[] { addressComponent.Element });
        }
    }
}