using LibPostalNet;
using LP.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LP.Data
{
    public class LPRecord
    {
        public const char SPLIT_SEPATARE_TAB = '\t';

        public const char LP_SEPATARE_SPACE = ' ';
        public const char LP_SEPATARE_SEMI = ',';
        public const char LP_SEPATARE_VBAR = '|';   // для склейки не работает

        private static LibpostalNormalizeOptions optExpand;
        private static LibpostalAddressParserOptions parseOpt;


        public int LineIndex;
        public string Line;
        public List<KeyValuePair<string, string>> ParseResult;
        public List<KeyValuePair<AddressFormatter, string>> ParseResultEnum;
        public HashSet<string> ExpandedAddressGlobalSet;
        public string ExpandedAddressIndividual;

        private string lineLowerNoSemi;

        #region LibPostal Init
        public static bool LibPostalSetup(string rootDir)
        {
            bool libpostalOk = SetupLibpostal(rootDir);
            if (libpostalOk)
            {
                SetupLibpostalOptions();
            }
            return libpostalOk;
        }

        private static bool SetupLibpostal(string rootDir)
        {
            var dataPath = Path.Combine(rootDir, "Libpostal");

            bool a = libpostal.LibpostalSetupDatadir(dataPath);
            bool b = libpostal.LibpostalSetupLanguageClassifierDatadir(dataPath);
            bool c = libpostal.LibpostalSetupParserDatadir(dataPath);

            return a && b && c;
        }

        private static void SetupLibpostalOptions()
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

        public static void LibPostalTeardown()
        {
            // Teardown (only called once at the end of your program)
            libpostal.LibpostalTeardown();
            libpostal.LibpostalTeardownParser();
            libpostal.LibpostalTeardownLanguageClassifier();
        }
        #endregion

        public LPRecord(int index, string line)
        {
            LineIndex = index;
            Line = line;

            FillParseLibpostal();
            FillConvertedParseToEnum();
            FillExpandedAddress();
        }

        private void FillParseLibpostal()
        {
            var addrStrNoTab = Line.Replace(SPLIT_SEPATARE_TAB, LP_SEPATARE_SEMI);
            var parse = libpostal.LibpostalParseAddress(addrStrNoTab.Trim(), parseOpt);

            ParseResult = parse.Results;
        }

        private void FillConvertedParseToEnum()
        {
            lineLowerNoSemi = Line.ToLowerInvariant().Replace(LP_SEPATARE_SEMI, LP_SEPATARE_SPACE);
            ParseResultEnum = 
                ParseResult.Select(r =>
                    new KeyValuePair<AddressFormatter, string>(AddressFormatterHelper.GetFormatterFromLibpostal(r.Key), RecoveryCase(r.Value))
                ).ToList();
        }

        private string RecoveryCase(string libpostalAnsverElement)
        {
            int found = lineLowerNoSemi.IndexOf(libpostalAnsverElement);
            if (found != -1)
            {
                //try {
                    return Line.Substring(found, libpostalAnsverElement.Length);
                //} catch { UnityEngine.Debug.LogError($"{LineIndex}: {libpostalAnsverElement} -> {Line}"); }
            }
            return libpostalAnsverElement;
        }

        private void FillExpandedAddress()
        {
            ExpandedAddressGlobalSet = 
                libpostal.LibpostalExpandAddress(
                    string.Join(LP_SEPATARE_SEMI, ParseResultEnum.Where(c => c.Key <= AddressFormatter.Road).Select(c => c.Value)),
                    optExpand
                ).Expansions.ToHashSet();

            ExpandedAddressIndividual = string.Empty;
            foreach (var addressComponent in ParseResultEnum)
            {
                optExpand.AddressComponents = (ushort)addressComponent.Key.ToLibpostalAddress();
                var expandAddr = libpostal.LibpostalExpandAddress(addressComponent.Value, optExpand);

                if (expandAddr.Expansions.Length > 0)
                {
                    //int maxLength = expandAddr.Expansions.Max(e  => e.Length);
                    //expandAddrCombine += expandAddr.Expansions.First(e => e.Length == maxLength) + " ";
                    ExpandedAddressIndividual += expandAddr.Expansions.First() + LP_SEPATARE_SEMI + " ";
                }
            }
        }
    }
}
