using LibPostalNet;
using LP.Model;
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
        //public static CoreProcess Instance { get; private set; }

        public List<AddressFormatter> HeaderOrder { get; private set; }

        public int CompletedLines => dataReader.CompletedLines;
        public int TotalLines => dataReader.TotalLines;
        public int CurrentLineIndex => dataReader.CurrentLineIndex;

        private PreTrainDataReader dataReader;


        private LibpostalNormalizeOptions optExpand;
        private LibpostalAddressParserOptions parseOpt;

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
        }

        private void OnDestroy()
        {
            // Teardown (only called once at the end of your program)
            libpostal.LibpostalTeardown();
            libpostal.LibpostalTeardownParser();
            libpostal.LibpostalTeardownLanguageClassifier();
        }

        public bool Setup()
        {
            bool libpostalOk = SetupLibpostal();
            if (libpostalOk)
            {
                SetupLibpostalOptions();
            }
            return libpostalOk;
        }

        public async Task LoadFileAsync(string filename)
        {
            await Task.Run(() => dataReader = new PreTrainDataReader(ValidateDataPath, filename));
            HeaderOrder = HeaderToAddress(dataReader.Header);
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

        public void SetRecord(string row) => dataReader.SetRecord(row);
        public void DeleteCurrentRecord() => dataReader.DeleteCurrentRecord();

        private List<KeyValuePair<string, string>> ParseLibpostal(string addrStr)
        {
            var addrStrNoTab = addrStr.Replace('\t', ' ');
            var parse = libpostal.LibpostalParseAddress(addrStrNoTab.Trim(), parseOpt);

            return parse.Results;
        }

        private LPRecord GetParseRecord(Func<string> getNexrRexort)
        {
            var line = getNexrRexort();
            var parse = ParseLibpostal(line);

            return new LPRecord()
            {
                LineIndex = dataReader.CurrentLineIndex,
                Line = line,
                ParseResult = parse,
            };
        }

        private static List<AddressFormatter> HeaderToAddress(string header)
        {
            //index	region	district	city	suburb	street	house_number	unit    category
            var helperReverce = Enum.GetValues(typeof(AddressFormatter)).Cast<AddressFormatter>().ToDictionary(af => af.ToTsvString());
            var h2a = header.Split('\t').Select(c => helperReverce[c]).ToList();
            return h2a;
        }

        private bool SetupLibpostal()
        {
            var dataPath = Path.Combine(Application.streamingAssetsPath, "Libpostal");

            bool a = libpostal.LibpostalSetupDatadir(dataPath);
            bool b = libpostal.LibpostalSetupLanguageClassifierDatadir(dataPath);
            bool c = libpostal.LibpostalSetupParserDatadir(dataPath);

            return a && b && c;
        }

        private void SetupLibpostalOptions()
        {
            optExpand = libpostal.LibpostalGetDefaultOptions();
            optExpand.LatinAscii = false;
            optExpand.StripAccents = false;
            optExpand.Decompose = false;

            optExpand.Transliterate = false;
            optExpand.Lowercase = false;

            //optExpand.DeleteAcronymPeriods = false;       // удалять сокращения??
            //optExpand.DeleteNumericHyphens = false;       // удалять числовые дефисы
            //optExpand.DropParentheticals = false;         // отбросить скобки
            //optExpand.DeleteWordHyphens = false;          // удалять перенос слова
            //optExpand.DropEnglishPossessives = false;     // отбросить английское склонение
            optExpand.DeleteApostrophes = false;

            optExpand.SplitAlphaFromNumeric = false;        // раздвигать буквы от цифр (особо мешает в номере дома)
            optExpand.ReplaceWordHyphens = false;           // удалять дефисы

            optExpand.Langs = new[] { "ru" };

            parseOpt = new LibpostalAddressParserOptions();
        }
    }

    public class LPRecord
    {
        public int LineIndex;
        public string Line;
        public List<KeyValuePair<string, string>> ParseResult;

        public IEnumerable<KeyValuePair<AddressFormatter, string>> ConvertParseToEnum()
            => ParseResult.Select(r => 
                    new KeyValuePair<AddressFormatter, string>(AddressFormatterHelper.GetFormatterFromLibpostal(r.Key), RecoveryCase(r.Value))
            );

        private string RecoveryCase(string libpostalAnsverElement)
        {
            int found = Line.IndexOf(libpostalAnsverElement);
            if (found != -1)
            {
                return Line.Substring(found, libpostalAnsverElement.Length);
            }
            return libpostalAnsverElement;
        }
    }
}