using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Stardust.Interstellar.Rest.Client;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Common;
using Stardust.Particles;
using Timer = System.Timers.Timer;

namespace Stardust.Continuum.Client
{
    public class BufferedHttpLogger<T> where T : class
    {
        private readonly ReaderWriterLockSlim _synclock = new ReaderWriterLockSlim();
        private readonly Func<T[], Task> _transmitterAction;
        private ConcurrentBag<T> buffer = new ConcurrentBag<T>();
        public bool Running { get; private set; }
        public BufferedHttpLogger(Func<T[], Task> transmitterAction)
        {
            _transmitterAction = transmitterAction;
            Start();
        }

        public void Enqueue(T item)
        {
            _synclock.TryEnterReadLock(2);
            try
            {
                buffer.Add(item);
            }
            finally
            {
                _synclock.ExitReadLock();
            }

        }
        public void Start()
        {
            Running = true;
            Task.Run(async () =>
            {
                try
                {
                    while (Running)
                    {
                        try
                        {
                            if (buffer.IsEmpty)
                            {
                                await Task.Delay(200);
                                continue;
                            }

                            T[] buff;
                            buff = ExtractBuffer();
                            await _transmitterAction.Invoke(buff);
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            });
        }

        private T[] ExtractBuffer()
        {
            T[] buff;
            _synclock.TryEnterWriteLock(10);
            ConcurrentBag<T> b;
            try
            {
                b = buffer;
                buffer = new ConcurrentBag<T>();
            }
            finally
            {
                _synclock.ExitWriteLock();
            }
            buff = b?.ToArray();
            return buff;
        }

        public void Stop()
        {
            Running = false;
        }
    }
    public static class ContinuumClient
    {
        /// <summary>
        /// If not null and greater than 1000 characters the streamed message element will be truncated for buffered transmissions
        /// </summary>
        /// <value>
        /// The size of the limit message.
        /// </value>
        public static int? LimitMessageSize { get; set; }
        private static Timer _timer = new Timer() { Interval = 500, AutoReset = true, Enabled = true };
        private static string url;

        private static ConcurrentBag<StreamItem> _logBuffer = new ConcurrentBag<StreamItem>();

        private static object triowing = new object();

        static ContinuumClient()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        public static void Shutdown()
        {
            _timer.Enabled = false;
            _logPumpIsDisabled = true;
            Flush();
        }

        private static void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                Flush();
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private static void Flush()
        {
            StreamItem[] items;
            lock (triowing)
            {
                ConcurrentBag<StreamItem> tempBuffer;
                lock (bufferLock)
                {
                    tempBuffer = _logBuffer;
                    _logBuffer = new ConcurrentBag<StreamItem>();
                }
                items = tempBuffer.ToArray();
            }
            switch (items.Length)
            {
                case 0:
                    return;
                case 1:
                    Task.Run(async () => await AddStreamInternal(items[0])).Wait();
                    break;
                default:
                    Task.Run(async () => await AddStreamInternal(items.Reverse().ToArray())).Wait();
                    break;
            }
        }

        public static string BaseUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    url = ConfigurationManagerHelper.GetValueOnKey("continuum:apiUrl");
                }
                return url;
            }
            set { url = value; }
        }

        private static string _project;

        public static string Project
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_project))
                {
                    _project = ConfigurationManagerHelper.GetValueOnKey("continuum:project");
                }
                return _project;
            }
            set { _project = value; }
        }

        private static string _environment;
        private static ILogStream client;
        private static object bufferLock = new object();
        private static bool _logPumpIsDisabled;

        public static string Environment
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_environment))
                {
                    _environment = ConfigurationManagerHelper.GetValueOnKey("continuum:environment");
                }
                return _environment;
            }
            set { _environment = value; }
        }

        public static void SetApiKey(string apiKey)
        {
            LogStreamConfig.ApiKey = apiKey;
        }

        public static void AddStream(StreamItem item, bool buffered = true)
        {

            if (buffered)
            {
                if (_logPumpIsDisabled) throw new ObjectDisposedException(nameof(ContinuumClient), "Batch message pump is closed");
                if (LimitMessageSize.HasValue && LimitMessageSize.Value > 1000)
                    item.Message = item.Message?.Substring(0, LimitMessageSize.Value);
                lock (bufferLock)
                {
                    _logBuffer.Add(item);
                }

            }
            else
            {
                Task.Run(async () =>
                {
                    await AddStreamInternal(item);
                });
            }
        }

        private static async Task AddStreamInternal(StreamItem item)
        {
            try
            {
                await LogStreamClient.AddStream(Project, Environment, item);
            }
            catch
            {
            }
        }

        public static void AddStream(StreamItem[] items)
        {
            Task.Run(async () =>
            {
                await AddStreamInternal(items);
            });
        }

        private static async Task AddStreamInternal(StreamItem[] items)
        {
            try
            {
                await LogStreamClient.AddStreamBatch(Project, Environment, items);
            }
            catch
            {
            }
        }

        public static void AddStream(string environment, StreamItem item)
        {
            Task.Run(async () =>
            {
                try
                {
                    await LogStreamClient.AddStream(Project, environment, item);
                }
                catch
                {

                }
            });
        }

        public static void AddStream(string environment, StreamItem[] items)
        {
            Task.Run(async () =>
            {
                try
                {
                    await LogStreamClient.AddStreamBatch(Project, environment, items);
                }
                catch
                {
                }
            });
        }

        public static void AddStream(string project, string environment, StreamItem item)
        {
            Task.Run(async () =>
            {
                try
                {
                    await LogStreamClient.AddStream(project, environment, item);
                }
                catch
                {

                }
            });
        }

        public static void AddStream(string project, string environment, StreamItem[] items)
        {
            Task.Run(async () =>
            {
                try
                {
                    await LogStreamClient.AddStreamBatch(project, environment, items);
                }
                catch
                {

                }
            });
        }

        private static ILogStream LogStreamClient
        {
            get
            {
                if (client == null)
                    client = ExtensionsFactory.GetLocator().CreateRestClient<ILogStream>(BaseUrl);
                var c = client;
                return c;
            }
        }
    }


}