using ESoft.CommonLibrary;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace OSI.Core.Models.Jobs
{   
    public class Job
    {
        public Func<JobParameters> Parameters { get; set; }

        public int RepeatIntervalParameter { get; set; } = 1;

        public DateTime? LastRun { get; private set; } = null;

        public bool CanRunOnAdminPage { get; set; } = true;

        private JobRepeatInterval _RepeatInterval = JobRepeatInterval.Daily;
        public JobRepeatInterval RepeatInterval
        {
            get { return _RepeatInterval; }
            set
            {
                _RepeatInterval = value;
                CalculateNextRun();
            }
        }

        private TimeSpan _Time;
        public TimeSpan Time
        {
            get { return _Time; }
            set
            {
                _Time = value;
                CalculateNextRun();
            }
        }

        private int _RepeatEvery = 1;

        public int RepeatEvery
        {
            get { return _RepeatEvery; }
            set
            {
                _RepeatEvery = value;
                CalculateNextRun();
            }
        }

        private void CalculateNextRun()
        {
            DateTime now = DateTime.Now;
            switch (RepeatInterval)
            {
                case JobRepeatInterval.Secondly:
                    NextRun = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second)
                        .AddSeconds(RepeatEvery);
                    break;
                case JobRepeatInterval.Minutely:
                    NextRun = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, _Time.Seconds)
                        .AddMinutes(now.Second > _Time.Seconds ? RepeatEvery : 0);
                    break;
                case JobRepeatInterval.Hourly:
                    NextRun = new DateTime(now.Year, now.Month, now.Day, now.Hour, _Time.Minutes, _Time.Seconds)
                        .AddHours(now.TimeOfDay.Subtract(TimeSpan.FromHours(now.TimeOfDay.Hours)) > _Time.Subtract(TimeSpan.FromHours(_Time.Hours)) ? RepeatEvery : 0);
                    break;
                case JobRepeatInterval.Daily:
                    NextRun = new DateTime(now.Year, now.Month, now.Day, _Time.Hours, _Time.Minutes, _Time.Seconds)
                        .AddDays(now.TimeOfDay > _Time ? RepeatEvery : 0);
                    break;
                case JobRepeatInterval.Weekly:
                    NextRun = new DateTime(now.Year, now.Month, now.Day, _Time.Hours, _Time.Minutes, _Time.Seconds)
                        .AddDays((int)now.DayOfWeek == RepeatIntervalParameter ? 0 :
                                 ((int)now.DayOfWeek > RepeatIntervalParameter ? 7 : 0) + RepeatIntervalParameter - (int)now.DayOfWeek);
                    if (now.Day == NextRun.Day && now.TimeOfDay > _Time)
                        NextRun = NextRun.AddDays(7);
                    break;
                case JobRepeatInterval.Monthly:
                    NextRun = new DateTime(now.Year, now.Month, RepeatIntervalParameter, _Time.Hours, _Time.Minutes, _Time.Seconds)
                        .AddMonths(now.Day > RepeatIntervalParameter ? RepeatEvery : 0);
                    if (now.Month == NextRun.Month && now.Day == NextRun.Day && now.TimeOfDay > _Time)
                        NextRun = NextRun.AddMonths(1);
                    break;
                default:
                    break;
            }
        }

        private void RecalculateNextRun()
        {
            switch (RepeatInterval)
            {
                case JobRepeatInterval.Secondly:
                    if (RepeatEvery > 1)
                        NextRun = NextRun.AddSeconds(RepeatEvery); //Jobs run almost every seconds, recalculation does nothing as DateTime.Now always higher than NextRun
                    break;
                case JobRepeatInterval.Minutely:
                    NextRun = NextRun.AddMinutes(RepeatEvery);
                    break;
                case JobRepeatInterval.Hourly:
                    NextRun = NextRun.AddHours(RepeatEvery);
                    break;
                case JobRepeatInterval.Daily:
                    NextRun = NextRun.AddDays(RepeatEvery);
                    break;
                case JobRepeatInterval.Weekly:
                    NextRun = NextRun.AddDays(7);
                    break;
                case JobRepeatInterval.Monthly:
                    NextRun = NextRun.AddMonths(RepeatEvery);
                    break;
                default:
                    break;
            }
        }

        public DateTime NextRun { get; private set; }

        public Func<JobParameters, Task<ApiResponse>> Function { private get; set; }

        public bool IsBusy { get; set; }

        public Task<ApiResponse> Run(bool recalculateNextRun, JobParameters pars = null)
        {
            if (recalculateNextRun)
                RecalculateNextRun();  // пересчет делаем в основном потоке, т.к. старт параллельного потока может случиться не сразу

            return Task.Run(() =>
            {
                LastRun = DateTime.Now;
                if (pars == null)
                    return Function(Parameters?.Invoke());
                else 
                    return Function(pars);
            });
        }
    }
}