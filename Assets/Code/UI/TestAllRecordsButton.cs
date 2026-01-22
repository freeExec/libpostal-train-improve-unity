using LP.Data;
using LP.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
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
        private CancellationTokenSource cancellationProcessToken;
        private AwaitableCompletionSource awatingSource;

        private List<string> completedBadLines;

        public bool IsProcessing => cancellationProcessToken is not null;

        public async void Process(string dataPath, string filename)
        {
            cancellationProcessToken = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            progressCanvas.alpha = 1;
            await Task.Yield();

            if (cancellationProcessToken.IsCancellationRequested == false)
            {
                await Task.Run(() => dataReader = new PreTrainDataReader(dataPath, filename), cancellationProcessToken.Token);
                if (cancellationProcessToken.IsCancellationRequested == false)
                {
                    StartCoroutine(PrecessCo());
                    await awatingSource.Awaitable;

                    var filenameComplete = Path.Combine(dataPath, Path.GetFileNameWithoutExtension(filename) + "_complete_bad" + ".tsv");
                    File.WriteAllLines(filenameComplete, completedBadLines);
                    completedBadLines.Clear();
                    completedBadLines = default;
                }
            }
            
            cancellationProcessToken.Dispose();
            cancellationProcessToken = null;
        }

        public void ProcessStop()
        {
            //progressCanvas.alpha = 0;
            cancellationProcessToken?.Cancel();
            cancellationProcessToken?.Dispose();
            cancellationProcessToken = null;
        }

        public void ClearProgress()
        {
            progressCompleted.Value = 0;
            progressCompleted.SetLabel(string.Empty);
            progressAll.Value = 0;
            progressAll.SetLabel(string.Empty);
        }

        private IEnumerator PrecessCo()
        {
            awatingSource = new AwaitableCompletionSource();

            var addressColumns = AddressFormatterHelper.HeaderToAddress(dataReader.Header);
            var comparer = new ElementModelMatchComparer();

            int matchLines = 0;
            int matchLinesCompleted = 0;
            int totalLinesCompleted = 0;

            completedBadLines = new List<string>(dataReader.CompletedLines / 3);

            // skip header
            for (int currentLineIndex = 1; currentLineIndex <= dataReader.TotalLines; currentLineIndex++)
            {
                if (cancellationProcessToken.IsCancellationRequested)
                    break;

                var line = dataReader.GetRecord(currentLineIndex);
                var lprecord = new LPRecord(currentLineIndex, line);

                var prepareComponents = line
                    .Split(LPRecord.SPLIT_SEPATARE_TAB)
                    .Zip(addressColumns, (value, address) => new ElementModel(address, value));

                var libpostalComponents = lprecord.ParseResultEnum.Select(p => new ElementModel(p.Key, p.Value));
                var isMatch = prepareComponents.Where(c => !c.IsEmpty).SequenceEqual(libpostalComponents, comparer);

                bool isMarkLine = dataReader.IsMarkRecord(currentLineIndex);
                if (isMarkLine)
                {
                    totalLinesCompleted++;
                    if (isMatch)
                        matchLinesCompleted++;
                    else
                        completedBadLines.Add(line);
                }

                if (isMatch)
                {
                    matchLines++;
                }

                if (currentLineIndex % 1000 == 0 && currentLineIndex > 0 || currentLineIndex == dataReader.TotalLines)
                {
                    progressAll.Value = (float)matchLines / currentLineIndex;
                    progressAll.SetLabel($"{progressAll.Value :P2} | {matchLines} | {(float)currentLineIndex / dataReader.TotalLines :P2}");

                    progressCompleted.Value = (float)matchLinesCompleted / totalLinesCompleted;
                    progressCompleted.SetLabel($"{progressCompleted.Value:P2} | {matchLinesCompleted} | {(float)totalLinesCompleted / dataReader.CompletedLines:P2}");

                    //print($"{currentLineIndex}, {dataReader.TotalLines} => {line}");
                    yield return null;
                }
            }

            dataReader = null;
            awatingSource.SetResult();
        }
    }
}