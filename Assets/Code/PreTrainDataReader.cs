using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Data
{
    internal class PreTrainDataReader
    {
        private const string PreTrainFileName = "license_separate_addresses.tsv";
        private const string CompleteBitMapFileName = "btimap.dat";

        private readonly string _completeBitMapFilePath;
        private readonly string _preTrainDataFilePath;

        private StreamReader _reader;
        private BitMap _bitMap;

        private string[] _originalLines;
        private int _currentLineIndex;

        public PreTrainDataReader(string StorePath)
        {
            _completeBitMapFilePath = Path.Combine(StorePath, CompleteBitMapFileName);
            _preTrainDataFilePath = Path.Combine(StorePath, PreTrainFileName);
            _currentLineIndex = 0;

            ReadTsvPreTrainData();
        }

        private void ReadTsvPreTrainData()
        {
            _originalLines = File.ReadAllLines(_preTrainDataFilePath);

            if (File.Exists(_completeBitMapFilePath))
            {
                using (var fBitMap = new FileStream(_completeBitMapFilePath, FileMode.Open, FileAccess.Read))
                {
                    _bitMap = BitMap.FromStrea(fBitMap);
                    if (_bitMap.Length != _originalLines.Length)
                        _bitMap = default;
                }
            }

            if (_bitMap == default)
            {
                _bitMap = new BitMap(_originalLines.Length);
            }
        }

        public void SaveTsvPreTrainData()
        {
            File.WriteAllLines(_preTrainDataFilePath, _originalLines);
            using (var fBitMap = new FileStream(_completeBitMapFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                _bitMap.Save(fBitMap);
            }
        }

        public void SetRecord(string line)
        {
            MarkRecordOk();
            _originalLines[_currentLineIndex] = line;
        }

        public void MarkRecordOk()
        {
            _bitMap[_currentLineIndex] = true;
        }

        public string GetNextRecord()
        {
            while (_currentLineIndex < _bitMap.Length)
            {
                _currentLineIndex++;
                if (_bitMap[_currentLineIndex])
                    continue;

                return _originalLines[_currentLineIndex];
            }

            return string.Empty;
        }
    }
}
