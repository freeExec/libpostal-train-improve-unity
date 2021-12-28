﻿using System;
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
        private int _currentIndex;
        private int _currentOriginalIndex;

        private List<KeyValuePair<int, int>> _orderByLongLines;

        public PreTrainDataReader(string StorePath)
        {
            _completeBitMapFilePath = Path.Combine(StorePath, CompleteBitMapFileName);
            _preTrainDataFilePath = Path.Combine(StorePath, PreTrainFileName);
            _currentIndex = -1;

            ReadTsvPreTrainData();
        }

        private void ReadTsvPreTrainData()
        {
            _originalLines = File.ReadAllLines(_preTrainDataFilePath);

            _orderByLongLines = new List<KeyValuePair<int, int>>();
            for (int i = 0; i < _originalLines.Length; i++)
            {
                _orderByLongLines.Add(new KeyValuePair<int, int>(_originalLines[i].Length, i));
            }
            _orderByLongLines.Sort((t1, t2) => t2.Key.CompareTo(t1.Key));


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
            _originalLines[_currentOriginalIndex] = line;
        }

        public void MarkRecordOk()
        {
            _bitMap[_currentOriginalIndex] = true;
        }

        public string GetNextRecord()
        {
            int index = _currentIndex;
            while (index < _bitMap.Length)
            {
                index++;
                if (_bitMap[index])
                    continue;

                _currentIndex = index;
                _currentOriginalIndex = index;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }

        public string GetNextRecordByLong()
        {
            int index = _currentIndex;
            while (index < _bitMap.Length)
            {
                index++;
                int originalIndex = _orderByLongLines[index].Value;
                if (_bitMap[originalIndex])
                    continue;

                _currentIndex = index;
                _currentOriginalIndex = originalIndex;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }
    }
}