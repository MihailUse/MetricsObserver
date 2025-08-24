using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using MetricsObserver.Server;

namespace MetricsObserver
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            var showMetricsInterval = TimeSpan.FromSeconds(5);

            try
            {
                using (var server = new MetricsObserverServer())
                {
                    server.ServerLog += WriteServerLog;
                    server.Start();
                    stopwatch.Start();

                    while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter))
                    {
                        if (stopwatch.Elapsed >= showMetricsInterval)
                        {
                            WriteMetrics(server.MetricsStorage);
                            stopwatch.Restart();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                WriteServerLog(exception.Message, MessageType.Error);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Вывод всех метрик
        /// </summary>
        /// <param name="metrics"></param>
        private static void WriteMetrics(MetricsStorage metrics)
        {
            if (!metrics.Any())
            {
                Console.WriteLine("[METRIC] Нет данных");
                return;
            }

            var formatedMetrics = metrics.ReadAll()
                .Aggregate(new StringBuilder("[METRIC] "),
                    (builder, item) => builder.AppendFormat(CultureInfo.InvariantCulture, " | {0} = {1}", item.Key, item.Value),
                    builder => builder.Remove(9, 3)); // удаляем лишний разделитель

            Console.WriteLine(formatedMetrics.ToString());
        }

        /// <summary>
        /// Вывод сообщений сервера
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void WriteServerLog(string message, MessageType type)
        {
            switch (type)
            {
                case MessageType.Status:
                    Console.WriteLine("[STATUS] {0}", message);
                    break;
                case MessageType.FormatError:
                    Console.WriteLine("Ошибка формата: {0}", message);
                    break;
                case MessageType.Error:
                    Console.WriteLine("Ошибка: {0}", message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
