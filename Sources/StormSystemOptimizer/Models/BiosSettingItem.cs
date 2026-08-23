using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public enum BiosSettingCategory
    {
        Memory,         // Память и XMP/EXPO
        Graphics,       // Видеокарта и Resizable BAR
        Processor,      // Процессор и PBO/SpeedShift
        StoragePcie,    // Шина PCIe и NVMe Gen 4/5
        BootUefi,       // Загрузка UEFI и CSM
        Cooling         // Вентиляторы и кривые охлаждения
    }

    public partial class BiosSettingItem : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _category = "Память (XMP/EXPO)";

        [ObservableProperty]
        private string _recommendedValue = "Включено (Enabled)";

        [ObservableProperty]
        private string _currentStatus = "Рекомендуется включить";

        [ObservableProperty]
        private string _performanceImpact = "+15% к скорости памяти";

        [ObservableProperty]
        private string _safetyLevel = "100% Безопасно (WHQL/JEDEC)";

        [ObservableProperty]
        private string _explanation = string.Empty;

        [ObservableProperty]
        private string _menuPathAsus = string.Empty;

        [ObservableProperty]
        private string _menuPathMsi = string.Empty;

        [ObservableProperty]
        private string _menuPathGigabyte = string.Empty;

        [ObservableProperty]
        private string _menuPathAsrock = string.Empty;

        [ObservableProperty]
        private string _menuPathEvga = string.Empty;

        [ObservableProperty]
        private string _menuPathNzxt = string.Empty;

        [ObservableProperty]
        private string _menuPathColorful = string.Empty;

        [ObservableProperty]
        private string _menuPathBiostar = string.Empty;

        [ObservableProperty]
        private string _menuPathMaxsun = string.Empty;

        [ObservableProperty]
        private string _menuPathChineseOem = string.Empty; // Huananzhi, Machinist, Jginyue, SZMZ, Onda

        [ObservableProperty]
        private string _menuPathDellAlienware = string.Empty;

        [ObservableProperty]
        private string _menuPathHpOmen = string.Empty;

        [ObservableProperty]
        private string _menuPathLenovoLegion = string.Empty;

        [ObservableProperty]
        private string _menuPathAcerPredator = string.Empty;

        [ObservableProperty]
        private string _menuPathSupermicro = string.Empty;

        [ObservableProperty]
        private string _menuPathIntelNuc = string.Empty;

        [ObservableProperty]
        private string _menuPathGenericAmi = string.Empty;

        [ObservableProperty]
        private string _activeBoardPath = string.Empty;

        [ObservableProperty]
        private bool _isAppliedOrRecommended = true;

        public void ResolveActiveBoardPath(string boardVendor, string boardModel = "", string cpuVendor = "Intel")
        {
            string v = (boardVendor ?? "").ToLowerInvariant();
            string m = (boardModel ?? "").ToLowerInvariant();

            // 1. ASUS / ROG / TUF / ProArt / Prime
            if (v.Contains("asus") || v.Contains("asustek") || v.Contains("rog") || v.Contains("tuf") || v.Contains("proart") || v.Contains("prime") || m.Contains("asus") || m.Contains("rog") || m.Contains("tuf"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathAsus) ? MenuPathAsus : $"Ai Tweaker / Advanced ➔ {Title}";
                return;
            }

            // 2. MSI / Micro-Star / MEG / MPG / MAG / PRO
            if (v.Contains("msi") || v.Contains("micro-star") || v.Contains("meg") || v.Contains("mpg") || v.Contains("mag") || m.Contains("msi") || m.Contains("mortar") || m.Contains("tomahawk") || m.Contains("torpedo") || m.Contains("carbon") || m.Contains("godlike"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathMsi) ? MenuPathMsi : $"OC ➔ Advanced / Settings ➔ {Title}";
                return;
            }

            // 3. Gigabyte / AORUS / Aero / Gaming / Ultra Durable
            if (v.Contains("gigabyte") || v.Contains("aorus") || v.Contains("aero") || m.Contains("aorus") || m.Contains("gigabyte") || m.Contains("b650m") || m.Contains("z790 aorus"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathGigabyte) ? MenuPathGigabyte : $"Tweaker / Settings ➔ {Title}";
                return;
            }

            // 4. ASRock / Taichi / Steel Legend / Phantom Gaming / Pro
            if (v.Contains("asrock") || v.Contains("taichi") || m.Contains("asrock") || m.Contains("steel legend") || m.Contains("phantom gaming") || m.Contains("livemixer"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathAsrock) ? MenuPathAsrock : $"OC Tweaker / Advanced ➔ {Title}";
                return;
            }

            // 5. EVGA (Classified, FTW, Dark, SR-3)
            if (v.Contains("evga") || m.Contains("evga") || m.Contains("dark k|ngp|n") || m.Contains("ftw3"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathEvga) ? MenuPathEvga : (!string.IsNullOrEmpty(MenuPathAsrock) ? MenuPathAsrock.Replace("OC Tweaker", "OverClocking") : $"OverClocking / Advanced ➔ {Title}");
                return;
            }

            // 6. NZXT (N5, N7, N9 Series)
            if (v.Contains("nzxt") || m.Contains("nzxt") || m.Contains("n7-") || m.Contains("n5-") || m.Contains("n9-"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathNzxt) ? MenuPathNzxt : (!string.IsNullOrEmpty(MenuPathAsrock) ? MenuPathAsrock : $"Overclocking / Advanced ➔ {Title}");
                return;
            }

            // 7. Colorful / iGame / CVN / Battle-AX / Colorfly
            if (v.Contains("colorful") || v.Contains("igame") || v.Contains("cvn") || v.Contains("colorfly") || m.Contains("colorful") || m.Contains("igame") || m.Contains("cvn") || m.Contains("battle-ax"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathColorful) ? MenuPathColorful : $"OC ➔ Advanced Memory/CPU / Advanced ➔ {Title}";
                return;
            }

            // 8. Biostar / Racing / Valkyrie / Silver / Hi-Fi
            if (v.Contains("biostar") || m.Contains("valkyrie") || m.Contains("racing") || m.Contains("biostar"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathBiostar) ? MenuPathBiostar : $"O.N.E ➔ Overclocking / Advanced ➔ {Title}";
                return;
            }

            // 9. Maxsun / Soyo / Terminator / Challenger / iCraft
            if (v.Contains("maxsun") || v.Contains("soyo") || m.Contains("maxsun") || m.Contains("terminator") || m.Contains("icraft") || m.Contains("challenger"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathMaxsun) ? MenuPathMaxsun : $"Turbo / OC ➔ Advanced Settings ➔ {Title}";
                return;
            }

            // 10. Huananzhi / Machinist / Jginyue / SZMZ / Onda / JingSha / Chinese X99/X79/B760 boards
            if (v.Contains("huananzhi") || v.Contains("machinist") || v.Contains("jginyue") || v.Contains("szmz") || v.Contains("onda") || v.Contains("jingsha") || v.Contains("qlcs") || v.Contains("alzenit") || m.Contains("x99") || m.Contains("x79") || m.Contains("mr9") || m.Contains("rs9") || m.Contains("qd4") || m.Contains("f8") || m.Contains("tf") || m.Contains("lga2011"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathChineseOem) ? MenuPathChineseOem : $"IntelRCSetup / Advanced ➔ Chipset Configuration ➔ {Title}";
                return;
            }

            // 11. Dell / Alienware (Aurora, Area-51, XPS, OptiPlex, Precision, G-Series)
            if (v.Contains("dell") || v.Contains("alienware") || m.Contains("alienware") || m.Contains("aurora") || m.Contains("optiplex") || m.Contains("precision") || m.Contains("xps"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathDellAlienware) ? MenuPathDellAlienware : $"Performance / Advanced ➔ System Configuration ➔ {Title}";
                return;
            }

            // 12. HP / OMEN / Victus / Pavilion Gaming / EliteDesk / ProDesk / Z-Series
            if (v.Contains("hp") || v.Contains("hewlett-packard") || v.Contains("omen") || v.Contains("victus") || m.Contains("omen") || m.Contains("victus") || m.Contains("elitedesk") || m.Contains("prodesk") || m.Contains("z4") || m.Contains("z6") || m.Contains("z8"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathHpOmen) ? MenuPathHpOmen : $"Advanced ➔ Overclocking / Device Configuration ➔ {Title}";
                return;
            }

            // 13. Lenovo / Legion / IdeaCentre / ThinkCentre / ThinkStation / LOQ
            if (v.Contains("lenovo") || v.Contains("legion") || m.Contains("legion") || m.Contains("thinkcentre") || m.Contains("thinkstation") || m.Contains("ideacentre") || m.Contains("loq"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathLenovoLegion) ? MenuPathLenovoLegion : $"Devices / Advanced ➔ Overclocking Configuration ➔ {Title}";
                return;
            }

            // 14. Acer / Predator / Nitro / Aspire / Veriton
            if (v.Contains("acer") || v.Contains("predator") || m.Contains("predator") || m.Contains("nitro") || m.Contains("aspire") || m.Contains("veriton"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathAcerPredator) ? MenuPathAcerPredator : $"Advanced ➔ Overclocking Configuration / System Agent ➔ {Title}";
                return;
            }

            // 15. Supermicro / Tyan
            if (v.Contains("supermicro") || v.Contains("tyan") || m.Contains("supermicro") || m.Contains("x11") || m.Contains("x12") || m.Contains("x13") || m.Contains("h11") || m.Contains("h12") || m.Contains("h13"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathSupermicro) ? MenuPathSupermicro : $"Advanced ➔ PCIe/PCI/PnP / Chipset Configuration ➔ {Title}";
                return;
            }

            // 16. Intel (NUC, Server, Desktop Reference, OEM)
            if (v.Contains("intel") || m.Contains("nuc") || m.Contains("nuc11") || m.Contains("nuc12") || m.Contains("nuc13"))
            {
                ActiveBoardPath = !string.IsNullOrEmpty(MenuPathIntelNuc) ? MenuPathIntelNuc : $"Performance ➔ Processor/Memory Configuration ➔ {Title}";
                return;
            }

            // 17. Framework / Clevo / Tongfang / Schenker / XMG / Eluktronics
            if (v.Contains("framework") || v.Contains("clevo") || v.Contains("tongfang") || v.Contains("schenker") || v.Contains("xmg") || v.Contains("eluktronics"))
            {
                ActiveBoardPath = $"Advanced ➔ Device Configuration / Overclocking ➔ {Title}";
                return;
            }

            // 18. Universal AMI Aptio / Phoenix / InsydeH2O / Generic fallback
            ActiveBoardPath = !string.IsNullOrEmpty(MenuPathGenericAmi) ? MenuPathGenericAmi : $"Advanced ➔ Chipset / CPU / Memory Configuration ➔ {Title}";
        }
    }
}
