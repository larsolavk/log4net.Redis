using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net.Core;
using log4net.Appender;
using log4net.Util;
using StackExchange.Redis;

namespace log4net.Redis.Appender
{
    public class RedisAppender : AppenderSkeleton
    {
        public string Hosts { get; set; }
        public string Password { get; set; }
        public string Key { get; set; }
        public int Period { get; set; }
        public int BatchSize { get; set; }
        public int MaxBatchPeriod { get; set; }
        public bool PurgeOnConnectionFailure { get; set; }

        ConcurrentQueue<string> _eventQueue;
        QueueConsumer _consumer;

        public override void ActivateOptions()
        {
            try
            {
                base.ActivateOptions();
                _eventQueue = new ConcurrentQueue<string>();
                _consumer = new QueueConsumer(_eventQueue, ValidateProperties(), this.ErrorHandler);
            }
            catch (Exception e)
            {
                LogLog.Error(this.GetType(), "Error during ActivateOptions", e);
            }
        }

        private Config ValidateProperties()
        {
            if (String.IsNullOrWhiteSpace(Hosts))
                throw new ArgumentException("Mandatory property 'Hosts' not set");
            if (String.IsNullOrWhiteSpace(Key))
                throw new ArgumentException("Mandatory property 'Key' not set");
            if (Period == 0)        
                Period = 1000;
            if (BatchSize == 0)
                BatchSize = 100;
            if (MaxBatchPeriod == 0)
                MaxBatchPeriod = Period;

            return new Config
            {
                BatchSize = BatchSize,
                Hosts = Hosts,
                Key = Key,
                MaxBatchPeriod = MaxBatchPeriod,
                Period = Period,
                PurgeOnConnectionFailure = PurgeOnConnectionFailure
            };
        }

        protected override void OnClose()
        {
            try
            {
                if (_consumer != null)
                    _consumer.Dispose();

                LogLog.Debug(this.GetType(), "Appender cleanup ended gracefully");
            }
            catch (Exception e)
            {
                ErrorHandler.Error(e.Message, e, ErrorCode.CloseFailure);
            }
        }

        protected override bool RequiresLayout
        {
            get { return true; }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                var s = RenderLoggingEvent(loggingEvent);
                _eventQueue.Enqueue(s);
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Error adding event to queue", e, ErrorCode.GenericFailure);
            }
        }
    }
}
