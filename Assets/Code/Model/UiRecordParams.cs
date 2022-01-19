using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace LP.Model
{
    [CreateAssetMenu(fileName = "UiRecordParams", menuName = "ScriptableObject/UiRecordParams")]
    public class UiRecordParams : ScriptableObject
    {
        [Serializable]
        public class UiRecordParam
        {
            [SerializeField] AddressFormatter _adressFormatter;
            [SerializeField] Color _color;
            [SerializeField] int _widthColumn;


            public AddressFormatter AddressFormatter => _adressFormatter;
            public Color Color => _color;
            public int WidthColumn => _widthColumn;
        }
        [SerializeField] public List<UiRecordParam> Params;

        //#if UNITY_EDITOR
        //        [MenuItem("Assets/Create/ScriptableObject/UiRecordParams")]
        //        public static void CreateScriptableObject()
        //        {
        //            ProjectWindowUtil.CreateAsset(CreateInstance<UiRecordParams>(),
        //                $"{nameof(UiRecordParams)}.asset");
        //        }
        //#endif
    }
}