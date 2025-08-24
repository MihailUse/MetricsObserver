using System;
using System.Net;
using System.Threading;

namespace MetricsObserver.Server
{
    public class MetricsObserverServer : IDisposable
    {
        public MetricsStorage MetricsStorage { get; }

        private bool _disposed;
        private bool _started;
        private Thread _thread;
        private readonly MetricsListener _metricsListener;

        public ServerLog ServerLog
        {
            get => _metricsListener.OnServerLogged;
            set => _metricsListener.OnServerLogged = value;
        }

        public MetricsObserverServer(string host = "127.0.0.1", int port = 8888)
        {
            if (!IPAddress.TryParse(host, out var address))
                throw new ArgumentException("Invalid host address");

            MetricsStorage = new MetricsStorage();
            _metricsListener = new MetricsListener(new IPEndPoint(address, port), MetricsStorage);
        }

        /// <summary>
        /// Запуск сервера
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MetricsObserverServer));

            if (_started)
                return;

            // создаем новый поток 
            _thread = new Thread(_metricsListener.ListenMetrics)
            {
                IsBackground = false, // делаем зависимым от основного потока
                Name = nameof(MetricsObserverServer)
            };

            // запускаем поток
            _thread.Start();
            _started = true;
        }

        /// <summary>
        /// Остановка сервера
        /// </summary>
        public void Stop()
        {
            if (!_started)
                return;

            _metricsListener.Stop();
            _thread.Join(); // ожидаем завершения работы потока
            _started = false;
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_started)
                Stop();

            MetricsStorage.Dispose();
            _disposed = true;
        }
    }
}
