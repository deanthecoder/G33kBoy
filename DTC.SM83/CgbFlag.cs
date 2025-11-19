namespace DTC.SM83;

[Flags]
public enum CgbFlag : byte
{
    None = 0x00,
    CgbSupported = 0x80,
    CgbOnly = 0xC0
}