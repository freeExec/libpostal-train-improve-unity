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
using System;

namespace LP.UI
{
    public class SegregateWindow : MonoBehaviour
    {
        private const char SPLIT_SEPATARE = '\t';

        [SerializeField] AddressRecord tsvAddressView = default;
        [SerializeField] AddressRecord postalAddressView = default;
        [SerializeField] AddressRecord outAddressView = default;

        [SerializeField] DroperBox _trashDrop = default;
        [SerializeField] DroperBox _libpostalParseDrop = default;
        [SerializeField] DroperBox _editComponentDrop = default;

        [SerializeField] Button _buttonSkip = default;
        [SerializeField] Button _buttonRefresh = default;
        [SerializeField] Button _buttonNext = default;
        [SerializeField] Button _buttonDump = default;

        [SerializeField] EditComponentWindow _editComponentWindow = default;
        [SerializeField] TextMeshProUGUI _counter = default;

        private PreTrainDataReader dataReader;
        private LibpostalNormalizeOptions optExpand;
        private LibpostalAddressParserOptions parseOpt;
        private List<AddressFormatter> headerOrder;

        void Start()
        {
            dataReader = new PreTrainDataReader(Application.streamingAssetsPath);

            headerOrder = HeaderToAddress(dataReader.Header);
            //var currentLine = dataReader.GetNextRecord();   // headers

            _buttonSkip.onClick.AddListener(OnSkipRecord);
            _buttonNext.onClick.AddListener(OnNextAddress);
            _buttonDump.onClick.AddListener(DumpProgress);
            _buttonRefresh.onClick.AddListener(OnRefreshAddress);

            _trashDrop.OnDropAddressComponent += (component) => component.SetEmpty();
            _libpostalParseDrop.OnDropAddressComponent += (component) => ShowLibpostalParse(component.Element.Value);
            _editComponentDrop.OnDropAddressComponent += OnEditComponentBegin;

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

            ShowNextAddress();
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

        /*private void MarkTsvOk()
        {
            dataReader.MarkRecordOk();
            SaveAddress();
        }*/

        private void OnSkipRecord()
        {
            ShowNextAddress();
        }

        private void SaveAddress(AddressRecord record)
        {
            //var row = string.Join("\t", tsvAddressView.Elements.Where(e => !e.IsEmpty).OrderBy(e => e.Group).Select(e => e.Value));

            //var row = string.Join("\t", record.Elements.OrderBy(e => e.Group).Select(e => e.Value));
            //if (string.IsNullOrEmpty(row))
            var elementsMap = record.Elements.ToLookup(e => e.Group);
            if (record.Elements.All(e => e.IsEmpty))
                return;

            var row = string.Join("\t", headerOrder.Select(h => string.Join(" ", elementsMap[h].Select(e => e.Value))));

            dataReader.SetRecord(row);
        }

        private void OnNextAddress()
        {
            if (!outAddressView.IsEmpty)
                SaveAddress(outAddressView);
            else
                SaveAddress(tsvAddressView);

            ShowNextAddress();

            _buttonDump.interactable = true;
        }

        private void ShowNextAddress(bool getNextAddr = true)
        {
            if (getNextAddr)
                _currentLine = dataReader.GetNextRecordByLong();

            var addressComponents = _currentLine
                .Split(SPLIT_SEPATARE)
                .Zip(tsvAddressView.AddressColumns, (value, address) =>
                    new ElementModel(address, value, ElementSource.PreparePythonScript));
            tsvAddressView.Setup(addressComponents);

            ShowLibpostalParse(_currentLine);

            //outAddressView.Clear();
            outAddressView.Setup(tsvAddressView.Elements.Where(e => !e.IsEmpty));

            _counter.text = $"Completed: {dataReader.CompletedLines}/{dataReader.TotalLines} ({(dataReader.CompletedLines / dataReader.TotalLines).ToString("P2")})";
        }

        private string _currentLine;
        private void OnRefreshAddress()
        {
            ShowNextAddress(false);
        }

        private void DumpProgress()
        {
            dataReader.SaveTsvPreTrainData();
            Debug.Log("Saved");
            _buttonDump.interactable = false;
        }

        private void ShowLibpostalParse(string addrStr)
        {
            var parse = libpostal.LibpostalParseAddress(addrStr, parseOpt);

            var addrStrLow = addrStr.ToLowerInvariant().Replace('\t', ' ').Replace(',', ' ');

            Func<string, string> recoveryCase = (libpostal) =>
            {
                int found = addrStrLow.IndexOf(libpostal);
                if (found != -1)
                {
                    return addrStr.Substring(found, libpostal.Length);
                }
                return libpostal;
            };


            var addressComponents = parse.Results
                .Select(r => new ElementModel(
                    AddressFormatterHelper.GetFormatterFromLibpostal(r.Key), 
                    recoveryCase(r.Value), 
                    ElementSource.Libpostal
                ));

            postalAddressView.Setup(addressComponents);
        }

        private ComponentsGroup _componentsGroup;
        private void OnEditComponentBegin(AddressComponent component)
        {
            _componentsGroup = component.Movable.FromComponentGroup;
            _editComponentWindow.Setup(component);

            _editComponentWindow.OnEditFinish += OnEditComponentFinish;

            _editComponentWindow.gameObject.SetActive(true);
        }

        private void OnEditComponentFinish(ElementModel element)
        {
            _editComponentWindow.OnEditFinish -= OnEditComponentFinish;
            _componentsGroup.ArriveComponent(element);
        }

        private static List<AddressFormatter> HeaderToAddress(string header)
        {
            //index	region	district	city	suburb	street	house_number	unit
            var helperReverce = Enum.GetValues(typeof(AddressFormatter)).Cast<AddressFormatter>().ToDictionary(af => af.ToTsvString());
            var h2a = header.Split('\t').Select(c => helperReverce[c]).ToList();
            return h2a;
        }
    }
}