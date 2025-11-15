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
using System.Threading.Tasks;

namespace LP.UI
{
    public class SegregateWindow : MonoBehaviour
    {
        private const int WARNING_NEED_DUMP = 15;

        [SerializeField] CoreProcess _coreProcess = default;

        [Header("Adresses")]
        [SerializeField] AddressRecord tsvAddressView = default;
        [SerializeField] AddressRecord postalAddressView = default;
        [SerializeField] AddressRecord outAddressView = default;

        [Header("Dropper Box")]
        [SerializeField] DroperBox _trashDrop = default;
        [SerializeField] DroperBox _libpostalParseDrop = default;
        [SerializeField] DroperBox _editComponentDrop = default;

        [Header("Buttons")]
        [SerializeField] Button _buttonDelete = default;
        [SerializeField] Button _buttonRefresh = default;
        [SerializeField] Button _buttonNext = default;
        [SerializeField] Button _buttonDump = default;
        [SerializeField] Button _buttonDumpReady = default;
        [SerializeField] Button _buttonTestLibpostal = default;
        [SerializeField] Button _buttonInsertSpace = default;

        [Header("Toggles")]
        [SerializeField] Toggle _useNextRecord = default;
        [SerializeField] Toggle _useLongestRecord = default;
        [SerializeField] Toggle _useSortAddrRecord = default;
        [SerializeField] Toggle _useRandomRecord = default;

        [SerializeField] Toggle _useNextMathRecord = default;
        [SerializeField] Toggle _useNextDifferentRecord = default;

        [SerializeField] GameObject _waiterView = default;

        [SerializeField] EditComponentWindow _editComponentWindow = default;

        [Header("Labels")]
        [SerializeField] TextMeshProUGUI _counter = default;
        [SerializeField] TextMeshProUGUI _normAddr = default;
        [SerializeField] TextMeshProUGUI _lastNormAddr = default;

        [SerializeField] MessageWindow _messageWindow = default;

        [SerializeField] TMP_Dropdown _selectFile = default;

        [SerializeField] DefaultCopySelector _copySelector = default;

        [Header("Colours")]
        [SerializeField] Color _warningColor = Color.red;

        private ComponentsGroup _componentsGroup;
        private LPRecord _currentLPRecord;

        private int _proccessedCount = 0;
        private Color _buttonDumpNormalColor;
        private Color _buttonDeleteNormalColor;
        private Color _buttonDeleteHoverColor;

        private HashSet<string> prevExpandedAddr;
        private HashSet<string> currentExpandedAddr;


        private bool Waiting
        {
            get { return _waiterView.activeSelf; }
            set { _waiterView.SetActive(value); }
        }

        private void Start()
        {
            Application.targetFrameRate = 15;

            _selectFile.onValueChanged.AddListener(OnFileSelectedHandler);
            SelectFileFill();

            _buttonDelete.onClick.AddListener(OnDeleteRecord);
            _buttonNext.onClick.AddListener(OnNextAddress);
            _buttonDump.onClick.AddListener(DumpProgress);
            _buttonRefresh.onClick.AddListener(OnRefreshAddress);
            _buttonDumpReady.onClick.AddListener(DumpReadyProgress);
            _buttonTestLibpostal.onClick.AddListener(TestOutOnLibpostal);
            _buttonInsertSpace.onClick.AddListener(OnInsertSpaceAndTrim);

            _trashDrop.OnDropAddressComponent += (component) => component.SetEmpty();
            _libpostalParseDrop.OnDropAddressComponent += (component) => ShowLibpostalParse(component.Element.Value, false, false);
            _editComponentDrop.OnDropAddressComponent += OnEditComponentBegin;

            _buttonDumpNormalColor = _buttonDump.colors.normalColor;
            _buttonDeleteNormalColor = _buttonDelete.colors.normalColor;
            _buttonDeleteHoverColor = _buttonDelete.colors.highlightedColor;

            if (!_coreProcess.Setup())
            {
                _messageWindow.Setup("Libpostal Init FAIL!");
                _messageWindow.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
        }

        const string FILE_EXT_TSV = ".tsv";
        const string FILE_MASK_TSV = "*" + FILE_EXT_TSV;
        const string FILE_COMPLITED_SUFFIX = "_complete";
        const string FILE_COMPLITED_TSV = FILE_COMPLITED_SUFFIX + FILE_EXT_TSV;

        private void SelectFileFill()
        {
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var fileTsv in Directory.EnumerateFiles(_coreProcess.ValidateDataPath, FILE_MASK_TSV))
            {
                if (fileTsv.EndsWith(FILE_COMPLITED_TSV)) continue;
                options.Add(new TMP_Dropdown.OptionData(Path.GetFileNameWithoutExtension(fileTsv)));
            }
            _selectFile.AddOptions(options);
            _selectFile.value = 0;
        }

        private void OnFileSelectedHandler(int index)
        {
            if (_proccessedCount >= WARNING_NEED_DUMP)
            {
                _messageWindow.OnFinish += RequestSaveRecord;
                _messageWindow.Setup("More unsaved records. Ok Save. (X) Discard.");
                _messageWindow.gameObject.SetActive(true);
                _proccessedCount = 0;
                return;
            }

            LoadSelectedFileAsync();
        }

        private void RequestSaveRecord(MessageBoxAnswer answer)
        {
            _messageWindow.OnFinish -= RequestSaveRecord;
            if (answer == MessageBoxAnswer.Ok)
            {
                DumpProgress();
            }

            LoadSelectedFileAsync();
        }

        private async void LoadSelectedFileAsync()
        {
            var filename = _selectFile.options[_selectFile.value].text;

            Waiting = true;
            await _coreProcess.LoadFileAsync(filename);

            SetNextAddress(tsvAddressView);
            ShowCurrentAddress();
            Waiting = false;
        }

        private void OnDeleteRecord()
        {
            _coreProcess.DeleteCurrentRecord();
            SetNextAddress(tsvAddressView);
            ShowCurrentAddress();
            _buttonDump.interactable = true;
        }

        private void SaveAddress(AddressRecord record)
        {
            var elementsMap = record.Elements.ToLookup(e => e.Group);
            if (record.Elements.All(e => e.IsEmpty))
                return;

            var row = string.Join("\t", _coreProcess.HeaderOrder.Select(h => string.Join(" ", elementsMap[h].Select(e => e.Value))));

            _coreProcess.SetRecord(row);
        }

        private void OnNextAddress()
        {
            if (!outAddressView.IsEmpty)
                SaveAddress(outAddressView);
            else
                SaveAddress(tsvAddressView);

            SetNextAddress(tsvAddressView);
            ShowCurrentAddress();

            _buttonDump.interactable = true;

            if (_proccessedCount == WARNING_NEED_DUMP)
            {
                ReplaceButtonNormalColor(_buttonDump, _warningColor, Color.black);
            }
            _proccessedCount++;
        }

        private void SetNextAddress(AddressRecord addressView)
        {
            if (_useLongestRecord.isOn)
                _currentLPRecord = _coreProcess.GetNextRecordByLong();
            else if (_useSortAddrRecord.isOn)
                _currentLPRecord = _coreProcess.GetNextRecordBySortAddr();
            else if (_useRandomRecord.isOn)
                _currentLPRecord = _coreProcess.GetNextRecordByRandom();
            else if (_useNextMathRecord.isOn)
            {
                bool isMath = false;
                var comparer = new ElementModelMatchComparer();
                do
                {
                    _currentLPRecord = _coreProcess.GetNextRecord();
                    if (string.IsNullOrEmpty(_currentLPRecord.Line)) break;
                    var trueComponents = FillComponents(_currentLPRecord.Line, addressView.AddressColumns);
                    var libpostalComponents = _currentLPRecord.ConvertParseToEnum().Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal));
                    isMath = trueComponents.Where(c => !c.IsEmpty).SequenceEqual(libpostalComponents, comparer);
                } while (!isMath);
            }
            else if (_useNextDifferentRecord.isOn)
            {
                bool isMath = false;
                var comparer = new ElementModelMatchComparer();
                do
                {
                    _currentLPRecord = _coreProcess.GetNextRecord();
                    if (string.IsNullOrEmpty(_currentLPRecord.Line)) break;
                    var trueComponents = FillComponents(_currentLPRecord.Line, addressView.AddressColumns);
                    var libpostalComponents = _currentLPRecord.ConvertParseToEnum().Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal));
                    isMath = trueComponents.Where(c => !c.IsEmpty).SequenceEqual(libpostalComponents, comparer);
                } while (isMath);
            }
            else
                _currentLPRecord = _coreProcess.GetNextRecord();
        }

        private static IEnumerable<ElementModel> FillComponents(string addrString, AddressFormatter[] addressColumns, ElementSource source = ElementSource.PreparePythonScript) =>
            addrString
                .Split(SPLIT_SEPATARE)
                .Zip(addressColumns, (value, address) =>
                    new ElementModel(address, value, ElementSource.PreparePythonScript));

        private void ShowCurrentAddress(bool applyNormAddr = true)
        {
            var addressComponents = FillComponents(_currentLine, tsvAddressView.AddressColumns);
            tsvAddressView.Setup(addressComponents);

            ShowLibpostalParse(_currentLine, true, applyNormAddr);

            if (_copySelector.CopySelector == CopySelector.Source)
                outAddressView.Setup(tsvAddressView.Elements.Where(e => !e.IsEmpty));
            else
                outAddressView.Setup(postalAddressView.Elements.Where(e => !e.IsEmpty));

            _counter.text = $"Completed: {_coreProcess.CompletedLines}/{_coreProcess.TotalLines} ({_coreProcess.CompletedLines / (float)_coreProcess.TotalLines:P4}) | {_coreProcess.CurrentLineIndex} | {_currentLine.Length}";
        }

        private void OnRefreshAddress()
        {
            ShowCurrentAddress(false);
        }

        private async void DumpProgress()
        {
            Waiting = true;
            await _coreProcess.SaveTsvPreTrainDataAsync();
            _buttonDump.interactable = false;
            Waiting = false;

            ReplaceButtonNormalColor(_buttonDump, _buttonDumpNormalColor, Color.black);

            _proccessedCount = 0;
        }

        private async void DumpReadyProgress()
        {
            Waiting = true;
            await _coreProcess.SaveTsvOnlyCompletePreTrainData();
            Waiting = false;

            _buttonDump.interactable = false;
        }

        private void TestOutOnLibpostal()
        {
            var elementsMap = outAddressView.Elements.ToLookup(e => e.Group);

            var addrStr = string.Join(" ", _coreProcess.HeaderOrder.Select(h => string.Join(" ", elementsMap[h].Select(e => e.Value))));
            ShowLibpostalParse(addrStr, true, false);
        }

        /*private IEnumerable<ElementModel> ParseLibpostal(string addrStr)
        {
            var addrStrNoTab = addrStr.Replace('\t', ' ');

            var parse = libpostal.LibpostalParseAddress(addrStrNoTab.Trim(), parseOpt);

            var addrStrLow = addrStrNoTab.ToLowerInvariant().Replace(',', ' ');

            Func<string, string> recoveryCase = (libpostalAnsver) =>
            {
                int found = addrStrLow.IndexOf(libpostalAnsver);
                if (found != -1)
                {
                    return addrStr.Substring(found, libpostalAnsver.Length);
                }
                return libpostalAnsver;
            };

            var addressComponents = parse.Results
                .Select(r => new ElementModel(
                    AddressFormatterHelper.GetFormatterFromLibpostal(r.Key),
                    recoveryCase(r.Value),
                    ElementSource.Libpostal
                ));

            return addressComponents;
        }*/

        private void ShowLibpostalParse(string addrStr, bool applyNormAddr, bool saveNormAddr)
        {
            var addrStrNoTab = addrStr.Replace('\t', ' ');
            var addressComponents = ParseLibpostal(addrStrNoTab);

            postalAddressView.Setup(addressComponents);

            if (applyNormAddr)
            {
                SetNormAddr(saveNormAddr, addressComponents);
            }

            /*int levensh = -1;
            int minLength = Mathf.Min(_lastNormAddr.text.Length, _normAddr.text.Length);
            if (minLength == 0)
            { }
            else if (minLength > 25)
                levensh = EditDistance.DamerauLevenshteinDistance(_lastNormAddr.text.Substring(0, minLength), _normAddr.text.Substring(0, minLength), 8);
            else
                levensh = EditDistance.DamerauLevenshteinDistance(_lastNormAddr.text, _normAddr.text, 8);

            ReplaceButtonNormalColor(_buttonDelete, (levensh >= 0) ? _warningColor : _buttonDeleteNormalColor,
                                                    (levensh >= 0) ? _warningColor : _buttonDeleteHoverColor);*/
            bool similar = prevExpandedAddr is not null && prevExpandedAddr.Overlaps(currentExpandedAddr);
            ReplaceButtonNormalColor(_buttonDelete, similar ? _warningColor : _buttonDeleteNormalColor,
                                                    similar ? _warningColor : _buttonDeleteHoverColor);
        }

        private void SetNormAddr(bool saveNormAddr, IEnumerable<ElementModel> addressComponents)
        {
            if (saveNormAddr)
            {
                prevExpandedAddr = currentExpandedAddr;
                _lastNormAddr.text = _normAddr.text;
            }

            string expandAddrCombine = string.Empty;
            foreach (ElementModel addressComponent in addressComponents)
            {
                optExpand.AddressComponents = (ushort)addressComponent.Group.ToLibpostalAddress();
                var expandAddr = libpostal.LibpostalExpandAddress(addressComponent.Value, optExpand);

                //int maxLength = expandAddr.Expansions.Max(e  => e.Length);
                //expandAddrCombine += expandAddr.Expansions.First(e => e.Length == maxLength) + " ";
                expandAddrCombine += expandAddr.Expansions.First() + " ";
            }

            currentExpandedAddr =
                libpostal.LibpostalExpandAddress(
                    string.Join(", ", addressComponents.Where(c => c.Group <= AddressFormatter.Road).Select(c => c.Value)),
                    optExpand
                ).Expansions.ToHashSet();

            _normAddr.text = expandAddrCombine;
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

        private void OnInsertSpaceAndTrim()
        {
            outAddressView.Setup(ImproveTextTools.InsertSpaceAndTrim(outAddressView.Elements));
        }

        private static void ReplaceButtonNormalColor(Button button, Color colorNormal, Color colorHover)
        {
            var colors = button.colors;
            colors.normalColor = colorNormal;
            if (colorHover != Color.black)
                colors.highlightedColor = colorHover;
            button.colors = colors;
        }
    }
}