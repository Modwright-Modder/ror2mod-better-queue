# Better Queue

Plan a survivor's Command build in the lobby, then let Command cubes follow the plan during a run.

The **Queue** tab sits after **Loadout**. Its compact editor has a scrollable category sidebar, an icon queue with quantity badges and drag reordering, and an item library. The lobby includes item search; the in-run scoreboard has no search field. Category tabs stay visible on the left, with a scrollbar when they exceed the panel height. Labels use RoR2's language-aware text component; buttons reuse the game's own sprite, material and hover colors. Item icons keep a compact size with quantity badges. The in-run editor is integrated into the game's own scoreboard: the native player list keeps its original width, and Queue flows below it in the same vertical layout. It reuses the scoreboard canvas, cursor and native blur; there is no additional black backdrop or standalone preview strip.

- Click an item to add one; Ctrl-click adds ten. Right-click removes one from its last planned stack.
- In the queue, click an icon to remove one; right-click adds one. Ctrl changes ten. Drag icons to reorder. Scroll the queue if it extends beyond the visible row.
- Item tooltips use full item descriptions. When LookingGlass is installed, Queue calls its existing formatter for colored descriptions and current inventory / next-stack calculations, respecting its calculation settings. Hover refreshes the item context. Mouse tooltips close when the pointer leaves an item, even if that button remains selected; gamepad navigation can still show focused item details. LookingGlass is optional; an unavailable formatter falls back to the full localized description.
- The two-arrow **Loop icon** sits beside the queued items, on the same row. Click it to toggle looping for the selected category; green means enabled. Each category advances independently.
- Changes autosave per survivor. During a run, hold **Tab** (or use the game's rebound/toggle scoreboard control) to see the queue below the existing player list. Edit the build, category, quantities and repeat mode directly there. **F7** is an optional toggle shortcut for that same native scoreboard. The scoreboard follows native hold/toggle behavior. Search is available only in the lobby. Use F7 again to close a scoreboard opened with that shortcut; the header has no Close button. The lobby Queue tab remains available.
- Collected permanent ground items advance the live queue. Temporary/rental items and equipment are left to the ordinary game picker. Scrappers, cauldrons and other pickers are not intercepted.
- The saved build survives a run's item consumption. Non-repeating run edits preserve fulfilled units; changing a repeating category restarts its current cycle.

## Installation and compatibility

Requires BepInExPack and HookGenPatcher. Install `BetterQueue.dll` under `BepInEx/plugins/BetterQueue`. Disable other Command queue plugins to avoid competing selections. Each player manages their own queue; the mod uses the game's normal Command choice transport and does not require a custom server message protocol. Standard network co-op is intended; split-screen is not supported.

The new plugin ID is `modwright.betterqueue`, independent of CommandQueue. On first launch after item catalogs load, survivor builds from `BepInEx/config/CommandQueue.saves.json` are imported into `BetterQueue.plans.json`. The original file stays untouched. Internal item names and stable entry IDs are stored instead of catalog indexes; unknown modded items are retained. Old named save slots are not imported as survivor builds.

Configuration: `BepInEx/config/modwright.betterqueue.cfg`. AutoSelect, ShowPreview (the scoreboard Queue editor) and the optional scoreboard shortcut are configurable. Saves use a temporary file and replacement backup. Damaged primary/recovery files are preserved before repair; unsupported newer save versions are never overwritten.

## Build and package

Use the .NET 9 SDK and PowerShell 7:

```powershell
dotnet build src/BetterQueue/BetterQueue.csproj -c Release
dotnet run --project tests/BetterQueue.Tests -c Release
pwsh ./scripts/package.ps1
```

The Thunderstore package is written to `dist/BetterQueue-1.3.1.zip`. GitHub Actions builds and tests it. See [PUBLISHING.md](PUBLISHING.md).

## Implementation and attribution

Better Queue has a new build/run model, reservation scheduler, survivor store, item browser, editor and HUD. RoR2 lobby-header integration and raw Command picker submission were adapted with attribution from the MIT-licensed CommandQueue ecosystem. Rewriting the presentation does not remove the attribution requirement: see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and [LICENSE-CommandQueue.txt](LICENSE-CommandQueue.txt).

New code is Apache-2.0; adapted integration remains covered by its included MIT notice. Item artwork is loaded from the installed game and is not distributed. No game DLLs, decompiled game sources, user builds or credentials are included.

## Validation

The release builds against RoR2 1.5.0-r.1 / MMHOOK 2026.10.8. The standalone scheduler suite checks 9,670 assertions, including sidebar/loop-row geometry in scoreboard and lobby sizes, reservations, reverse-order ground pickups, repeat cycles, live edits and unchanged saved templates. A local audit against the installed game's actual RoR2 assembly checks member access. Native HUD and installed HookGen API visibility audits pass. The pickup receipt hook no longer calls the private nested PickupMessage HookGen class; it decodes a copied packet through the public MessageBase contract and preserves the native reader position. Permanent receipt quantities advance the queue; temporary/rental receipts do not. Optional LookingGlass formatter lookup was checked against the installed LookingGlass 1.16.3 method signature. Live formatted tooltip values and mouse-exit behavior, category scrolling, loop visuals, native scoreboard hold/toggle behavior, lobby search, receipt transport and host/client integration still require in-game verification.
