using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LibPostalNet;
using TMPro;
using LP.Data;
using LP.Model;
using UnityEngine.UI;

namespace LP.UI
{
    public class SegregateWindow : MonoBehaviour
    {
        private const char SPLIT_SEPATARE = '\t';

        [SerializeField] AddressRecord tsvAddressView = default;
        [SerializeField] AddressRecord postalAddressView = default;
        [SerializeField] AddressRecord outAddressView = default;

        [SerializeField] Button _buttonSkip = default;
        [SerializeField] Button _buttonCopyTsv = default;
        [SerializeField] Button _buttonNext = default;
        [SerializeField] Button _buttonDump = default;

        private PreTrainDataReader dataReader;
        private LibpostalNormalizeOptions optExpand;
        private LibpostalAddressParserOptions parseOpt;

        void Start()
        {
            dataReader = new PreTrainDataReader(Application.streamingAssetsPath);
            //var currentLine = dataReader.GetNextRecord();   // headers

            _buttonSkip.onClick.AddListener(OnSkipRecord);
            _buttonCopyTsv.onClick.AddListener(CopyTsvToResult);
            _buttonNext.onClick.AddListener(OnNextAddress);
            _buttonDump.onClick.AddListener(DumpProgress);

            var dataPath = Path.Combine(Application.streamingAssetsPath, "Libpostal");
            bool a = libpostal.LibpostalSetupDatadir(dataPath);
            bool b = libpostal.LibpostalSetupLanguageClassifierDatadir(dataPath);
            bool c = libpostal.LibpostalSetupParserDatadir(dataPath);

            // Debug.Log(a && b && c);

            optExpand = libpostal.LibpostalGetDefaultOptions();
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

            //var expansion = libpostal.LibpostalExpandAddress(currentLine, optExpand);
            //_label.text = expansion.Expansions[1];

            parseOpt = new LibpostalAddressParserOptions();

            OnNextAddress();
        }

        private void OnDestroy()
        {
            //if (dataReader != default)
            //    dataReader.Dispose();

            // Teardown (only called once at the end of your program)
            libpostal.LibpostalTeardown();
            libpostal.LibpostalTeardownParser();
            libpostal.LibpostalTeardownLanguageClassifier();
        }

        private void MarkTsvOk()
        {
            dataReader.MarkRecordOk();
            SaveCurrentAddress();
        }

        private void OnSkipRecord()
        {
            outAddressView.Clear();
            OnNextAddress();
        }

        private void CopyTsvToResult()
        {
            //var row = string.Join(",", tsvAddressView.Elements.Where(e => !e.IsEmpty).OrderBy(e => e.Group).Select(e => e.Value));
            //Debug.Log(row);

            outAddressView.Setup(tsvAddressView.Elements.Where(e => !e.IsEmpty));
        }

        private void SaveCurrentAddress()
        {
            //var row = string.Join("\t", tsvAddressView.Elements.Where(e => !e.IsEmpty).OrderBy(e => e.Group).Select(e => e.Value));
            var row = string.Join("\t", tsvAddressView.Elements.OrderBy(e => e.Group).Select(e => e.Value));
            if (string.IsNullOrEmpty(row))
                return;

            dataReader.SetRecord(row);
        }

        private void OnNextAddress()
        {
            if (!outAddressView.IsEmpty)
                SaveCurrentAddress();

            var currentLine = dataReader.GetNextRecord();

            var addressComponents = currentLine
                .Split(SPLIT_SEPATARE)
                .Zip(tsvAddressView.AddressColumns, (value, address) =>
                    new ElementModel(address, value, ElementSource.PreparePythonScript));
            tsvAddressView.Setup(addressComponents);

            var parse = libpostal.LibpostalParseAddress(currentLine, parseOpt);

            addressComponents = parse.Results
                .Select(r => new ElementModel(AddressFormatterHelper.GetFormatterFromLibpostal(r.Key), r.Value, ElementSource.Libpostal));
            postalAddressView.Setup(addressComponents);

            outAddressView.Clear();
        }

        private void DumpProgress()
        {
            dataReader.SaveTsvPreTrainData();
        }
    }
}