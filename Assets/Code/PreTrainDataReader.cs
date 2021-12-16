using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Data
{
    internal class PreTrainDataReader : IDisposable
    {
        private const string CompleteLineFileName = "CompleteLine.txt";
        private const string PreTrainFileName = "license_separate_addresses.tsv";

        private readonly string _completeFilePath;
        private readonly string _preTrainDataFilePath;

        private int _completePreTrainDataLine;
        private StreamReader _reader;

        public PreTrainDataReader(string StorePath)
        {
            _completeFilePath = Path.Combine(StorePath, CompleteLineFileName);
            _preTrainDataFilePath = Path.Combine(StorePath, PreTrainFileName);
        }

        public void ReadTsvPreTrainData()
        {
            _completePreTrainDataLine = 0;
            if (File.Exists(_completeFilePath))
            {
                int.TryParse(File.ReadAllText(_completeFilePath), out _completePreTrainDataLine);
            }
        }

        public void SaveTsvPreTrainData()
        {
            File.WriteAllText(_completeFilePath, _completePreTrainDataLine.ToString());
        }

        public string GetNextRecord()
        {
            if (_reader == default)
            {
                ReadTsvPreTrainData();
                _reader = new StreamReader(_preTrainDataFilePath);
                for (int i = _completePreTrainDataLine; i > 0; i--)
                {
                    _reader.ReadLine();
                }
            }
            
            return _reader.ReadLine();
        }

        public void Dispose()
        {
            if (_reader != default)
                _reader.Dispose();
        }
    }
}
