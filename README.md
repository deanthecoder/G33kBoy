[![Twitter URL](https://img.shields.io/twitter/url/https/twitter.com/deanthecoder.svg?style=social&label=Follow%20%40deanthecoder)](https://twitter.com/deanthecoder)

<p align="center">
  <img src="img/logo.png" alt="G33kBoy Logo">
</p>

# G33kBoy
A cross-platform Avalonia-based Game Boy emulator. (Work In Progress)

![Application screenshot](img/app.png)

## Purpose
G33kBoy exists so I can learn Game Boy hardware properly, starting from a clean SM83 CPU implementation.  
My [ZX Spectrum emulator](https://github.com/deanthecoder/ZXSpeculator) taught me a lot about emulation, and the Game Boy has a *similar* CPU – but it also includes a dedicated PPU (video) and sound hardware that need to be emulated too. It is also a playground for some CPU performance techniques I wanted to try.

## Keyboard controls
Global key hooks translate the following keys into the Game Boy joypad:

<div style="display: flex; align-items: flex-start; gap: 40px;">
  <div>

  | Keyboard | Joypad |
  |----------|--------|
  | Arrow keys | D-pad |
  | `Z` | B |
  | `X` | A |
  | `C` | Auto-fire A |
  | Space | Select |
  | Enter/Return | Start |
  </div>

  <img src="img/KeyMap.png" alt="Key map" width="200">
</div>

## Emulator features
- **ROM loading from ZIPs** – Load standard `.gb` ROMs directly, or from a `.zip` archive containing a Game Boy ROM.
- **Multiple speed modes** – Cycle between normal, fast, maximum, and pause to match how you want to play or test.
- **On-demand auto-fire** – Enable hardware auto-fire (Hardware → Auto-fire) to have `C` pulse the A button.
- **Ambient blur background** – Optional blurred background so the Game Boy screen stands out while the app blends into your desktop.
- **Original green display** – Toggle a classic four-shade green palette to mimic the original DMG screen.
- **LCD emulation** – Optional high‑fidelity LCD simulation including pixel‑grid structure, sub‑pixel glow, grain, edge shadowing, and per‑pixel diffusion to closely mimic the look of the original DMG screen.
- **Motion blur** – Blend frames together for a persistence-of-vision effect that smooths fast movement.
- **Screenshot capture** – Save the current frame as a TGA screenshot.
- **Tile map export** – Export the current tile map as a TGA image for debugging graphics or capturing assets.

## LCD emulation
![LCD pixel close-up](img/LCDPixels.png)

The Game Boy’s original DMG screen has a distinctive look: soft diffusion, pixel‑grid separation, slight edge‑shadowing, and a subtle shimmer caused by panel grain.  
G33kBoy includes an optional LCD emulation mode that reproduces these characteristics without shaders, using a hand‑tuned software renderer.

**Techniques used:**
- Pixel‑grid outlines for authentic DMG cell structure  
- Per‑pixel grain to simulate panel irregularities  
- Dynamic edge‑shadowing for a recessed‑screen feel  
- Tinted top/bottom/side glow that matches the original greenish bleed  
- High‑quality scaling that preserves “LCD softness” without blur  

LCD emulation can be toggled at runtime and has very little performance overhead thanks to lookup‑table optimisation.

## Status
- ✔ CPU: full instruction set + automated tests
- ✔ PPU: Scanline-based renderer
- ✔ Boot ROM behaviour
- ✔ Battery-backed RAM persistence
- ✔ Zipped ROM loading
- ✔ Audio
- ✔ Cartridge MBCs
- ☐ Gameplay 'roll back'
- ☐ Game Boy Color support

## Highlights
- **SM83 accuracy** – `DTC.SM83` implements the Game Boy CPU, including interrupt handling, prefixed instructions, memory bus, PPU timing, and a mnemonic disassembler to inspect opcode streams.
- **Shared core utilities** – `DTC.Core` provides reusable commands, extensions, converters, and Avalonia helpers so the emulator and any future UI/drivers share common infrastructure.
- **Avalonia UI shell** – `G33kBoy` hosts the emulator in a cross-platform desktop window.
- **Validation suite** – `UnitTests` target the CPU core via NUnit, ensuring regressions are caught early.
- **Battery-backed saves** – Cartridge RAM is checkpointed every few seconds, compressed, and restored when the ROM loads so in-game progress (scores, unlocks, etc.) persists between sessions.

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
