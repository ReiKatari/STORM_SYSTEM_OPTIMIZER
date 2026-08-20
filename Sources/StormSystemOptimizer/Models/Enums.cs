namespace StormSystemOptimizer.Models
{
    public enum OptimizationCategory
    {
        JunkAndCache,
        MemoryRam,
        StartupApps,
        WindowsServices,
        NetworkAndDns,
        PrivacyTelemetry,
        SystemHealth,
        PowerAndVisual
    }

    public enum RiskLevel
    {
        Safe,         // 100% safe, no side effects
        Recommended,  // Recommended for optimal performance
        Advanced      // Optional tweaks for power users
    }
}
