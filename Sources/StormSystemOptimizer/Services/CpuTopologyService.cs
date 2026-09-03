using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace StormSystemOptimizer.Services
{
    public enum CpuTopologyKind
    {
        Unknown,
        SingleDomain,
        IntelHybrid,
        AmdAsymmetricCache,
        MultiCcdSymmetric
    }

    public class CpuSetEntry
    {
        public uint Id { get; set; }
        public ushort Group { get; set; }
        public byte LogicalProcessorIndex { get; set; }
        public byte CoreIndex { get; set; }
        public byte LastLevelCacheIndex { get; set; }
        public byte NumaNodeIndex { get; set; }
        public byte EfficiencyClass { get; set; }
        public bool IsParked { get; set; }
        public ulong LastLevelCacheBytes { get; set; }
    }

    public class LlcDomainInfo
    {
        public byte LastLevelCacheIndex { get; set; }
        public ulong L3Bytes { get; set; }
        public List<byte> LogicalProcessors { get; } = new();
        public List<uint> CpuSetIds { get; } = new();
    }

    public class CpuNamedMask
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<uint> CpuSetIds { get; } = new();
        public List<byte> LogicalProcessors { get; } = new();
    }

    public class CpuTopologySnapshot
    {
        public CpuTopologyKind Kind { get; set; } = CpuTopologyKind.Unknown;
        public string ClassificationName { get; set; } = "Определение...";
        public string Summary { get; set; } = string.Empty;
        public int TotalLogicalProcessors { get; set; }
        public int TotalPhysicalCores { get; set; }
        public bool HasSmt { get; set; }
        public List<CpuSetEntry> Entries { get; } = new();
        public List<LlcDomainInfo> Domains { get; } = new();
        public List<CpuNamedMask> DerivedMasks { get; } = new();

        public CpuNamedMask? DefaultGameMask { get; set; }
        public CpuNamedMask? DefaultBackgroundMask { get; set; }
    }

    public class CpuTopologyService
    {
        private static CpuTopologyService? _instance;
        public static CpuTopologyService Instance => _instance ??= new CpuTopologyService();

        public CpuTopologySnapshot CurrentTopology { get; private set; }

        private CpuTopologyService()
        {
            CurrentTopology = DetectTopology();
        }

        public CpuTopologySnapshot DetectTopology()
        {
            var snapshot = new CpuTopologySnapshot();
            try
            {
                var entries = ReadSystemCpuSets();
                var cacheMap = ReadL3CacheDomains();

                // Merge L3 Cache size into entries
                foreach (var entry in entries)
                {
                    if (cacheMap.TryGetValue(entry.LastLevelCacheIndex, out ulong bytes))
                    {
                        entry.LastLevelCacheBytes = bytes;
                    }
                    snapshot.Entries.Add(entry);
                }

                snapshot.TotalLogicalProcessors = entries.Count;
                var physicalCores = entries.Select(e => e.CoreIndex).Distinct().Count();
                snapshot.TotalPhysicalCores = physicalCores > 0 ? physicalCores : entries.Count;
                snapshot.HasSmt = snapshot.TotalLogicalProcessors > snapshot.TotalPhysicalCores;

                // Group by LLC Domain
                var domainDict = new Dictionary<byte, LlcDomainInfo>();
                foreach (var entry in entries)
                {
                    if (!domainDict.TryGetValue(entry.LastLevelCacheIndex, out var domain))
                    {
                        domain = new LlcDomainInfo
                        {
                            LastLevelCacheIndex = entry.LastLevelCacheIndex,
                            L3Bytes = entry.LastLevelCacheBytes
                        };
                        domainDict[entry.LastLevelCacheIndex] = domain;
                    }
                    domain.LogicalProcessors.Add(entry.LogicalProcessorIndex);
                    domain.CpuSetIds.Add(entry.Id);
                }
                snapshot.Domains.AddRange(domainDict.Values.OrderBy(d => d.LogicalProcessors.FirstOrDefault()));

                // Classifier
                ClassifyTopology(snapshot);

                // Derive Masks
                DeriveMasks(snapshot);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CpuTopologyService] Detection error: {ex.Message}");
                snapshot.ClassificationName = "Стандартная (Single Domain)";
                snapshot.Summary = "Используются стандартные потоки Windows";
                snapshot.Kind = CpuTopologyKind.SingleDomain;
            }

            CurrentTopology = snapshot;
            return snapshot;
        }

        private void ClassifyTopology(CpuTopologySnapshot snapshot)
        {
            var effClasses = snapshot.Entries.Select(e => e.EfficiencyClass).Distinct().OrderBy(x => x).ToList();

            // Case A: Intel Hybrid (P-Cores vs E-Cores)
            if (effClasses.Count > 1)
            {
                snapshot.Kind = CpuTopologyKind.IntelHybrid;
                snapshot.ClassificationName = "Intel Hybrid (P/E-ядра)";
                snapshot.Summary = $"Обнаружена гибридная архитектура: {snapshot.TotalPhysicalCores} ядер, P-ядра высокой мощности и энергоэффективные E-ядра.";
                return;
            }

            // Case B: AMD Asymmetric Cache (X3D with Dual-CCD)
            if (snapshot.Domains.Count > 1)
            {
                var distinctL3 = snapshot.Domains.Select(d => d.L3Bytes).Where(b => b > 0).Distinct().ToList();
                if (distinctL3.Count > 1)
                {
                    snapshot.Kind = CpuTopologyKind.AmdAsymmetricCache;
                    snapshot.ClassificationName = "AMD Dual-CCD 3D V-Cache (X3D)";
                    var maxL3 = snapshot.Domains.Max(d => d.L3Bytes);
                    snapshot.Summary = $"Обнаружен 3D V-Cache: CCD0 ({FormatBytes(maxL3)} L3) для максимального FPS в играх и второй частотный CCD для фоновых задач.";
                    return;
                }

                // Case C: Multi-CCD Symmetric
                snapshot.Kind = CpuTopologyKind.MultiCcdSymmetric;
                snapshot.ClassificationName = "Multi-CCD Symmetric (AMD / Dual-Die)";
                snapshot.Summary = $"Обнаружено несколько симметричных кристаллов CCD ({snapshot.Domains.Count} шт.) с равным размером L3 кэша.";
                return;
            }

            // Case D: Single Domain
            snapshot.Kind = CpuTopologyKind.SingleDomain;
            snapshot.ClassificationName = "Single Cache Domain (Монолитное ядро / 1 CCD)";
            snapshot.Summary = snapshot.HasSmt
                ? $"Единый домен L3 кэша. Доступна оптимизация No SMT для исключения межпоточной задержки."
                : "Единый домен L3 кэша без SMT.";
        }

        private void DeriveMasks(CpuTopologySnapshot snapshot)
        {
            snapshot.DerivedMasks.Clear();

            // 1. All
            var allMask = new CpuNamedMask
            {
                Name = "All",
                Description = "Все доступные потоки процессора"
            };
            foreach (var e in snapshot.Entries)
            {
                allMask.CpuSetIds.Add(e.Id);
                allMask.LogicalProcessors.Add(e.LogicalProcessorIndex);
            }
            snapshot.DerivedMasks.Add(allMask);

            // 2. All No SMT (1 thread per core)
            if (snapshot.HasSmt)
            {
                var noSmtMask = new CpuNamedMask
                {
                    Name = "All (No SMT)",
                    Description = "Только основные физические ядра без гиперпоточности (минимальный инпут-лаг)"
                };
                var seenCores = new HashSet<byte>();
                foreach (var e in snapshot.Entries)
                {
                    if (seenCores.Add(e.CoreIndex))
                    {
                        noSmtMask.CpuSetIds.Add(e.Id);
                        noSmtMask.LogicalProcessors.Add(e.LogicalProcessorIndex);
                    }
                }
                snapshot.DerivedMasks.Add(noSmtMask);
            }

            // Architecture specific masks
            switch (snapshot.Kind)
            {
                case CpuTopologyKind.IntelHybrid:
                    {
                        var hiEff = snapshot.Entries.Max(e => e.EfficiencyClass);
                        var loEff = snapshot.Entries.Min(e => e.EfficiencyClass);

                        var pMask = new CpuNamedMask
                        {
                            Name = "P-cores",
                            Description = "Производительные ядра для игр (Performance)"
                        };
                        var eMask = new CpuNamedMask
                        {
                            Name = "E-cores",
                            Description = "Энергоэффективные ядра для фона (Efficient)"
                        };

                        foreach (var en in snapshot.Entries)
                        {
                            if (en.EfficiencyClass == hiEff)
                            {
                                pMask.CpuSetIds.Add(en.Id);
                                pMask.LogicalProcessors.Add(en.LogicalProcessorIndex);
                            }
                            else if (en.EfficiencyClass == loEff)
                            {
                                eMask.CpuSetIds.Add(en.Id);
                                eMask.LogicalProcessors.Add(en.LogicalProcessorIndex);
                            }
                        }

                        snapshot.DerivedMasks.Insert(0, eMask);
                        snapshot.DerivedMasks.Insert(0, pMask);
                        snapshot.DefaultGameMask = pMask;
                        snapshot.DefaultBackgroundMask = eMask;
                        break;
                    }

                case CpuTopologyKind.AmdAsymmetricCache:
                    {
                        var sortedDomains = snapshot.Domains.OrderByDescending(d => d.L3Bytes).ToList();
                        var cacheDomain = sortedDomains[0];
                        var freqDomain = sortedDomains.Count > 1 ? sortedDomains[1] : null;

                        var cacheMask = new CpuNamedMask
                        {
                            Name = "Cache CCD (3D V-Cache)",
                            Description = $"Кристалл с гигантским кэшем L3 ({FormatBytes(cacheDomain.L3Bytes)})"
                        };
                        cacheMask.CpuSetIds.AddRange(cacheDomain.CpuSetIds);
                        cacheMask.LogicalProcessors.AddRange(cacheDomain.LogicalProcessors);

                        snapshot.DerivedMasks.Insert(0, cacheMask);
                        snapshot.DefaultGameMask = cacheMask;

                        if (freqDomain != null)
                        {
                            var freqMask = new CpuNamedMask
                            {
                                Name = "Freq CCD",
                                Description = $"Высокочастотный кристалл без 3D V-Cache ({FormatBytes(freqDomain.L3Bytes)})"
                            };
                            freqMask.CpuSetIds.AddRange(freqDomain.CpuSetIds);
                            freqMask.LogicalProcessors.AddRange(freqDomain.LogicalProcessors);

                            snapshot.DerivedMasks.Insert(1, freqMask);
                            snapshot.DefaultBackgroundMask = freqMask;
                        }
                        break;
                    }

                case CpuTopologyKind.MultiCcdSymmetric:
                    {
                        for (int i = 0; i < snapshot.Domains.Count; i++)
                        {
                            var dom = snapshot.Domains[i];
                            var ccdMask = new CpuNamedMask
                            {
                                Name = $"CCD{i}",
                                Description = $"Кристалл {i} ({FormatBytes(dom.L3Bytes)} L3, {dom.LogicalProcessors.Count} потоков)"
                            };
                            ccdMask.CpuSetIds.AddRange(dom.CpuSetIds);
                            ccdMask.LogicalProcessors.AddRange(dom.LogicalProcessors);
                            snapshot.DerivedMasks.Insert(i, ccdMask);
                        }

                        snapshot.DefaultGameMask = snapshot.DerivedMasks[0];
                        snapshot.DefaultBackgroundMask = snapshot.DerivedMasks.Count > 1 ? snapshot.DerivedMasks[1] : allMask;
                        break;
                    }

                case CpuTopologyKind.SingleDomain:
                default:
                    {
                        var noSmt = snapshot.DerivedMasks.FirstOrDefault(m => m.Name.Contains("No SMT"));
                        snapshot.DefaultGameMask = noSmt ?? allMask;
                        snapshot.DefaultBackgroundMask = allMask;
                        break;
                    }
            }
        }

        // Win32 API: GetSystemCpuSetInformation
        private static List<CpuSetEntry> ReadSystemCpuSets()
        {
            var list = new List<CpuSetEntry>();
            uint requiredLen = 0;
            NativeMethods.GetSystemCpuSetInformation(IntPtr.Zero, 0, out requiredLen, Process.GetCurrentProcess().Handle, 0);

            if (requiredLen == 0) return list;

            IntPtr buffer = Marshal.AllocHGlobal((int)requiredLen);
            try
            {
                if (NativeMethods.GetSystemCpuSetInformation(buffer, requiredLen, out requiredLen, Process.GetCurrentProcess().Handle, 0))
                {
                    int offset = 0;
                    while (offset < requiredLen)
                    {
                        IntPtr ptr = IntPtr.Add(buffer, offset);
                        var info = Marshal.PtrToStructure<NativeMethods.SYSTEM_CPU_SET_INFORMATION>(ptr);

                        if (info.Type == 0) // CpuSetInformation
                        {
                            list.Add(new CpuSetEntry
                            {
                                Id = info.Id,
                                Group = info.Group,
                                LogicalProcessorIndex = info.LogicalProcessorIndex,
                                CoreIndex = info.CoreIndex,
                                LastLevelCacheIndex = info.LastLevelCacheIndex,
                                NumaNodeIndex = info.NumaNodeIndex,
                                EfficiencyClass = info.EfficiencyClass,
                                IsParked = (info.AllFlags & 1) != 0
                            });
                        }

                        offset += (int)info.Size;
                        if (info.Size == 0) break;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return list;
        }

        // Win32 API: GetLogicalProcessorInformationEx(RelationCache)
        private static Dictionary<byte, ulong> ReadL3CacheDomains()
        {
            var dict = new Dictionary<byte, ulong>();
            uint length = 0;
            NativeMethods.GetLogicalProcessorInformationEx(NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, IntPtr.Zero, ref length);

            if (length == 0) return dict;

            IntPtr buffer = Marshal.AllocHGlobal((int)length);
            try
            {
                if (NativeMethods.GetLogicalProcessorInformationEx(NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, buffer, ref length))
                {
                    int offset = 0;
                    byte cacheIndex = 0;
                    while (offset < length)
                    {
                        IntPtr ptr = IntPtr.Add(buffer, offset);
                        var rel = (NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP)Marshal.ReadInt32(ptr, 0);
                        uint size = (uint)Marshal.ReadInt32(ptr, 4);

                        if (rel == NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache)
                        {
                            byte level = Marshal.ReadByte(ptr, 8);
                            uint cacheSize = (uint)Marshal.ReadInt32(ptr, 12);
                            ulong mask = (ulong)Marshal.ReadInt64(ptr, 40); // GroupMask.Mask

                            if (level == 3) // L3 Cache
                            {
                                for (byte bit = 0; bit < 64; bit++)
                                {
                                    if ((mask & (1UL << bit)) != 0)
                                    {
                                        dict[bit] = cacheSize;
                                    }
                                }
                                dict[cacheIndex] = cacheSize;
                                cacheIndex++;
                            }
                        }

                        if (size == 0) break;
                        offset += (int)size;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return dict;
        }

        public static string FormatBytes(ulong bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{(bytes / (1024.0 * 1024 * 1024)):F1} ГБ";
            if (bytes >= 1024 * 1024)
                return $"{(bytes / (1024.0 * 1024)):F0} МБ";
            if (bytes >= 1024)
                return $"{(bytes / 1024.0):F0} КБ";
            return $"{bytes} Б";
        }
    }
}
