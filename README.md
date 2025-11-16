[![Twitter URL](https://img.shields.io/twitter/url/https/twitter.com/deanthecoder.svg?style=social&label=Follow%20%40deanthecoder)](https://twitter.com/deanthecoder)

<p align="center">
  <img src="img/logo.png" alt="G33kBoy Logo">
</p>

# G33kBoy
An Avalonia-based shell around the `DTC.SM83` Game Boy SM83 CPU implementation, powered by utilities from `DTC.Core`.

## Highlights
- **SM83 accuracy** – `DTC.SM83` implements the Game Boy CPU, including interrupt handling, prefixed instructions, memory bus, PPU timing, and a mnemonic disassembler to inspect opcode streams.
- **Shared core utilities** – `DTC.Core` provides reusable commands, extensions, converters, and Avalonia helpers so the emulator and any future UI/drivers share common infrastructure.
- **Avalonia UI shell** – `G33kBoy` hosts the emulator in a cross-platform desktop window.
- **Validation suite** – `UnitTests` target the CPU core via NUnit, ensuring regressions are caught early.

## Purpose
G33kBoy exists so I can learn Game Boy hardware properly, starting from a clean SM83 CPU implementation.  
My [ZX Spectrum emulator](https://github.com/deanthecoder/ZXSpeculator) taught me a lot about emulation, and the GameBoy has a *similar* CPU - But additionally contains a dedicated PPU (/video) and sound chips which need emulation too. Plus there's some techniques for improving CPU performance I wanted to try out.

## Status
- ✔ CPU: full instruction set + automated tests
- ✔ PPU: Scanline-based renderer
- ✔ Boot ROM behaviour
- ☐ Audio
- ☐ Cartridge MBCs
- ☐ Gameplay 'roll back'
- ☐ GameBoy Color support

## External resources
- `external/GameboyCPUTests/` – CPU tests for regression validation
  https://github.com/adtennant/GameboyCPUTests
- `external/blargg-test-roms/` – Blargg test ROMs to verify accuracy.
  https://github.com/retrio/gb-test-roms
- `external/dmg-acid2.gb` – The Acid2 test ported to the DMG; useful for visual and timing verification.
  https://github.com/mattcurrie/dmg-acid2

## Useful links
- [Pan Docs](https://gbdev.io/pandocs/)
- [Game Boy Doctor](https://github.com/robert/gameboy-doctor/)
- [Game Boy Opcode Generator](https://meganesu.github.io/generate-gb-opcodes/)

## Special thanks
- Quality, feedback, and testing – Lavanya
- Artistic input and advice – Sam (aka Doobie)

## License
Licensed under the MIT License. See [LICENSE](LICENSE) for details.
