// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Text;

namespace DTC.SM83;

public sealed class Cartridge
{
    private static readonly Dictionary<string, string> NewLicenseeLookup = new()
    {
        ["00"] = "Unknown",
        ["01"] = "Nintendo",
        ["08"] = "Capcom",
        ["13"] = "Electronic Arts",
        ["18"] = "Hudson Soft",
        ["19"] = "b-ai",
        ["20"] = "KSS",
        ["22"] = "POW",
        ["24"] = "PCM Complete",
        ["25"] = "San-X",
        ["28"] = "Kemco Japan",
        ["29"] = "SETA",
        ["30"] = "Infogrames",
        ["31"] = "Nintendo",
        ["32"] = "Bandai",
        ["33"] = "Ocean/Acclaim",
        ["34"] = "Konami",
        ["35"] = "Hector",
        ["37"] = "Taito",
        ["38"] = "Hudson",
        ["39"] = "Banpresto",
        ["41"] = "Ubi Soft",
        ["42"] = "Atlus",
        ["44"] = "Malibu",
        ["46"] = "Angel",
        ["47"] = "Spectrum Holobyte",
        ["49"] = "Irem",
        ["50"] = "Absolute",
        ["51"] = "Acclaim",
        ["52"] = "Activision",
        ["53"] = "American Sammy",
        ["54"] = "Konami",
        ["55"] = "Hi Tech",
        ["56"] = "LJN",
        ["57"] = "Matchbox",
        ["58"] = "Mattel",
        ["59"] = "Milton Bradley",
        ["60"] = "Titus",
        ["61"] = "Virgin",
        ["64"] = "LucasArts",
        ["67"] = "Ocean",
        ["69"] = "Electronic Arts",
        ["70"] = "Infogrames",
        ["71"] = "Interplay",
        ["72"] = "Broderbund",
        ["73"] = "Sculptered Soft",
        ["75"] = "SCI",
        ["78"] = "THQ",
        ["79"] = "Accolade",
        ["80"] = "Misawa",
        ["83"] = "Lozc",
        ["86"] = "Tokuma Shoten",
        ["87"] = "Tsukuda Original"
    };

    private static readonly Dictionary<byte, string> OldLicenseeLookup = new()
    {
        [0x00] = "Unknown",
        [0x01] = "Nintendo",
        [0x08] = "Capcom",
        [0x09] = "Hot-B",
        [0x0A] = "Jaleco",
        [0x0B] = "Coconuts",
        [0x0C] = "Elite Systems",
        [0x13] = "EA",
        [0x18] = "Hudsonsoft",
        [0x19] = "ITC",
        [0x1A] = "Yanoman",
        [0x1D] = "Clary",
        [0x1F] = "Virgin",
        [0x24] = "PCM Complete",
        [0x25] = "San-X",
        [0x28] = "Kemco",
        [0x29] = "SETA",
        [0x30] = "Infogrames",
        [0x31] = "Nintendo",
        [0x32] = "Bandai",
        [0x33] = "Use new licensee code",
        [0x34] = "Konami",
        [0x35] = "Hector",
        [0x38] = "Capcom",
        [0x39] = "Banpresto",
        [0x3C] = "Entertainment i",
        [0x3E] = "Gremlin",
        [0x41] = "Ubi Soft",
        [0x42] = "Atlus",
        [0x44] = "Malibu",
        [0x46] = "Angel",
        [0x47] = "Spectrum Holobyte",
        [0x49] = "Irem",
        [0x4A] = "Virgin",
        [0x4D] = "Malibu",
        [0x4F] = "U.S. Gold",
        [0x50] = "Absolute",
        [0x51] = "Acclaim",
        [0x52] = "Activision",
        [0x53] = "American Sammy",
        [0x54] = "Gametek",
        [0x55] = "Park Place",
        [0x56] = "LJN",
        [0x57] = "Matchbox",
        [0x59] = "Milton Bradley",
        [0x5A] = "Mindscape",
        [0x5B] = "Romstar",
        [0x5C] = "Naxat",
        [0x5D] = "Tradewest",
        [0x60] = "Titus",
        [0x61] = "Virgin",
        [0x67] = "Ocean",
        [0x69] = "Electronic Arts",
        [0x6E] = "Elite Systems",
        [0x6F] = "Electro Brain",
        [0x70] = "Infogrames",
        [0x71] = "Interplay",
        [0x72] = "Broderbund",
        [0x73] = "Sculptered Soft",
        [0x75] = "SCI",
        [0x78] = "THQ",
        [0x79] = "Accolade",
        [0x7A] = "Triffix",
        [0x7C] = "Microprose",
        [0x7F] = "Kemco",
        [0x83] = "LOZC",
        [0x86] = "Tokuma Shoten",
        [0x8B] = "Bullet-Proof Software",
        [0x8C] = "Vic Tokai",
        [0x8E] = "Ape",
        [0x91] = "Chunsoft",
        [0x92] = "Video System",
        [0x93] = "Ocean/Acclaim",
        [0x95] = "Varie",
        [0x96] = "Yonezawa/S’Pal",
        [0x97] = "Kaneko",
        [0x99] = "Pack-In-Video",
        [0x9A] = "Nichibutsu",
        [0x9B] = "Tecmo",
        [0x9C] = "Imagineer"
    };

    /// <summary>
    /// Creates a new Game Boy cartridge from raw ROM data.
    /// </summary>
    public Cartridge(byte[] romData)
    {
        if (romData == null)
            throw new ArgumentNullException(nameof(romData));

        if (romData.Length < 0x150)
            throw new ArgumentException("ROM is too small to contain a valid Game Boy header.", nameof(romData));

        RomData = romData;

        CgbFlag = (CgbFlag)RomData[0x0143];
        Title = ReadTitle();
        NewLicenseeCode = ReadAscii(0x0144, 2);
        CartridgeType = (CartridgeType)RomData[0x0147];

        RomSizeCode = (RomSize)RomData[0x0148];
        RamSizeCode = (RamSize)RomData[0x0149];

        OldLicenseeCode = RomData[0x014B];

        RomBankCount = CalculateRomBankCount(RomSizeCode);
        RomSizeBytes = CalculateRomSizeBytes(RomSizeCode, RomData.Length);

        RamBankCount = CalculateRamBankCount(RamSizeCode);
        RamSizeBytes = CalculateRamSizeBytes(RamSizeCode);
    }

    public override string ToString()
    {
        var licensee = OldLicenseeCode == 0x33
            ? NewLicenseeLookup.TryGetValue(NewLicenseeCode, out var name) ? name : $"Unknown ({NewLicenseeCode})"
            : OldLicenseeLookup.TryGetValue(OldLicenseeCode, out var older) ? older : $"Unknown ({OldLicenseeCode:X2})";

        return $"Title: {Title}\n" +
               $"Publisher: {licensee}\n" +
               $"Type: {CartridgeType}\n" +
               $"ROM: {RomSizeBytes / 1024}KB\n" +
               $"RAM: {RamSizeBytes / 1024}KB";
    }

    /// <summary>
    /// Raw ROM data for this cartridge, starting at address 0x0000.
    /// </summary>
    public byte[] RomData { get; }

    /// <summary>
    /// Game title from 0x0134-0x0143, trimmed of trailing zeros and spaces.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Color Game Boy capability flags stored at 0x0143.
    /// </summary>
    private CgbFlag CgbFlag { get; }

    /// <summary>
    /// Two-character ASCII “new licensee” code at 0x0144-0x0145 (used when old licensee is 0x33).
    /// </summary>
    private string NewLicenseeCode { get; }

    /// <summary>
    /// Cartridge hardware type (mapper and extras) stored at 0x0147.
    /// </summary>
    public CartridgeType CartridgeType { get; }

    /// <summary>
    /// Encoded ROM size value from 0x0148.
    /// </summary>
    private RomSize RomSizeCode { get; }

    /// <summary>
    /// Encoded RAM size value from 0x0149.
    /// </summary>
    private RamSize RamSizeCode { get; }

    /// <summary>
    /// Legacy publisher code at 0x014B (0x33 means NewLicenseeCode is used).
    /// </summary>
    private byte OldLicenseeCode { get; }

    /// <summary>
    /// Number of 16 KiB ROM banks reported by the header.
    /// </summary>
    public int RomBankCount { get; }

    /// <summary>
    /// Total ROM size in bytes reported by the header (falls back to actual ROM length if header is unknown).
    /// </summary>
    public int RomSizeBytes { get; }

    /// <summary>
    /// Number of 8 KiB external RAM banks reported by the header.
    /// </summary>
    public int RamBankCount { get; }

    /// <summary>
    /// Total external RAM size in bytes reported by the header.
    /// </summary>
    public int RamSizeBytes { get; }

    /// <summary>
    /// True if the cartridge advertises any CGB support.
    /// </summary>
    public bool IsCgbCapable => (CgbFlag & CgbFlag.CgbSupported) != 0;

    /// <summary>
    /// True if the cartridge is marked as CGB-only.
    /// </summary>
    public bool IsCgbOnly => CgbFlag == CgbFlag.CgbOnly;

    private string ReadAscii(int offset, int length) =>
        Encoding.ASCII.GetString(RomData, offset, length);

    private string ReadTitle()
    {
        var usesCgbLayout = (CgbFlag & CgbFlag.CgbSupported) != 0;
        var titleLength = usesCgbLayout ? 11 : 16;
        return ReadAscii(0x0134, titleLength).TrimEnd('\0', ' ');
    }

    private static int CalculateRomBankCount(RomSize size) =>
        size switch
        {
            RomSize.Rom32K => 2,
            RomSize.Rom64K => 4,
            RomSize.Rom128K => 8,
            RomSize.Rom256K => 16,
            RomSize.Rom512K => 32,
            RomSize.Rom1M => 64,
            RomSize.Rom2M => 128,
            RomSize.Rom4M => 256,
            RomSize.Rom8M => 512,
            RomSize.Rom1_1M => 72,
            RomSize.Rom1_2M => 80,
            RomSize.Rom1_5M => 96,
            _ => 0
        };

    private static int CalculateRomSizeBytes(RomSize size, int fallbackLength)
    {
        var banks = CalculateRomBankCount(size);
        return banks == 0 ? fallbackLength : banks * 16 * 1024;

    }

    private static int CalculateRamBankCount(RamSize size) =>
        size switch
        {
            RamSize.None => 0,
            RamSize.Ram8K => 1,
            RamSize.Ram32K => 4,
            RamSize.Ram128K => 16,
            RamSize.Ram64K => 8,
            _ => 0
        };

    private static int CalculateRamSizeBytes(RamSize size) =>
        CalculateRamBankCount(size) * 8 * 1024;

    public (bool IsSupported, string Message) IsSupported()
    {
        var isSupportedType = CartridgeType switch
        {
            CartridgeType.RomOnly or
            CartridgeType.RomRam or
            CartridgeType.RomRamBattery or
            CartridgeType.Mbc1 or
            CartridgeType.Mbc1Ram or
            CartridgeType.Mbc1RamBattery or
            CartridgeType.Mbc3 or
            CartridgeType.Mbc3Ram or
            CartridgeType.Mbc3RamBattery or
            CartridgeType.Mbc3TimerBattery or
            CartridgeType.Mbc3TimerRamBattery or
            CartridgeType.Mbc5 or
            CartridgeType.Mbc5Ram or
            CartridgeType.Mbc5RamBattery or
            CartridgeType.Mbc5Rumble or
            CartridgeType.Mbc5RumbleRam or
            CartridgeType.Mbc5RumbleRamBattery => true,
            _ => false
        };

        return isSupportedType ? (true, null) : (false, $"Cartridge type {CartridgeType} is not supported yet.");
    }
}
