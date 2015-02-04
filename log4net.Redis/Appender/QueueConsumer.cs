using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using log4net.Util;
using log4net.Core;
using StackExchange.Redis;

namespace log4net.Redis.Appender
{
    internal class QueueConsumer : IDisposable
    {
        ConcurrentQueue<string> _queue;
        Queue<RedisValue> _batch;
        CancellationTokenSource _ctSource;
        Task _consumerTask;
        Config _config;
        static ConnectionMultiplexer _redis;
        DateTime _lastPush;
        IErrorHandler ErrorHandler { get; set; }

        public QueueConsumer(ConcurrentQueue<string> queue, Config config, log4net.Core.IErrorHandler errorHandler)
        {
            _queue = queue;
            _config = config;
            _batch = new Queue<RedisValue>();
            _ctSource = new CancellationTokenSource();
            _lastPush = DateTime.Now;
            ErrorHandler = errorHandler;
            _consumerTask = Task.Factory.StartNew(new Action(ConsumerLoop), _ctSource.Token);
        }
        
        private void ConsumerLoop()
        {
            while (true)
            {
                if (_ctSource.IsCancellationRequested)
                {
                    LogLog.Debug(this.GetType(), String.Format("Cancellation requested - Adding {0} queued messages to batch of {1} and sending in toalt {2} messages to Redis before cleaning up",
                        _queue.Count, _batch.Count, _queue.Count + _batch.Count));

                    HandleQueuedEvents();
                    PushToRedis();
                    break;
                }
                HandleQueuedEvents();
                System.Threading.Thread.Sleep(_config.Period);
            }
            LogLog.Debug(this.GetType(), "ConsumerLoop ended gracefully");
        }

        private void HandleQueuedEvents()
        {
            string logEvent;
            try
            {
                while (_queue.TryDequeue(out logEvent))
                {
                    _batch.Enqueue(logEvent);

                    if (IsTimeToPush())
                        PushToRedis();
                }
                if (IsTimeToPush())
                    PushToRedis();
            }
            catch (Exception e)
            {
                LogLog.Error(this.GetType(), e.Message, e);
            }
        }

        private bool IsTimeToPush()
        {
            if (_batch.Count >= _config.BatchSize ||
                (_batch.Count > 0 && DateTime.Now.Subtract(_lastPush) > TimeSpan.FromMilliseconds(_config.MaxBatchPeriod)))
                return true;

            return false;
        }

        private bool PushToRedis()
        {
            if (_batch.Count == 0)
                return false;

            try
            {
                if (_redis == null)
                    _redis = ConnectionMultiplexer.Connect(_config.Hosts);
            }
            catch (Exception e)
            {
                LogLog.Error(this.GetType(), e.Message, e);
            }

            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    LogLog.Debug(this.GetType(), String.Format("Sending {0} log messages to Redis", _batch.Count));
                    var res = _redis.GetDatabase().ListRightPush(_config.Key, _batch.ToArray());
                    LogLog.Debug(this.GetType(), String.Format("{0} log messages currently in queue in Redis", res));

                    _batch = new Queue<RedisValue>();
                    _lastPush = DateTime.Now;
                    return true;
                }
                catch (Exception e)
                {
                    ErrorHandler.Error(e.Message, e, ErrorCode.WriteFailure);
                }
            }
            else
            {
                if (_config.PurgeOnConnectionFailure)
                {
                    LogLog.Error(this.GetType(), "Not connected to Redis. Purging buffered messages (messages will be lost)");
                    _batch = new Queue<RedisValue>();
                    _lastPush = DateTime.Now;
                }
                else
                {
                    LogLog.Error(this.GetType(), "Not connected to Redis. Keeping batch for sending later");
                    // TODO: Store to file until connection is up?
                }
            }
            return false;
        }

        public void Dispose()
        {
            _ctSource.Cancel();
            _consumerTask.Wait();

            if (_redis != null)
            {
                _redis.Close(true);
                _redis.Dispose();
                _redis = null;
            }
        }
    }
}
