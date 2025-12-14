using LibPostalNet;
using LP.Data;
using LP.Model;
using LP.UI.HistoryAddrComparer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public class SegregateWindow : MonoBehaviour
    {
        private const int WARNING_NEED_DUMP = 15;
        private const int ITER_NEXT_RECORD_PER_FRAME = 250;

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
        [SerializeField] Button _buttonTestAll = default;

        [Header("Toggles")]
        [SerializeField] Toggle _useNextRecord = default;
        [SerializeField] Toggle _useLongestRecord = default;
        [SerializeField] Toggle _useSortAddrRecord = default;
        [SerializeField] Toggle _useRandomRecord = default;

        [SerializeField] Toggle _useNextMathRecord = default;
        [SerializeField] Toggle _useNextDifferentRecord = default;

        [SerializeField] Toggle _autoApprovalRecord = default;

        [SerializeField] GameObject _waiterView = default;
        [SerializeField] BlockerWithCancel _waitWithCancel = default;

        [SerializeField] EditComponentWindow _editComponentWindow = default;

        [Header("Labels")]
        [SerializeField] TextMeshProUGUI _counter = default;

        [SerializeField] HistoryAddrComparerPanel _historyAddrComparer = default;

        [SerializeField] TextMeshProUGUI _proccessedCountLabel = default;
        [SerializeField] TextMeshProUGUI _removedCountLabel = default;

        [SerializeField] MessageWindow _messageWindow = default;

        [SerializeField] TMP_Dropdown _selectFile = default;

        [SerializeField] DefaultCopySelector _copySelector = default;

        [SerializeField] TestAllRecordsButton _testAllRecords = default;

        [Header("Colours")]
        [SerializeField] Color _warningColor = Color.red;

        private ComponentsGroup _componentsGroup;
        private LPRecord _currentLPRecord;

        private int _proccessedCount = 0;
        private int _removedCount = 0;
        private Color _buttonDumpNormalColor;
        //private Color _buttonDumpHoverColor;

        private Color _buttonDeleteNormalColor;
        private Color _buttonDeleteHoverColor;

        private List<KeyValuePair<AddressFormatter, HashSet<string>>> prevExpandedAddrSet;
        private List<KeyValuePair<AddressFormatter, HashSet<string>>> currentExpandedAddrSet;

        private Coroutine _coroAutoApproval;

        private bool Waiting
        {
            get { return _waiterView.activeSelf; }
            set { _waiterView.SetActive(value); }
        }

        private void Start()
        {
            Application.targetFrameRate = 30;

            _selectFile.onValueChanged.AddListener(OnFileSelectedHandler);
            SelectFileFill();

            _buttonDelete.onClick.AddListener(OnDeleteRecord);
            _buttonNext.onClick.AddListener(OnNextAddress);
            _buttonDump.onClick.AddListener(DumpProgress);
            _buttonRefresh.onClick.AddListener(OnRefreshAddress);
            _buttonDumpReady.onClick.AddListener(DumpReadyProgress);
            _buttonTestLibpostal.onClick.AddListener(TestOutOnLibpostal);
            _buttonInsertSpace.onClick.AddListener(OnInsertSpaceAndTrim);
            _buttonTestAll.onClick.AddListener(OnTestAllLines);

            _trashDrop.OnDropAddressComponent += (component) => component.SetEmpty();
            _libpostalParseDrop.OnDropAddressComponent += OnDropCustomElement; // (component) => ShowLibpostalParse(component.Element.Value, false, false);
            _editComponentDrop.OnDropAddressComponent += OnEditComponentBegin;

            _buttonDumpNormalColor = _buttonDump.colors.normalColor;
            _buttonDeleteNormalColor = _buttonDelete.colors.normalColor;
            _buttonDeleteHoverColor = _buttonDelete.colors.highlightedColor;

            if (_coreProcess.IsLibpostalSetupSuccessful == false)
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
            if (_proccessedCount + _removedCount >= WARNING_NEED_DUMP)
            {
                _messageWindow.OnFinish += RequestSaveRecord;
                _messageWindow.Setup("More unsaved records. Ok Save. (X) Discard.");
                _messageWindow.gameObject.SetActive(true);
                _proccessedCount = 0;
                _removedCount = 0;
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

            SetNextAddress();
            ShowCurrentAddress();
            Waiting = false;
        }

        private void OnDeleteRecord()
        {
            _coreProcess.DeleteCurrentRecord();
            _removedCount++;
            SetNextAddress();
            ShowCurrentAddress();
            _buttonDump.interactable = true;

            if (_proccessedCount + _removedCount == WARNING_NEED_DUMP)
            {
                ReplaceButtonNormalColor(_buttonDump, _warningColor);
            }
        }

        private void SaveAddress(AddressRecord record)
        {
            var elementsMap = record.Elements.ToLookup(e => e.Group);
            if (record.Elements.All(e => e.IsEmpty))
                return;

            var row = string.Join("\t", _coreProcess.HeaderOrder.Select(h => string.Join(" ", elementsMap[h].Select(e => e.Value))));

            _coreProcess.SetRecord(row);

            _proccessedCount++;
        }

        private void OnNextAddress()
        {
            if (_autoApprovalRecord.isOn)
            {
                _autoApprovalRecord.isOn = false;
                _coroAutoApproval = StartCoroutine(OnAutoApprovalNextRecord());
                return;
            }

            if (_coroAutoApproval is not null)
            {
                StopCoroutine(_coroAutoApproval);
                _coroAutoApproval = null;
            }

            if (!outAddressView.IsEmpty)
                SaveAddress(outAddressView);
            else
                SaveAddress(tsvAddressView);

            SetNextAddress();
            ShowCurrentAddress();

            _buttonDump.interactable = true;

            if (_proccessedCount + _removedCount == WARNING_NEED_DUMP)
            {
                ReplaceButtonNormalColor(_buttonDump, _warningColor);
            }
        }

        private IEnumerator OnAutoApprovalNextRecord()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            _waitWithCancel.Show(cts);

            bool isMatch = false;
            do
            {
                //SetNextAddress(_coreProcess.HeaderOrder); // Mode = NextMatch
                int tryFindCounter = 1;
                do
                {
                    _currentLPRecord = _coreProcess.GetNextRecord();
                    if (_currentLPRecord.IsEmpty) break;
                    isMatch = IsMatchTsvAndLibpostal(_currentLPRecord, _coreProcess.HeaderOrder);

                    if (tryFindCounter % ITER_NEXT_RECORD_PER_FRAME == 0)
                        yield return null;

                    if (cts.IsCancellationRequested)
                        break;

                    tryFindCounter++;
                } while (!isMatch);

                if (cts.IsCancellationRequested)
                    break;

                if (_currentLPRecord.IsEmpty)
                    break;

                if (isMatch)
                {
                    ShowCurrentAddress();
                    SaveAddress(tsvAddressView);
                }

                if (_proccessedCount % ITER_NEXT_RECORD_PER_FRAME == 0)
                    yield return null;

            } while (isMatch);

            ShowCurrentAddress();

            _buttonDump.interactable = true;

            if (_proccessedCount + _removedCount == WARNING_NEED_DUMP)
            {
                ReplaceButtonNormalColor(_buttonDump, _warningColor);
            }

            _waitWithCancel.Hide();
        }

        private void SetNextAddress()
        {
            if (_useLongestRecord.isOn)
                _currentLPRecord = _coreProcess.GetNextRecordByLong();
            else if (_useSortAddrRecord.isOn)
                _currentLPRecord = _coreProcess.GetNextRecordBySortAddr();
            else if (_useRandomRecord.isOn)
                _currentLPRecord = _coreProcess.GetNextRecordByRandom();
            else if (_useNextMathRecord.isOn)
            {
                bool isMatch = false;
                do
                {
                    _currentLPRecord = _coreProcess.GetNextRecord();
                    if (_currentLPRecord.IsEmpty) break;
                    isMatch = IsMatchTsvAndLibpostal(_currentLPRecord, _coreProcess.HeaderOrder);
                } while (!isMatch);
            }
            else if (_useNextDifferentRecord.isOn)
            {
                bool isMatch = false;
                do
                {
                    _currentLPRecord = _coreProcess.GetNextRecord();
                    if (_currentLPRecord.IsEmpty) break;
                    isMatch = IsMatchTsvAndLibpostal(_currentLPRecord, _coreProcess.HeaderOrder);
                } while (isMatch);
            }
            else
                _currentLPRecord = _coreProcess.GetNextRecord();
        }

        private static ElementModelMatchComparer ElementModelMatchComparer = new ElementModelMatchComparer();
        private static bool IsMatchTsvAndLibpostal(LPRecord record, AddressFormatter[] headerOrder)
        {
            if (string.IsNullOrEmpty(record.Line)) return false;
            var trueComponents = FillComponents(record.Line.Replace(LPRecord.LP_SEPATARE_SEMI, LPRecord.LP_SEPATARE_SPACE), headerOrder);
            var libpostalComponents = record.ParseResultEnum.Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal));
            return trueComponents.Where(c => !c.IsEmpty).SequenceEqual(libpostalComponents, ElementModelMatchComparer);
        }

        private static IEnumerable<ElementModel> FillComponents(string addrString, AddressFormatter[] addressColumns, ElementSource source = ElementSource.PreparePythonScript) =>
            addrString
                .Split(LPRecord.SPLIT_SEPATARE_TAB)
                .Zip(addressColumns, (value, address) =>
                    new ElementModel(address, value, ElementSource.PreparePythonScript));

        private void ShowCurrentAddress(bool saveNormAddr = true)
        {
            var addressComponents = FillComponents(_currentLPRecord.Line, /*tsvAddressView.AddressColumns*/_coreProcess.HeaderOrder);
            tsvAddressView.Setup(addressComponents);

            postalAddressView.Setup(_currentLPRecord.ParseResultEnum.Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal)));
            SetNormAddr(saveNormAddr);
            //SetSimirarStatus();

            if (_copySelector.CopySelector == CopySelector.Source)
                outAddressView.Setup(tsvAddressView.Elements.Where(e => !e.IsEmpty));
            else
                outAddressView.Setup(postalAddressView.Elements.Where(e => !e.IsEmpty));

            _counter.text = $"Completed: {_coreProcess.CompletedLines}/{_coreProcess.TotalLines} ({_coreProcess.CompletedLines / (float)_coreProcess.TotalLines:P4}) | {_coreProcess.CurrentLineIndex} | {_currentLPRecord.Line.Length}";

            _proccessedCountLabel.text = $"+{_proccessedCount}";
            _removedCountLabel.text = $"-{_removedCount}";
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

            ReplaceButtonNormalColor(_buttonDump, _buttonDumpNormalColor);

            _proccessedCount = 0;
            _removedCount = 0;

            SetNextAddress();
            ShowCurrentAddress(false);
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
            var lpRecord = new LPRecord(_currentLPRecord.LineIndex, addrStr);
            postalAddressView.Setup(lpRecord.ParseResultEnum.Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal)));
            SetNormAddr(false);
        }

        private void OnDropCustomElement(AddressComponent component)
        {
            _currentLPRecord = new LPRecord(_currentLPRecord.LineIndex, component.Element.Value);
            postalAddressView.Setup(_currentLPRecord.ParseResultEnum.Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal)));
        }

        /*private void SetSimirarStatus()
        {
            bool similar = prevExpandedAddr is not null && prevExpandedAddr.Overlaps(currentExpandedAddr);
            ReplaceButtonNormalColor(_buttonDelete, similar ? _warningColor : _buttonDeleteNormalColor);
        }*/

        private void SetNormAddr(bool saveNormAddr)
        {
            if (saveNormAddr)
            {
                prevExpandedAddrSet = currentExpandedAddrSet;
            }

            currentExpandedAddrSet = _currentLPRecord.ExpandedAddressIndividualSet;

            bool similar = _historyAddrComparer.Setup(currentExpandedAddrSet, prevExpandedAddrSet);
            ReplaceButtonNormalColor(_buttonDelete, similar ? _warningColor : _buttonDeleteNormalColor);
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

        private void OnTestAllLines()
        {
            _testAllRecords.ClearProgress();
            var filename = _selectFile.options[_selectFile.value].text;
            _testAllRecords.Process(_coreProcess.ValidateDataPath, filename);
            StartCoroutine(WaitTestAllLineCoro());
        }

        private IEnumerator WaitTestAllLineCoro()
        {
            var cts = new CancellationTokenSource();
            _waitWithCancel.Show(cts);
            while (true)
            {
                yield return new WaitForSeconds(0.3f);
                
                if (cts.IsCancellationRequested)
                    _testAllRecords.ProcessStop();
                
                if (_testAllRecords.IsProcessing == false)
                    break;
            }
            _waitWithCancel.Hide();
        }

        private static void ReplaceButtonNormalColor(Button button, Color colorNormal)
        {
            var colors = button.colors;
            colors.normalColor = colorNormal;

            Color.RGBToHSV(colorNormal, out  var h, out var s, out var v);
            v = v * 0.8f;
            colors.highlightedColor = Color.HSVToRGB(h, s,  v);

            button.colors = colors;
        }
    }
}