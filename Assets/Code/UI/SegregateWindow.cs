using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LibPostalNet;
using TMPro;
using LP.Data;
using LP.Model;

namespace LP.UI
{
    public class SegregateWindow : MonoBehaviour
    {
        private const char SPLIT_SEPATARE = '\t';

        [SerializeField] AddressRecord tsvAddressView = default;
        [SerializeField] AddressRecord postalAddressView = default;
        [SerializeField] AddressRecord outAddressView = default;

        private PreTrainDataReader dataReader;

        void Start()
        {
            dataReader = new PreTrainDataReader(Application.streamingAssetsPath);
            var currentLine = dataReader.GetNextRecord();   // headers
            currentLine = dataReader.GetNextRecord();



            //var line = "БЕЛГОРОДСКАЯ ОБЛАСТЬ\t\tПГТ БОРИСОВКА\t\tУЛ. 8 МАРТА\tД.9";
            var addressComponents = currentLine
                .Split(SPLIT_SEPATARE)
                .Zip(tsvAddressView.AddressColumns, (value, address) => 
                    new ElementModel(address, value, ElementSource.OpenData));
            tsvAddressView.Setup(addressComponents);

            return;

            var dataPath = Path.Combine(Application.streamingAssetsPath, "Libpostal");
            //dataPath = "d:/Unity3D/LP-addres-segregate/Assets/StreamingAssets/Libpostal";
            bool a = libpostal.LibpostalSetupDatadir(dataPath);
            bool b = libpostal.LibpostalSetupLanguageClassifierDatadir(dataPath);
            bool c = libpostal.LibpostalSetupParserDatadir(dataPath);

            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(c);

            var query = "БЕЛГОРОДСКАЯ ОБЛАСТЬ, ПГТ БОРИСОВКА, УЛ. 8 МАРТА, Д.9";

            var optExpand = libpostal.LibpostalGetDefaultOptions();
            optExpand.LatinAscii = false;
            optExpand.StripAccents = false;
            optExpand.Decompose = false;

            //optExpand.DeleteAcronymPeriods = false;
            //optExpand.DeleteNumericHyphens = false;
            //optExpand.DropParentheticals = false;
            //optExpand.DeleteWordHyphens = false;
            //optExpand.DropEnglishPossessives = false;
            optExpand.DeleteApostrophes = false;

            optExpand.SplitAlphaFromNumeric = false;        // раздвигать буквы от цифр (особо мешает в номере дома)
            optExpand.ReplaceWordHyphens = false;           // удалять дефисы

            var expansion = libpostal.LibpostalExpandAddress(query, optExpand);
            //_label.text = expansion.Expansions[1];

            var parseOpt = new LibpostalAddressParserOptions();
            var result = libpostal.LibpostalParseAddress(query, parseOpt);

            var c1 = result.Results.First();
            //_label.text = $"{c1.Key}:{c1.Value}";
        }

        private void OnDestroy()
        {
            if (dataReader != default)
                dataReader.Dispose();

            // Teardown (only called once at the end of your program)
            libpostal.LibpostalTeardown();
            libpostal.LibpostalTeardownParser();
            libpostal.LibpostalTeardownLanguageClassifier();
        }

    }
}