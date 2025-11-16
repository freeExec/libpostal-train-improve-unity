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

        private const string PreTrainExtension = ".tsv";
        private const string CompletePreTrainFileName = "_complete.tsv";
        private const string CompleteBitMapExtension = ".bitmap";

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
        public int TotalLines => _originalLines.Length - 1; // за вычетом строки-заголовка
        public int CurrentLineIndex => _currentOriginalIndex;

        public PreTrainDataReader(string storePath, string filenameWithoutExtension)
        {
            _preTrainDataFilePath = Path.Combine(storePath, filenameWithoutExtension + PreTrainExtension);
            _completePreTrainDataFilePath = Path.Combine(storePath, filenameWithoutExtension + CompletePreTrainFileName);
            _completeBitMapFilePath = Path.Combine(storePath, filenameWithoutExtension + CompleteBitMapExtension);
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
            RemoveBadUTF8Char(ref _originalLines);
            RemoveDublicate(ref _originalLines, ref _bitMap);

            Profiler.BeginSample("GetMappedCount");
            CompletedLines = _bitMap.GetMappedCount() - 1;  // минус заголовок
            Profiler.EndSample();

            Profiler.BeginSample("OrderByLongLines");
            _sortLongestStates = new SortState<int>(_originalLines.Length);
            _sortAddrStates = new SortState<string>(_originalLines.Length);

            const string HEADER_STREET = "street";
            const string HEADER_HOUSE_NUMBER = "house_number";

            char[] splitTab = new char[] { '\t' };
            var columns = Header.Split(splitTab);

            int streetIndex = Array.IndexOf(columns, HEADER_STREET);
            int houseIndex = Array.IndexOf(columns, HEADER_HOUSE_NUMBER);
            bool isHouseLastColumn = houseIndex == columns.Length - 1;

            for (int i = 0; i < _originalLines.Length; i++)     // включаем заголовк, он не будет выбран, т.к. зарание помечен, что сделан
            {
                _sortLongestStates.OrderLines.Add(new KeyValuePair<int, int>(_originalLines[i].Length, i));

                if (streetIndex != -1 && houseIndex != -1)
                {
                    int sepPos = -1;
                    int ci = 0;
                    int s = 0, e = 0;
                    do
                    {
                        sepPos = _originalLines[i].IndexOf('\t', sepPos + 1);

                        if (ci == streetIndex - 1)
                            s = sepPos + 1;
                        else if (ci == houseIndex)
                        {
                            e = sepPos;
                            if (isHouseLastColumn)
                                e = _originalLines[i].Length - 1;
                        }

                        ci++;
                    } while (sepPos != -1);
                    _sortAddrStates.OrderLines.Add(new KeyValuePair<string, int>(_originalLines[i].Substring(s, e - s), i));
                }
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

        private static void RemoveBadUTF8Char(ref string[] lines)
        {
            const char BAD_CHAR = '�';
            const char REPLACE_CHAR = '_';

            Profiler.BeginSample("RemoveBadUTF8Char");
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains(BAD_CHAR))
                {
                    lines[i] = line.Replace(BAD_CHAR, REPLACE_CHAR);
                    UnityEngine.Debug.Log($"bad chars\n{line}");
                }
            }
            Profiler.EndSample();
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
            if (CompletedLines < _bitMap.Length)
                CompletedLines++;
        }

        public string GetRecord(int indexLine) => _originalLines[indexLine];

        public string GetNextRecord()
        {
            int index = _currentOriginalIndex + 1;

            while (index < _bitMap.Length && _bitMap[index])
            { index++; }

            if (index < _bitMap.Length)
            {
                _currentOriginalIndex = index;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }
        public string GetNextRecordByRandom()
        {
            int index = UnityEngine.Random.Range(0, _originalLines.Length);

            while (index < _bitMap.Length && _bitMap[index])
            { index++; }

            if (index < _bitMap.Length)
            {
                _currentOriginalIndex = index;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }

        public string GetNextRecordByLong()
        {
            int index = _sortLongestStates.CurrentIndex + 1;
            while (index < _bitMap.Length)
            {
                int originalIndex = _sortLongestStates.OrderLines[index].Value;
                index++;
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
            int index = _sortAddrStates.CurrentIndex + 1;
            while (index < _bitMap.Length - 1)
            {
                int originalIndex = _sortAddrStates.OrderLines[index].Value;
                index++;
                if (_bitMap[originalIndex])
                    continue;                

                _sortAddrStates.CurrentIndex = index;
                _currentOriginalIndex = originalIndex;
                return _originalLines[_currentOriginalIndex];
            }

            return string.Empty;
        }
    }
}
