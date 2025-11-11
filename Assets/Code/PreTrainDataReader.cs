using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Profiling;

namespace LP.Data
{
    /*internal class KV<K, V>
    {
        public K Key;
        public V Value;

        public KV(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }*/

    internal class PreTrainDataReader
    {
        private enum SortType
        {
            Default,
            Longest,
            StreetHouse,

            Count
        }

        private class SortState<T>
        {
            public int CurrentIndex;
            public List<KeyValuePair<T, int>> OrderLines;

            public SortState(int capacity)
            {
                CurrentIndex = -1;
                OrderLines = new List<KeyValuePair<T, int>>(capacity);
            }
        }

        private const string PreTrainFileName = "license_separate_addresses.tsv";
        private const string CompletePreTrainFileName = "license_separate_addresses_complete.tsv";
        private const string CompleteBitMapFileName = "bitmap.dat";

        private readonly string _completeBitMapFilePath;
        private readonly string _preTrainDataFilePath;
        private readonly string _completePreTrainDataFilePath;

        private StreamReader _reader;
        private BitMap _bitMap;

        private int _currentOriginalIndex;
        private string[] _originalLines;

        private SortState<int> _sortLongestStates;
        private SortState<string> _sortAddrStates;

        private bool _hasDeletedRecord;

        public string Header => _originalLines[0];
        public int CompletedLines { get; private set; }
        public int TotalLines => _originalLines.Length;
        public int CurrentLine => _currentOriginalIndex;

        public PreTrainDataReader(string storePath)
        {
            _completeBitMapFilePath = Path.Combine(storePath, CompleteBitMapFileName);
            _preTrainDataFilePath = Path.Combine(storePath, PreTrainFileName);
            _completePreTrainDataFilePath = Path.Combine(storePath, CompletePreTrainFileName);
            //_currentIndex = -1;

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
                _bitMap[0] = true;  // header
            //TestRemoveDublicateMap();
            CleanAndPrepare();
            //TestRemoveDublicateMap();
        }

        private void CleanAndPrepare()
        {
            Profiler.BeginSample("CleanAndPrepare");
            RemoveDublicate(ref _originalLines, ref _bitMap);

            Profiler.BeginSample("GetMappedCount");
            CompletedLines = _bitMap.GetMappedCount();
            Profiler.EndSample();

            Profiler.BeginSample("OrderByLongLines");
            _sortLongestStates = new SortState<int>(_originalLines.Length);
            _sortAddrStates = new SortState<string>(_originalLines.Length);

            int streetIndex = 5;
            int houseIndex = 6;
            //char[] splitTab = new char[] { '\t' };
            for (int i = 1; i < _originalLines.Length; i++)
            {
                _sortLongestStates.OrderLines.Add(new KeyValuePair<int, int>(_originalLines[i].Length, i));

                int cp = -1;
                int ci = 0;
                int s = 0, e = 0;
                do
                {
                    cp = _originalLines[i].IndexOf('\t', cp + 1);

                    if (ci == streetIndex - 1)
                        s = cp + 1;
                    else if (ci == houseIndex)
                        e = cp;

                    ci++;
                } while (cp != -1);
                _sortAddrStates.OrderLines.Add(new KeyValuePair<string, int>(_originalLines[i].Substring(s, e - s), i));
            }
            _sortLongestStates.OrderLines.Sort((t1, t2) => t2.Key.CompareTo(t1.Key));
            _sortAddrStates.OrderLines.Sort((t1, t2) => string.CompareOrdinal(t1.Key, t2.Key));
            Profiler.EndSample();
            _hasDeletedRecord = false;
            Profiler.EndSample();
        }

        private static void RemoveDublicate(ref string[] lines, ref BitMap bitMap)
        {
            Profiler.BeginSample("RemoveDublicate");
            var newLines = new List<string>(lines.Length);
            var hashSetLow = new Dictionary<string, object>(lines.Length);
            var newBitMap = new BitMap(bitMap.Length);

            int columnsHeader = lines[0].Split('\t').Length;

            for (int i = 0, n = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (string.IsNullOrEmpty(line))
                    continue;

                var lineLow = line.ToLowerInvariant();
                //var columns = line.Split('\t');

                //if (columns.Length != columnsHeader)
                //{
                //    UnityEngine.Debug.LogWarning($"lose column\n{line}");
                //}

                Profiler.BeginSample("Contains");
                if (!hashSetLow.ContainsKey(lineLow))
                {
                    Profiler.BeginSample("Add");
                    newLines.Add(line);
                    hashSetLow.Add(lineLow, null);
                    newBitMap[n] = bitMap[i];
                    n++;
                    Profiler.EndSample();
                }
                else
                {
                    UnityEngine.Debug.Log($"dublicate\n{line}");
                }
                Profiler.EndSample();
            }
            int removeLines = lines.Length - newLines.Count;

            lines = newLines.ToArray();
            bitMap = BitMap.Trim(newBitMap, lines.Length);

            Profiler.EndSample();
            UnityEngine.Debug.Log($"Remove lines: {removeLines}");
        }

        public void SaveTsvPreTrainData()
        {
            if (_hasDeletedRecord)
                CleanAndPrepare();
            File.WriteAllLines(_preTrainDataFilePath, _originalLines);
            using (var fBitMap = new FileStream(_completeBitMapFilePath, FileMode.Create, FileAccess.Write))
            {
                _bitMap.Save(fBitMap);
            }
        }

        public void SaveTsvOnlyCompletePreTrainData()
        {
            var completedLines = new List<string>(_originalLines.Length);
            completedLines.Add(_originalLines[0]);  // header
            for (int i = 1; i < _originalLines.Length; i++)
            {
                if (!_bitMap[i])
                    continue;

                completedLines.Add(_originalLines[i]);
            }

            File.WriteAllLines(_completePreTrainDataFilePath, completedLines);
        }

        public void SetRecord(string line)
        {
            MarkRecordOk();
            _originalLines[_currentOriginalIndex] = line;
        }

        public void DeleteCurrentRecord()
        {
            _originalLines[_currentOriginalIndex] = string.Empty;
            _hasDeletedRecord = true;
        }

        public void MarkRecordOk()
        {
            _bitMap[_currentOriginalIndex] = true;
            CompletedLines++;
        }

        public string GetNextRecord()
        {
            int index = _currentOriginalIndex;
            while (index < _bitMap.Length)
            {
                index++;
                if (_bitMap[index])
                    continue;

                _currentOriginalIndex = index;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }
        public string GetNextRecordByRandom()
        {
            int index = UnityEngine.Random.Range(0, _originalLines.Length);
            while (index < _bitMap.Length)
            {
                index++;
                if (_bitMap[index])
                    continue;

                _currentOriginalIndex = index;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }

        public string GetNextRecordByLong()
        {
            int index = _sortLongestStates.CurrentIndex;
            while (index < _bitMap.Length)
            {
                index++;
                int originalIndex = _sortLongestStates.OrderLines[index].Value;
                if (_bitMap[originalIndex])
                    continue;

                _sortLongestStates.CurrentIndex = index;
                _currentOriginalIndex = originalIndex;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }

        public string GetNextRecordBySortAddr()
        {
            int index = _sortAddrStates.CurrentIndex;
            while (index < _bitMap.Length)
            {
                index++;
                int originalIndex = _sortAddrStates.OrderLines[index].Value;
                if (_bitMap[originalIndex])
                    continue;

                _sortAddrStates.CurrentIndex = index;
                _currentOriginalIndex = originalIndex;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }

        private void TestRemoveDublicateMap()
        {
            UnityEngine.Debug.Log($"VVVVVVV");
            for (int i = 1; i < _originalLines.Length; i++)
            {
                if (!_bitMap[i])
                    continue;

                UnityEngine.Debug.Log($"Check: {i} => {_originalLines[i]}");
            }
        }
    }
}
