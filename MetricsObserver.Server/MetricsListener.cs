using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;

namespace MetricsObserver.Server
{
    public delegate void ServerLog(string message, MessageType type);

    /// <summary>
    /// Класс обертка для инкапсуляции работы потока
    /// </summary>
    public class MetricsListener
    {
        public ServerLog OnServerLogged;

        private int _state; // 0 - stopped, 1 - running
        private const int Stopped = 0;
        private const int Running = 1;

        private const int DatagramSize = 256;
        private readonly IPEndPoint _ipEndPoint;
        private readonly MetricsStorage _metricsStorage;

        public MetricsListener(IPEndPoint ipEndPoint, MetricsStorage metricsStorage)
        {
            _ipEndPoint = ipEndPoint;
            _metricsStorage = metricsStorage;
        }

        /// <summary>
        /// Получение метрик
        /// </summary>
        public void ListenMetrics()
        {
            var client = new UdpClient(_ipEndPoint);
            Interlocked.Exchange(ref _state, Running);
            IPEndPoint remoteIpEndPoint = null;
            LogMessage($"Сервер запущен на {_ipEndPoint.Address}:{_ipEndPoint.Port}", MessageType.Status);

            // здесь можно было бы использовать CancellationTokenSource, но по заданию он не входит в список разрешенных
            while (Interlocked.CompareExchange(ref _state, 0, 0) == Running)
            {
                try
                {
                    if (client.Available <= 0)
                        continue;

                    var buffer = client.Receive(ref remoteIpEndPoint);
                    if (buffer.Length > DatagramSize)
                        continue;

                    var unparsedMetric = Encoding.UTF8.GetString(buffer);

                    if (TryParseMessage(unparsedMetric, out var metric, out var value))
                        _metricsStorage.AddOrUpdate(metric, value);
                    else
                        LogMessage(unparsedMetric, MessageType.FormatError);
                }
                catch (Exception e)
                {
                    LogMessage(e.Message, MessageType.Error);
                }
            }

            client.Close();
            LogMessage("Сервер Остановлен", MessageType.Status);
        }

        public void Stop()
        {
            Interlocked.Exchange(ref _state, Stopped);
        }

        private void LogMessage(string message, MessageType messageType)
        {
            OnServerLogged?.Invoke(message, messageType);
        }

        /// <summary>
        /// Парсинг сообщения
        /// </summary>
        /// <param name="message">Сообщение в формате: {имя_метрики}:{значение}</param>
        /// <param name="metricName">Непустая строка без пробелов и символа ‘:’ (двоеточий)</param>
        /// <param name="value">Число (целое или с плавающей точкой)</param>
        /// <returns></returns>
        private static bool TryParseMessage(string message, out string metricName, out double value)
        {
            metricName = string.Empty;
            value = 0;

            var split = message.Split(':');
            if (split.Length != 2)
                return false;

            if (string.IsNullOrWhiteSpace(split[0]))
                return false;

            if (split[0].Contains(" "))
                return false;

            if (!double.TryParse(split[1].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return false;

            metricName = split[0];
            return true;
        }
    }
}
