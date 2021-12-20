using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LP.UI
{
    public class DroperBox : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("DROP droper");
            if (eventData.pointerDrag != default)
            {
                var dropComponent = eventData.pointerDrag.GetComponent<AddressComponent>();
                dropComponent.SetEmpty();
            }
        }
    }
}