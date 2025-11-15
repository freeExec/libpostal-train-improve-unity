using LP.Data;
using LP.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public class TestAllRecordsButton : MonoBehaviour
    {
        [SerializeField] ProgressBarGradientColor progressAll;
        [SerializeField] ProgressBarGradientColor progressCompleted;

        private PreTrainDataReader dataReader;

        public async void Process(string filename)
        {
            await Task.Run(() => dataReader = new PreTrainDataReader(CoreProcess.ValidateDataPath, filename));

            StartCoroutine(PrecessCo());
        }

        private IEnumerator PrecessCo()
        {
            int currentLineIndex = 1;   // skip header
            var addressColumns = AddressFormatterHelper.HeaderToAddress(dataReader.Header);
            var comparer = new ElementModelMatchComparer();

            int matchLines = 0;

            while (true)
            {
                var line = dataReader.GetRecord(currentLineIndex);
                var lprecord = new LPRecord(currentLineIndex, line);

                var prepareComponents = line
                    .Split(LPRecord.SPLIT_SEPATARE_TAB)
                    .Zip(addressColumns, (value, address) =>
                        new ElementModel(address, value, ElementSource.PreparePythonScript));

                var libpostalComponents = lprecord.ParseResultEnum.Select(p => new ElementModel(p.Key, p.Value, ElementSource.Libpostal));
                var isMatch = prepareComponents.Where(c => !c.IsEmpty).SequenceEqual(libpostalComponents, comparer);

                if (isMatch)
                    matchLines++;

                currentLineIndex++;

                if (currentLineIndex % 10 == 0 && currentLineIndex > 0)
                {
                    progressAll.Value = (float)currentLineIndex / dataReader.TotalLines;
                    yield return null;
                }
            }
        }
    }
}