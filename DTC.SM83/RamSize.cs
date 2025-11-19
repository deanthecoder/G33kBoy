namespace DTC.SM83;

public enum RamSize : byte
{
    None = 0x00,
    RamUnused1 = 0x01,
    Ram8K = 0x02,
    Ram32K = 0x03,
    Ram128K = 0x04,
    Ram64K = 0x05
}