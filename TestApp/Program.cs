using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Diagnostics;

[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch = true)]
namespace TestApp
{
    class Program
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            int threads = 10;
            int loops = 10000;
            List<Task> tasks = new List<Task>();

            Console.WriteLine("Press enter to start!");
            Console.ReadLine();

            log.Info("Application started...");

            Stopwatch sw = new Stopwatch();
            Stopwatch swTotal = new Stopwatch();
            swTotal.Start();
            sw.Start();

            for (int i = 0; i < threads; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    try
                    {
                        for (int j = 0; j < loops; j++)
                            log.Info(String.Format("{0} [{1}] - Logging line {2}", DateTime.UtcNow.ToString("o"), System.Threading.Thread.CurrentThread.ManagedThreadId, j));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            sw.Stop();

            Console.WriteLine("{0} Threads * {1} loops => Total {2} log events in {3} ms => {4:0} events/s", threads, loops, threads * loops, sw.ElapsedMilliseconds, (((double)threads * loops)/sw.ElapsedMilliseconds) * 1000);
            Console.WriteLine("Sleeping for 3 seconds...");

            System.Threading.Thread.Sleep(3000);

            Console.WriteLine("Shutting down logger...");
            log.Logger.Repository.Shutdown();
            swTotal.Stop();
            Console.WriteLine("Using in total {0} ms => {1:0} events/s", swTotal.ElapsedMilliseconds, (((double)threads * loops) / swTotal.ElapsedMilliseconds) * 1000);
            Console.WriteLine("Press enter to quit");
            Console.ReadLine();
        }
    }
}
