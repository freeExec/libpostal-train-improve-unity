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

        [SerializeField] Button _buttonDelete = default;
        [SerializeField] Button _buttonRefresh = default;
        [SerializeField] Button _buttonNext = default;
        [SerializeField] Button _buttonDump = default;
        [SerializeField] Button _buttonDumpReady = default;
        [SerializeField] Button _buttonTestLibpostal = default;
        [SerializeField] Button _buttonInsertSpace = default;

        [SerializeField] Toggle _useNextRecord = default;
        [SerializeField] Toggle _useLongestRecord = default;
        [SerializeField] Toggle _useSortAddrRecord = default;
        [SerializeField] Toggle _useRandomRecord = default;

        [SerializeField] GameObject _waiterView = default;

        [SerializeField] EditComponentWindow _editComponentWindow = default;
        [SerializeField] TextMeshProUGUI _counter = default;

        private PreTrainDataReader dataReader;
        private LibpostalNormalizeOptions optExpand;
        private LibpostalAddressParserOptions parseOpt;
        private List<AddressFormatter> headerOrder;

        private ComponentsGroup _componentsGroup;
        private string _currentLine;

        private bool Waiting
        {
            get { return _waiterView.activeSelf; }
            set { _waiterView.SetActive(value); }
        }

        async void Start()
        {
            Waiting = true;
            await System.Threading.Tasks.Task.Run(() => dataReader = new PreTrainDataReader(Application.streamingAssetsPath));

            headerOrder = HeaderToAddress(dataReader.Header);
            //var currentLine = dataReader.GetNextRecord();   // headers

            _buttonDelete.onClick.AddListener(OnDeleteRecord);
            _buttonNext.onClick.AddListener(OnNextAddress);
            _buttonDump.onClick.AddListener(DumpProgress);
            _buttonRefresh.onClick.AddListener(OnRefreshAddress);
            _buttonDumpReady.onClick.AddListener(DumpReadyProgress);
            _buttonTestLibpostal.onClick.AddListener(TestOutOnLibpostal);
            _buttonInsertSpace.onClick.AddListener(OnInsertSpace);

            _trashDrop.OnDropAddressComponent += (component) => component.SetEmpty();
            _libpostalParseDrop.OnDropAddressComponent += (component) => ShowLibpostalParse(component.Element.Value);
            _editComponentDrop.OnDropAddressComponent += OnEditComponentBegin;

            var dataPath = Path.Combine(Application.streamingAssetsPath, "Libpostal");
            bool a = libpostal.LibpostalSetupDatadir(dataPath);
            bool b = libpostal.LibpostalSetupLanguageClassifierDatadir(dataPath);
            bool c = libpostal.LibpostalSetupParserDatadir(dataPath);

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

            parseOpt = new LibpostalAddressParserOptions();

            ShowNextAddress();
            Waiting = false;
        }

        private void OnDestroy()
        {
            // Teardown (only called once at the end of your program)
            libpostal.LibpostalTeardown();
            libpostal.LibpostalTeardownParser();
            libpostal.LibpostalTeardownLanguageClassifier();
        }

        private async void OnDeleteRecord()
        {
            Waiting = true;
            await System.Threading.Tasks.Task.Run(() => dataReader.DeleteCurrentRecord());
            ShowNextAddress();
            _buttonDump.interactable = true;
            Waiting = false;
        }

        private void SaveAddress(AddressRecord record)
        {
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
            {
                if (_useLongestRecord.isOn)
                    _currentLine = dataReader.GetNextRecordByLong();
                else if (_useSortAddrRecord.isOn)
                    _currentLine = dataReader.GetNextRecordBySortAddr();
                else if (_useRandomRecord.isOn)
                    _currentLine = dataReader.GetNextRecordByRandom();
                else
                    _currentLine = dataReader.GetNextRecord();
            }

            var addressComponents = _currentLine
                .Split(SPLIT_SEPATARE)
                .Zip(tsvAddressView.AddressColumns, (value, address) =>
                    new ElementModel(address, value, ElementSource.PreparePythonScript));
            tsvAddressView.Setup(addressComponents);

            ShowLibpostalParse(_currentLine);

            outAddressView.Setup(tsvAddressView.Elements.Where(e => !e.IsEmpty));

            _counter.text = $"Completed: {dataReader.CompletedLines}/{dataReader.TotalLines} ({(dataReader.CompletedLines / (float)dataReader.TotalLines).ToString("P4")}) | {dataReader.CurrentLine} | {_currentLine.Length}";
        }

        private void OnRefreshAddress()
        {
            ShowNextAddress(false);
        }

        private async void DumpProgress()
        {
            Waiting = true;
            await System.Threading.Tasks.Task.Run(() => dataReader.SaveTsvPreTrainData());
            Debug.Log("Saved");
            _buttonDump.interactable = false;
            Waiting = false;
        }

        private void DumpReadyProgress()
        {
            dataReader.SaveTsvOnlyCompletePreTrainData();
            Debug.Log("Saved Ready");
            _buttonDump.interactable = false;
        }

        private void TestOutOnLibpostal()
        {
            var elementsMap = outAddressView.Elements.ToLookup(e => e.Group);

            var addrStr = string.Join(" ", headerOrder.Select(h => string.Join(" ", elementsMap[h].Select(e => e.Value))));
            ShowLibpostalParse(addrStr);
        }

        private void ShowLibpostalParse(string addrStr)
        {
            var parse = libpostal.LibpostalParseAddress(addrStr.Trim(), parseOpt);

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

        (AddressFormatter AddressFormatter, string[] Replaces)[] _replacesHelperToInserSpace = new (AddressFormatter AddressFormatter, string[] Replaces)[]
        {
                ( AddressFormatter.Road, new string[] { "ул." } ),
                ( AddressFormatter.HouseNumber, new string[] { "д." } ),
                ( AddressFormatter.HouseNumber, new string[] { "лит." } ),
                ( AddressFormatter.Unit, new string[] { "пом." } ),
        };

        private void OnInsertSpace()
        {
            var elements = outAddressView.Elements.ToList();
            
            foreach (var tuple in _replacesHelperToInserSpace)
            {
                var fixElement = elements.FirstOrDefault(e => e.Group == tuple.AddressFormatter);
                if (fixElement == null)
                    continue;
                foreach (var replace in tuple.Replaces)
                {
                    int pos = fixElement.Value.IndexOf(replace);
                    int posToInsert = pos + replace.Length;
                    if (pos != -1 && fixElement.Value[posToInsert] != ' ')
                    {
                        elements.Remove(fixElement);
                        fixElement = new ElementModel(tuple.AddressFormatter, fixElement.Value.Insert(posToInsert, " "), ElementSource.ManualUserSeparate);
                        elements.Add(fixElement);
                    }
                }
            }

            outAddressView.Setup(elements);
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