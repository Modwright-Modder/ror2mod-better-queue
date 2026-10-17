# Changelog

## 1.3.1

- Remove the Queue header Close button. Keep the native Tab hold/toggle controls and F7 shortcut; reclaim header width for autosave status.
- Release build, 9,670 existing scheduler/layout assertions and installed-game/HookGen API visibility audits pass. Live UI verification remains manual.

## 1.3.0

- Replace the category dropdown with a persistent, scrollable left sidebar, tier color markers and an active-category highlight. Keep the native player-list width, UI assets and scoreboard blur.
- Replace Loop text with original vector two-arrow artwork. Place it beside the queued item row; icon and frame turn green when looping is enabled for that category.
- Remove search from the in-run scoreboard and remove its temporary pin, pause and chat hooks. Lobby search remains available.
- Pass scoreboard ownership directly when building an inactive HUD, so it cannot be mistaken for the lobby editor.
- Stop selected mouse items from reopening their tooltip after pointer exit. Explicitly remove the item's tooltip on exit; preserve LookingGlass descriptions and gamepad navigation details.
- Release build, 9,670 scheduler/layout assertions, native RoR2 member audit and installed HookGen visibility audit pass. Live UI, tooltip exit and host/client checks remain manual.

## 1.2.2

- Use full localized item descriptions in library and planned-queue tooltips. Optionally call LookingGlass's existing item formatter with the local master, current item count and next-copy preview; refresh on hover/select. Keep LookingGlass optional and fall back safely when its formatter is unavailable.
- Clicking the scoreboard search field temporarily pins the native scoreboard so Tab can be released while typing. Add a native-style Close button and editing hint. Tab, Esc, F7 or Close exits; restore the prior toggle setting on hide, detach and disable.
- Keep Enter in the selected search field from opening game chat. Intercept pause only for this pinned editing session; ordinary scoreboard and pause behavior remain native.
- Release build, 9,202 assertions, native RoR2 API audit and installed HookGen visibility audit pass. Live LookingGlass values and keyboard/UI behavior still need in-game verification.

## 1.2.1

- Fix overlapping scoreboard headers by preserving native horizontal sizing and padding. Place Queue below the player strips in the existing vertical layout instead of over the objective/stats column.
- Reuse the native pickup UI button sprite, material, colors and language-aware RoR2 font. Remove teal styling, duplicated headings and the unsupported dropdown glyph.
- Use a compact toolbar, fixed-size queue row, searchable scrolling item library and legible quantity badges. The editor stays transparent over the existing scoreboard blur.
- Add geometry regression coverage for compact lobby and co-op scoreboard sizes. Release build, 9,194 assertions, native RoR2 API and installed HookGen visibility audits pass. Live visual and multiplayer checks still require an in-game session.

## 1.2.0

- Move the in-run queue editor into the native Tab scoreboard beside the existing player list. Reuse its canvas, cursor and post-process blur; remove the separate editor black backdrop and always-on bottom preview strip.
- Use transparent queue/list surfaces, native scoreboard visibility/rebinding/toggle behavior, and an optional F7 toggle of the same scoreboard. Preserve lobby editing, per-survivor autosave, reservations and live run progress.
- Restore original scoreboard/player-container rectangles when detached or disabled. Attach on scoreboard-open and when the local HUD changes across stages.
- Replace the inaccessible private nested PickupMessage Deserialize hook with the public HookGen HandlePickupMessage hook. Decode a copy through MessageBase, preserve the live reader position, count permanent added quantities and ignore temporary/rental pickups.
- Release build, 8,998 scheduler assertions, native RoR2 API audit and installed HookGen visibility audit pass. In-game native layout/blur and live receipt/host-client verification remain manual.

## 1.1.1

- Respect the viewing camera's HUD visibility so the queue strip stays hidden during casino machine/table views, even when no screen button owns UI focus.


## 1.1.0

- Replace verbose build rows and per-row controls with compact clickable icons, quantity badges and drag reordering.
- Use one category selector, a Loop toggle and a larger searchable item grid.
- Move the HUD preview to a short bottom-centre icon strip; hide it while another interface owns focus.
- Narrow the in-run editor while preserving all survivor plans and live-run scheduling.


## 1.0.0

- New Better Queue plugin identity and standalone build/run scheduler.
- Autosaved survivor builds with stable entry IDs and non-destructive legacy survivor import.
- Compact Queue tab after Loadout: category selector, search, numbered steps, quantities and reorder controls.
- In-run F7 editor and next-item preview.
- Permanent ground-pickup reservations, order-preserving repeat cycles and fulfilled-unit reconciliation.
- Raw picker transport compatibility and destroyed inventory-display subscription cleanup.
