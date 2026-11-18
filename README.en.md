# Tape Art · Probably Stolen mod

Turns the dusty cassette player in your pawnshop into the best-dressed sound on the street.

- Steam Workshop: <https://steamcommunity.com/sharedfiles/filedetails/?id=3841830230>
- Target game version: **1.0.0.x** (manifest declares `1.0.0`)

## What it adds

**Tape Art sign** — a lavender cassette neon sign, 2×2 cells. Leave it in the display case to pull the tape hoarder in; the Tapehead starting perk also grants one.

**Tape hoarder merchant** — stocks five damaged or corrupted cassettes plus one art sign (not repeated if you already own it), and buys **pristine cassettes only**, at the shop's normal counter prices. Buying follows the vanilla negotiation flow: he only evaluates what you put on the counter.

**Wild's network — Tape Art** (base tab, after the supplier card)

| Tier | Cost | Effect |
|---|---|---|
| I | 225 | Two pristine cassettes on the back counter; merchant visits tomorrow and at least weekly afterwards; unlocks repairing damaged cassettes in the storage bay with common electronic parts |
| II | 540 | The cassette player gains a core; pristine cassettes in the playback bay raise store appeal; credits are charged nightly and electronic parts reduce the cost |

**Cassette player core** — +20 store appeal per pristine cassette in the playback bay (max 9 → 180). Only the playback bay counts, never storage. The core cannot be removed once installed, and the player looks completely vanilla until tier II is unlocked.

**Calendar nightly report** — the tape portion of the appeal bar is drawn in lavender and labelled 磁带, with the nightly credit cost listed underneath.

## Install

Subscribe on the Steam Workshop, or drop `manifest.xml`, a compiled `TapeArt.dll`, `Localization/` and `Textures/` into `Probably Stolen/Mods/tape_art/`. Enable it in the in-game Mods menu and restart.

## Build from source

Requires .NET SDK 6+ (the project targets `netstandard2.1`) and the game's managed assemblies.

```powershell
# references E:\STEAM\steamapps\common\Probably Stolen by default; override with -p:GameDir=...
dotnet build src/TapeArt/TapeArt.csproj -c Release
```

Output: `src/TapeArt/bin/Release/netstandard2.1/TapeArt.dll`.

**Debugging**: release builds omit the F9 test buttons. Rebuild with `-p:DefineConstants=TAPE_ART_DEBUG` to get them (requires an empty `dev.txt` in the game folder).

## Layout

```
src/TapeArt/          mod source and assets
  *.cs                IMod entry, hooks, patches, items, merchant, calendar bar
  Localization/       zh.csv / en.csv (text lives outside the DLL)
  Textures/Items/     sign, core and icon pixel art
  tools/make_art.py   generates those PNGs (Python + Pillow)
docs/                 workshop copy, verification checklist, design notes, release log
```

## Design notes

- Everything goes through the game's `ModHook` / `ModHelper` API when possible. Harmony is only used where vanilla exposes no entry point: adding the core slot, stacking store appeal, the calendar bar segment, electronic-part interactions, the showcase prediction row and the upgrade price fallback.
- All player-facing text lives in CSV files with built-in Chinese fallbacks, so a missing translation never shows a raw key.
- Vanilla references and the pitfalls found along the way are documented in [docs/DESIGN.md](docs/DESIGN.md).

## License

MIT, see [LICENSE](LICENSE).
