using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESoft.CommonLibrary;
using Microsoft.Extensions.Logging;
using NLog;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using OSI.Core.Models.Jobs;
using OSI.Core.Models.Enums;
using OSI.Core.Models;

namespace OSI.Core.Services
{
    public interface IJobSvc
    {
        void StartTimer();
        Task<ApiResponse> StartJob(Job job, bool recalculateNextRun, JobParameters parameters = null);
    }

    public partial class JobSvc : IJobSvc
    {
        private List<Job> _jobs;
        private readonly NLog.Logger _logger = LogManager.GetLogger(nameof(JobSvc));
        private readonly IJobLogic jobLogic;
        private string _jobName;
        private Timer MainTimer;

        public JobSvc(IJobLogic jobLogic)
        {
            this.jobLogic = jobLogic;
            _jobs = new List<Job>();

            // отправка dbf Казпочте
            Job sendDbfToKazpostJob = new Job
            {
                RepeatInterval = JobRepeatInterval.Daily,
                Time = new TimeSpan(3, 0, 0),
                Parameters = () => new JobParameters
                {
                    StartDate = DateTime.Today.AddDays(-1),
                    Email = "d.agmanova@kazpost.kz,gulbanuar@kazpost.kz,kshumakher84@gmail.com,ussiksamara@gmail.com"
                    //Email = "kshumakher84@gmail.com,ussiksamara@gmail.com"
                }
            };
            sendDbfToKazpostJob.Function = async (pars) =>
            {
                WriteLog("Запуск SendDBFToKazPost, date = " + pars.StartDate.ToString("dd-MM-yyyy") + ", email = " + pars.Email);
                var apiResult = await jobLogic.SendDBFToKazPost(pars);
                return apiResult;
            };
            _jobs.Add(sendDbfToKazpostJob);
        }

        public void StartTimer()
        {
            MainTimer = new Timer((obj) =>
            {
                DateTime now = DateTime.Now;
                _jobs.ForEach(job =>
                {
                    if (now >= job.NextRun) job.Run(true);
                });
            }, null, 0, 1000);
        }

        private void WriteLog(string message)
        {
            LogEventInfo ei = new LogEventInfo(NLog.LogLevel.Info, nameof(JobSvc), message);
            ei.Properties["JobName"] = _jobName;
            _logger.Log(ei);
        }

        private void WriteError(string message)
        {
            LogEventInfo ei = new LogEventInfo(NLog.LogLevel.Error, nameof(JobSvc), message);
            ei.Properties["JobName"] = _jobName;
            _logger.Log(ei);
        }

        public async Task<ApiResponse> StartJob(Job job, bool recalculateNextRun, JobParameters parameters = null)
        {
            return await job.Run(recalculateNextRun, parameters);
        }
    }
}
