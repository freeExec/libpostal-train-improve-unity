using LibPostalNet;
using LP.Model;
using LP.UI;
using LP.UI.SimpleWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LP.Data
{
    public class CoreProcess : MonoBehaviour
    {
        [SerializeField] private MessageWindow _messageWindow;  // показать ошибку в EXE-билде

        public bool IsLibpostalSetupSuccessful { get; private set; }
        public AddressFormatter[] HeaderOrder { get; private set; }

        public int CompletedLines => dataReader.CompletedLines;
        public int TotalLines => dataReader.TotalLines;
        public int CurrentLineIndex => dataReader.CurrentLineIndex;

        private PreTrainDataReader dataReader;

        public string ValidateDataPath
        {
            get
            {
#if UNITY_EDITOR
                var validateDataPath = Application.dataPath + "\\..\\Data";
#else
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 2)
                {
                    _messageWindow.Setup("Set Data DIR. Exit!");
                    _messageWindow.gameObject.SetActive(true);

                    return "BAD-PATH";
                }

                var validateDataPath = args[1];
#endif
                return validateDataPath;
            }
        }

        private void Awake()
        {
            /*if (Instance != null)
            {
                Destroy(this);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(Instance);*/
            IsLibpostalSetupSuccessful = LPRecord.LibPostalSetup(Application.streamingAssetsPath);
        }

        private void OnDestroy()
        {
            LPRecord.LibPostalTeardown();
        }

        public async Task LoadFileAsync(string filename)
        {
            await Task.Run(() => dataReader = new PreTrainDataReader(ValidateDataPath, filename), destroyCancellationToken);
            HeaderOrder = AddressFormatterHelper.HeaderToAddress(dataReader.Header);
        }

        public async Task SaveTsvPreTrainDataAsync()
        {
            await Task.Run(() => dataReader.SaveTsvPreTrainData());
        }

        public async Task SaveTsvOnlyCompletePreTrainData()
        {
            await Task.Run(() => dataReader.SaveTsvOnlyCompletePreTrainData());
        }

        public LPRecord GetNextRecord() => GetParseRecord(dataReader.GetNextRecord);
        public LPRecord GetNextRecordByRandom() => GetParseRecord(dataReader.GetNextRecordByRandom);
        public LPRecord GetNextRecordBySortAddr() => GetParseRecord(dataReader.GetNextRecordBySortAddr);
        public LPRecord GetNextRecordByLong() => GetParseRecord(dataReader.GetNextRecordByLong);
        public LPRecord GetNextFromLineIndex(int lineIndex, bool isCompletedFilter)
        {
            if (isCompletedFilter)
            {
                var line = dataReader.GetNextCompletedRecord(lineIndex);
                return new LPRecord(dataReader.CurrentLineIndex, line);
            }
            else
                return new LPRecord(lineIndex, dataReader.GetNextRecord(lineIndex));
        }

        public void SetRecord(string row) => dataReader.SetRecord(row);
        public void DeleteCurrentRecord() => dataReader.DeleteCurrentRecord();

        private LPRecord GetParseRecord(Func<string> getNexrRexort)
        {
            var line = getNexrRexort();
            return new LPRecord(dataReader.CurrentLineIndex, line);
        }
    }    
}