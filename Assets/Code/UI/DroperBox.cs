using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LP.UI
{
    public class DroperBox : MonoBehaviour, IDropHandler
    {
        public event Action<AddressComponent> OnDropAddressComponent = delegate (AddressComponent c) { };

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != default)
            {
                var dropComponent = eventData.pointerDrag.GetComponent<AddressComponent>();
                dropComponent.Movable.IsDropping = true;
                OnDropAddressComponent(dropComponent);
                //var arriverHandler = GetComponent<IArriveComponentHandler>();
                //if (arriverHandler != null)
                //    arriverHandler.ArriveComponent(dropComponent);
            }
        }
    }
}