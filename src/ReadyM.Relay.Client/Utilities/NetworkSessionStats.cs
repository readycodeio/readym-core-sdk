using System;
using System.Collections.Generic;
using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace ReadyM.Relay.Client.Utilities
{
    /// <summary>
    /// All-time network session stats:
    /// - Ping distribution via fixed histogram [0..MaxValidPingMs] (exact percentiles)
    /// - Packet loss (ping > MaxValidPingMs) + streak severity
    /// - Upload/Download rate in bytes/sec computed from LiteNetLib NetStatistics deltas (exact percentiles via stored samples)
    ///
    /// Not thread-safe by design (we use a single network thread).
    /// </summary>
    public sealed class NetworkSessionStats
    {
        private const int DefaultMaxValidPingMs = 1000;

        private readonly int _maxValidPingMs;
        private readonly int[] _pingHistogram;
        
        private readonly string _playerId;
        private readonly int _region;

        private long _pingCount;
        private long _pingSum;

        private long _totalPingPackets;
        private long _lostPingPackets;
        private int _currentLossStreak;
        private int _maxLossStreak;

        // Transfer-rate sampling
        private bool _hasTransferBaseline;
        private long _lastBytesSent;
        private long _lastBytesReceived;
        private double _lastSampleTimeSec;

        private readonly RateSeries _uploadBps = new();
        private readonly RateSeries _downloadBps = new();

        public NetworkSessionStats(string playerId, int region, int maxValidPingMs = DefaultMaxValidPingMs)
        {
            if (maxValidPingMs < 1) 
                throw new ArgumentOutOfRangeException(nameof(maxValidPingMs));
            
            _playerId = playerId;
            _region = region;
            
            _maxValidPingMs = maxValidPingMs;
            _pingHistogram = new int[_maxValidPingMs + 1];
        }

        // ---------------------------
        // Ping + loss
        // ---------------------------

        private readonly object _pingLock = new();

        public void AddPing(long pingMs)
        {
            lock (_pingLock)
            {
                _totalPingPackets++;

                if (pingMs > _maxValidPingMs)
                {
                    _lostPingPackets++;
                    _currentLossStreak++;
                    if (_currentLossStreak > _maxLossStreak) _maxLossStreak = _currentLossStreak;
                    return;
                }

                _currentLossStreak = 0;

                int bucket = (int)pingMs;
                if (bucket < 0) bucket = 0;
                if (bucket > _maxValidPingMs) bucket = _maxValidPingMs;

                _pingHistogram[bucket]++;
                _pingCount++;
                _pingSum += pingMs;
            }
        }

        public long TotalPingPackets
        {
            get
            {
                lock (_pingLock)
                {
                    return _totalPingPackets;
                }
            }
        }

        public long LostPingPackets
        {
            get
            {
                lock (_pingLock)
                {
                    return _lostPingPackets;
                }
            }
        }

        public double PingLossRate
        {
            get
            {
                lock (_pingLock)
                {
                    return _totalPingPackets == 0 ? 0.0 : (double)_lostPingPackets / _totalPingPackets;
                }
            }
        }

        public int CurrentLossStreak
        {
            get
            {
                lock (_pingLock)
                {
                    return _currentLossStreak;
                }
            }
        }

        public int MaxLossStreak
        {
            get
            {
                lock (_pingLock)
                {
                    return _maxLossStreak;
                }
            }
        }

        public double PingMeanMs
        {
            get
            {
                lock (_pingLock)
                {
                    return _pingCount == 0 ? 0.0 : (double)_pingSum / _pingCount;
                }
            }
        }

        public long PingMedianMs => PingPercentileMs(0.50);

        public long PingP90Ms => PingPercentileMs(0.90);

        public long PingP95Ms => PingPercentileMs(0.95);

        public long PingP98Ms => PingPercentileMs(0.98);

        public long PingPercentileMs(double p)
        {
            lock (_pingLock)
            {
                if (_pingCount == 0) return 0;
                if (p <= 0) return MinObservedPingMs();
                if (p >= 1) return MaxObservedPingMs();

                long rank = (long)Math.Ceiling(p * _pingCount);
                if (rank < 1) rank = 1;
                if (rank > _pingCount) rank = _pingCount;

                long cumulative = 0;
                for (int ms = 0; ms <= _maxValidPingMs; ms++)
                {
                    int c = _pingHistogram[ms];
                    if (c == 0) continue;

                    cumulative += c;
                    if (cumulative >= rank)
                        return ms;
                }

                return _maxValidPingMs;
            }
        }

        private int MinObservedPingMs()
        {
            for (int ms = 0; ms <= _maxValidPingMs; ms++)
                if (_pingHistogram[ms] != 0)
                    return ms;
            return 0;
        }

        private int MaxObservedPingMs()
        {
            for (int ms = _maxValidPingMs; ms >= 0; ms--)
                if (_pingHistogram[ms] != 0)
                    return ms;
            return 0;
        }

        // ---------------------------
        // Transfer rate (bytes/sec)
        // ---------------------------

        private readonly object _transferLock = new();

        /// <summary>
        /// Call periodically (ideally every ~1 second). Computes bytes/sec samples from LiteNetLib counters.
        /// nowSeconds must be monotonic (e.g., Time.realtimeSinceStartup in Unity).
        /// </summary>
        public void UpdateTransfer(NetStatistics liteNetLibStatistics, double nowSeconds)
        {
            lock (_transferLock)
            {
                var bytesSent = liteNetLibStatistics.BytesSent;
                var bytesReceived = liteNetLibStatistics.BytesReceived;

                if (!_hasTransferBaseline)
                {
                    _hasTransferBaseline = true;
                    _lastBytesSent = bytesSent;
                    _lastBytesReceived = bytesReceived;
                    _lastSampleTimeSec = nowSeconds;
                    return;
                }

                double dt = nowSeconds - _lastSampleTimeSec;
                if (dt <= 0.000_001)
                    return; // ignore bad timestamps

                long deltaSent = bytesSent - _lastBytesSent;
                long deltaRecv = bytesReceived - _lastBytesReceived;

                // LiteNetLib counters should be monotonic; if reset happens, clamp.
                if (deltaSent < 0) deltaSent = 0;
                if (deltaRecv < 0) deltaRecv = 0;

                double uploadBps = deltaSent / dt;
                double downloadBps = deltaRecv / dt;

                _uploadBps.Add(uploadBps);
                _downloadBps.Add(downloadBps);

                _lastBytesSent = bytesSent;
                _lastBytesReceived = bytesReceived;
                _lastSampleTimeSec = nowSeconds;
            }
        }

        public double UploadMeanBps
        {
            get
            {
                lock (_transferLock)
                {
                    return _uploadBps.Mean;
                }
            }
        }

        public double UploadMedianBps
        {
            get
            {
                lock (_transferLock)
                {
                    return _uploadBps.Percentile(0.50);
                }
            }
        }

        public double UploadP90Bps
        {
            get
            {
                lock (_transferLock)
                {
                    return _uploadBps.Percentile(0.90);
                }
            }
        }

        public double UploadP95Bps
        {
            get
            {
                lock (_transferLock)
                {
                    return _uploadBps.Percentile(0.95);
                }
            }
        }

        public double UploadP98Bps
        {
            get
            {
                lock (_transferLock)
                {
                    return _uploadBps.Percentile(0.98);
                }
            }
        }

        public double DownloadMeanBps
        {
            get
            {
                lock (_transferLock)
                {
                    return _downloadBps.Mean;
                }
            }
        }

        public double DownloadMedianBps
        {
            get
            {
                lock (_transferLock)
                {
                    return _downloadBps.Percentile(0.50);
                }
            }
        }

        public double DownloadP90Bps
        {
            get
            {
                lock (_transferLock)
                {
                    return _downloadBps.Percentile(0.90);
                }
            }
        }

        public double DownloadP95Bps
        {
            get
            {
                lock (_transferLock)
                {
                    return _downloadBps.Percentile(0.95);
                }
            }
        }

        public double DownloadP98Bps
        {
            get
            {
                lock (_transferLock)
                {
                    return _downloadBps.Percentile(0.98);
                }
            }
        }

        public int TransferSamples
        {
            get
            {
                lock (_transferLock)
                {
                    return _uploadBps.Count;
                }
            }
        } // same count for upload/download

        public void Reset()
        {
            lock (_pingLock)
            lock (_transferLock)
            {
                Array.Clear(_pingHistogram, 0, _pingHistogram.Length);
                _pingCount = 0;
                _pingSum = 0;
                _totalPingPackets = 0;
                _lostPingPackets = 0;
                _currentLossStreak = 0;
                _maxLossStreak = 0;

                _hasTransferBaseline = false;
                _lastBytesSent = 0;
                _lastBytesReceived = 0;
                _lastSampleTimeSec = 0;

                _uploadBps.Reset();
                _downloadBps.Reset();
            }
        }

        /// Writes a structured log with all stats.
        public void DumpToLog(ILogger logger)
        {
            logger.LogInformation(
                "Network session stats: PlayerId={PlayerId}, Region={Region}, PingLossRate={PingLossRate}, PingMeanMs={PingMeanMs}, PingMedianMs={PingP50Ms}ms, PingP90Ms={PingP90Ms}ms, PingP95Ms={PingP95Ms}ms, PingP98Ms={PingP98Ms}ms, UploadMeanBps={UpMeanBps}, UploadMedianBps={UpP50Bps}, UploadP90Bps={UpP90Bps}, UploadP95Bps={UpP95Bps}, UploadP98Bps={UpP98Bps}, DownloadMeanBps={DownMeanBps}, DownloadMedianBps={DownP50Bps}, DownloadP90Bps={DownP90Bps}, DownloadP95Bps={DownP95Bps}, DownloadP98Bps={DownP98Bps}",
                _playerId, _region, PingLossRate, PingMeanMs, PingMedianMs, PingP90Ms, PingP95Ms, PingP98Ms,
                UploadMeanBps, UploadMedianBps, UploadP90Bps, UploadP95Bps, UploadP98Bps,
                DownloadMeanBps, DownloadMedianBps, DownloadP90Bps, DownloadP95Bps, DownloadP98Bps);
        }

        /// <summary>
        /// Stores rate samples and computes exact percentiles by sorting a copy when dirty.
        /// For ~10k samples, this is cheap and simple.
        /// </summary>
        private sealed class RateSeries
        {
            private readonly List<double> _samples = new(capacity: 12_000);
            private double _sum;

            private bool _dirty = true;
            private double[]? _sortedCache;

            public int Count => _samples.Count;
            public double Mean => _samples.Count == 0 ? 0.0 : _sum / _samples.Count;

            public void Add(double value)
            {
                if (value < 0) value = 0; // defensive
                _samples.Add(value);
                _sum += value;
                _dirty = true;
            }

            public double Percentile(double p)
            {
                if (_samples.Count == 0) return 0.0;
                if (p <= 0) return Min();
                if (p >= 1) return Max();

                EnsureSorted();

                int n = _sortedCache!.Length;
                int rank = (int)Math.Ceiling(p * n) - 1; // nearest-rank
                if (rank < 0) rank = 0;
                if (rank >= n) rank = n - 1;

                return _sortedCache[rank];
            }

            private void EnsureSorted()
            {
                if (!_dirty && _sortedCache != null) return;

                _sortedCache = _samples.ToArray();
                Array.Sort(_sortedCache);
                _dirty = false;
            }

            private double Min()
            {
                EnsureSorted();
                return _sortedCache![0];
            }

            private double Max()
            {
                EnsureSorted();
                return _sortedCache![_sortedCache.Length - 1];
            }

            public void Reset()
            {
                _samples.Clear();
                _sum = 0;
                _dirty = true;
                _sortedCache = null;
            }
        }
    }
}