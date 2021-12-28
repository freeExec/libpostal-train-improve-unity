using System;
using System.Collections;
using System.Collections.Generic;
using LP.Model;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LP.UI
{
    public class EditComponentWindow : MonoBehaviour
    {
        [SerializeField] TMP_InputField _inputField = default;
        [SerializeField] Button _buttonOk = default;
        [SerializeField] Button _buttonClose = default;

        private AddressComponent _editingComponent;

        public event Action<ElementModel> OnEditFinish = delegate(ElementModel c) { };

        private void Start()
        {
            _buttonOk.onClick.AddListener(OnClickFinish);
            _buttonClose.onClick.AddListener(OnClickClose);
        }

        public void Setup(AddressComponent component)
        {
            _editingComponent = component;
            _inputField.text = component.Element.Value;
        }

        private void OnClickClose()
        {
            OnEditFinish(_editingComponent.Element);
            gameObject.SetActive(false);
        }

        private void OnClickFinish()
        {
            var element = new ElementModel(_editingComponent.Element.Group, _inputField.text, ElementSource.ManualUserEditing);
            OnEditFinish(element);

            gameObject.SetActive(false);
        }
    }
}