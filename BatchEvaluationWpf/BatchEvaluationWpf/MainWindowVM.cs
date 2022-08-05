using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Linq;
using BatchEvaluationWpf.Mathmatics;
using System.Diagnostics;

namespace BatchEvaluationWpf
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        private Stopwatch _watcher;
        private readonly BackgroundWorker _worker;

        public ObservableCollection<OperationStatistic> OperationStatisticList { get; private set; }

        public MainWindowVM()
        {
            OperationStatisticList = new ObservableCollection<OperationStatistic>()
            {
                new OperationStatistic() { Name="+", Key = OperatorType.Addition},
                new OperationStatistic() {Name="-", Key = OperatorType.Subtraction},
                new OperationStatistic() {Name="*", Key = OperatorType.Multiplication},
                new OperationStatistic() {Name="/", Key = OperatorType.Division},
                new OperationStatistic() {Name="-a", Key = OperatorType.UnaryMinus},
            };
            _worker = new BackgroundWorker();
            _worker.DoWork += Calculate;
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.ProgressChanged += ProgressChanged;
            _worker.RunWorkerCompleted += CalculationCompleted;

            ProgressVisibility = false;
        }

        private string _sourceFileName;
        public string SourceFileName
        {
            get { return _sourceFileName; }
            set { _sourceFileName = value; OnPropertyChanged("SourceFileName"); }
        }

        private TimeSpan _globalTime;
        public TimeSpan GlobalTime
        {
            get { return _globalTime; }
            set { _globalTime = value; OnPropertyChanged("GlobalTime"); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get { return _totalCount; }
            set { _totalCount = value; OnPropertyChanged("TotalCount"); }
        }
        private int TotalCount_InterlockedAdd(int num)
        {
            var newValue = Interlocked.Add(ref _totalCount, num);
            OnPropertyChanged("TotalCount");
            return newValue;
        }

        private TimeSpan _remainingTime;
        public TimeSpan RemainingTime
        {
            get { return _remainingTime; }
            set { _remainingTime = value; OnPropertyChanged("RemainingTime"); }
        }

        private bool _isInProgress;
        public bool IsInProgress
        {
            get { return _isInProgress; }
            set { _isInProgress = value; OnPropertyChanged("IsInProgress"); }
        }

        private bool _progressVisibility;
        public bool ProgressVisibility
        {
            get { return _progressVisibility; }
            set { _progressVisibility = value; OnPropertyChanged("ProgressVisibility"); }
        }

        private int _currentProgress;
        public int CurrentProgress
        {
            get { return _currentProgress; }
            set { if (_currentProgress != value) { _currentProgress = value; OnPropertyChanged("CurrentProgress"); } }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CurrentProgress = e.ProgressPercentage;

            if (CurrentProgress == 100)
                ProgressVisibility = false;
        }

        private void CalculationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                ProgressVisibility = false;
            }
        }

        private DelegateCommand _loadFromCommand;
        public ICommand LoadFromCommand
        {
            get { return _loadFromCommand ?? (_loadFromCommand = new DelegateCommand(LoadFromFile)); }
        }

        private DelegateCommand _startStopCalculationCommand;
        public ICommand StartStopCalculationCommand
        {
            get { return _startStopCalculationCommand ?? (_startStopCalculationCommand = new DelegateCommand(() => StratStop())); }
        }

        private void LoadFromFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Text Files (*.txt)|*.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                SourceFileName = dlg.FileName;
            }
        }

        private void StratStop()
        {
            if (_worker.IsBusy)
                _worker.CancelAsync();
            else
                _worker.RunWorkerAsync();
        }

        private void Calculate(object sender, DoWorkEventArgs e)
        {
            ClearStatistics();
            (sender as BackgroundWorker).ReportProgress(0);
            ProgressVisibility = true;
            IsInProgress = true;
            _watcher = Stopwatch.StartNew();
            try
            {
                if (String.IsNullOrEmpty(SourceFileName))
                    return;

                int exprCountToCalculate = 0;
                List<string> loadedItems;
                using (var reader = new StreamReader(SourceFileName))
                {
                    exprCountToCalculate = ((reader.Peek() >= 0) && int.TryParse(reader.ReadLine(), out int cntFromFile)) ? cntFromFile : 0;
                    loadedItems = LoadStrings(reader).ToList();
                }

                var loadedItemsCount = loadedItems.Count();
                exprCountToCalculate = (loadedItemsCount < exprCountToCalculate) ? loadedItemsCount : exprCountToCalculate;
                var progressStep = (double)exprCountToCalculate / 100;

                var resultList = loadedItems.AsParallel().AsOrdered().Take(exprCountToCalculate).Select(item => CalculateItem(item, exprCountToCalculate, progressStep, sender, e));

                var currentDirectory = Path.GetDirectoryName(SourceFileName);
                var targetFileName = Path.GetFileNameWithoutExtension(SourceFileName) + "_result.txt";
                var targetFile = Path.Combine(currentDirectory, targetFileName);
                using (var sw = File.CreateText(targetFile))
                {
                    foreach (var s in resultList)
                    {
                        if (_worker.CancellationPending == true)
                        {
                            e.Cancel = true;
                            break;
                        }
                        sw.WriteLine(s);
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO display in the UI
            }
            finally
            {
                _watcher.Stop();
                IsInProgress = false;
            }
        }

        private void ClearStatistics()
        {
            foreach (var st in OperationStatisticList)
                st.Clear();
            GlobalTime = new TimeSpan();
            TotalCount = 0;
            RemainingTime = new TimeSpan();
        }

        private static IEnumerable<string> LoadStrings(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
        }

        private string CalculateItem(string expression, int exprCountToCalculate, double progressStep, object sender, DoWorkEventArgs e)
        {
            string result;
            if (_worker.CancellationPending == true)
            {
                e.Cancel = true;
                return String.Empty;
            }
            try
            {
                var _tokenizer = new Tokenizer();
                var tokens = _tokenizer.Parse(expression);

                var _shuntingYard = new ShuntingYardAlgorithm();
                var yardTokens = _shuntingYard.Apply(tokens);

                var notationCalculator = new PostfixNotationCalculator();
                notationCalculator.UpdateStatistics = UpdateOperationStatistics;
                result = notationCalculator.Calculate(yardTokens).Value.ToString();
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            GlobalTime = _watcher.Elapsed;
            TotalCount_InterlockedAdd(1);
            var timePerExpression = _watcher.Elapsed.Divide(TotalCount);
            RemainingTime = (exprCountToCalculate - TotalCount) * timePerExpression;
            (sender as BackgroundWorker).ReportProgress((int)(TotalCount / progressStep));
            //Thread.Sleep(30);
            return result;
        }

        private void UpdateOperationStatistics(OperatorType operatorType, TimeSpan time)
        {
            var stat = OperationStatisticList.FirstOrDefault(o => o.Key == operatorType);
            if (stat == null)
                return;

            var newCount = stat.Count_Add(1);
            var newTotalTime = stat.TotalTime_Add(time);
            stat.AverageTime = newTotalTime.Divide(newCount);
            stat.GlobalTimePercent = Math.Round(stat.TotalTime.Divide(_watcher.Elapsed) * 100, 5);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
