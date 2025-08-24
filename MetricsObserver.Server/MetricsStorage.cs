using System;
using System.Collections.Generic;
using System.Threading;

namespace MetricsObserver.Server
{
    /// <summary>
    /// Обертка над Dictionary для работы в разных потоках
    /// </summary>
    public class MetricsStorage : IDisposable
    {
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, double> _metrics = new Dictionary<string, double>();

        public bool Any()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _metrics.Count > 0;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public Dictionary<string, double> ReadAll()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return new Dictionary<string, double>(_metrics);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public void AddOrUpdate(string key, double value)
        {
            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (_metrics.TryGetValue(key, out var result))
                {
                    // по-хорошему здесь написать Math.Abs(result - value) < TOLERANCE
                    // но в задаче вроде как не допускается потеря точности
                    // + со значениями не производится никаких операций
                    if (result.Equals(value))
                        return;
                }

                _cacheLock.EnterWriteLock();
                try
                {
                    _metrics[key] = value;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
        }

        public void Dispose()
        {
            _cacheLock?.Dispose();
        }
    }
}
