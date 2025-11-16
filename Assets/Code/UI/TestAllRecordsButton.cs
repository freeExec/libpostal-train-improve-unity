using LP.Data;
using LP.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        [SerializeField] CanvasGroup progressCanvas;

        private PreTrainDataReader dataReader;
        private AwaitableCompletionSource awatingSource;

        public async void Process(string dataPath, string filename)
        {
            progressCanvas.alpha = 1;
            await Task.Yield();

            var filenameComplete = Path.GetFileNameWithoutExtension(filename) + "_complete" + Path.GetExtension(filename);
            await Task.Run(() => dataReader = new PreTrainDataReader(dataPath, filenameComplete));
            StartCoroutine(PrecessCo(progressCompleted));
            await awatingSource.Awaitable;

            await Task.Run(() => dataReader = new PreTrainDataReader(dataPath, filename));
            StartCoroutine(PrecessCo(progressAll));
        }

        private IEnumerator PrecessCo(ProgressBarGradientColor progress)
        {
            awatingSource = new AwaitableCompletionSource();

            var addressColumns = AddressFormatterHelper.HeaderToAddress(dataReader.Header);
            var comparer = new ElementModelMatchComparer();

            int matchLines = 0;

            // skip header
            for (int currentLineIndex = 1; currentLineIndex <= dataReader.TotalLines; currentLineIndex++)
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

                if (currentLineIndex % 1000 == 0 && currentLineIndex > 0 || currentLineIndex == dataReader.TotalLines)
                {
                    progress.Value = (float)matchLines / currentLineIndex;
                    progress.SetLabel($"{progress.Value :P2} | {matchLines} | {(float)currentLineIndex / dataReader.TotalLines :P2}");

                    //print($"{currentLineIndex}, {dataReader.TotalLines} => {line}");
                    yield return null;
                }
            }

            dataReader = null;
            awatingSource.SetResult();
        }
    }
}