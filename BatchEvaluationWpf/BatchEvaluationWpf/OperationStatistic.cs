using BatchEvaluationWpf.Mathmatics;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BatchEvaluationWpf
{
    public class OperationStatistic : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private OperatorType _key;
        public OperatorType Key
        {
            get { return _key; }
            set { _key = value; OnPropertyChanged("Key"); }
        }

        private int _count;
        public int Count
        {
            get { return _count; }
            set { _count = value; OnPropertyChanged("Count"); }
        }

        private TimeSpan _totalTime;
        public TimeSpan TotalTime
        {
            get { return _totalTime; }
            set { _totalTime = value; OnPropertyChanged("TotalTime"); }
        }

        private TimeSpan _averageTime;
        public TimeSpan AverageTime
        {
            get { return _averageTime; }
            set { _averageTime = value; OnPropertyChanged("AverageTime"); }
        }

        private double _globalTimePercent;
        public double GlobalTimePercent
        {
            get { return _globalTimePercent; }
            set { _globalTimePercent = value; OnPropertyChanged("GlobalTimePercent"); }
        }

        public void Clear()
        {
            Count = 0;
            TotalTime = new TimeSpan();
            AverageTime = new TimeSpan();
            GlobalTimePercent = 0;
        }

        public int Count_Add(int num)
        {
            var newValue = Interlocked.Add(ref _count, num);
            OnPropertyChanged("Count");
            return newValue;
        }

        private readonly object TotalTimeLock = new object();
        public TimeSpan TotalTime_Add(TimeSpan time)
        {
            TimeSpan newValue;
            lock (TotalTimeLock)
            {
                _totalTime += time;
                newValue = _totalTime;
            }
            OnPropertyChanged("TotalTime");
            return newValue;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
