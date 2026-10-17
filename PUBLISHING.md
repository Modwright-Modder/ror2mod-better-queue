# Publishing

The repository is prepared for `https://github.com/Modwright-Modder/ror2mod-better-queue`. Check the diff, commit and push when ready. No push or Thunderstore upload is performed by the packaging script.

1. Install .NET 9 and PowerShell 7. Run the build and standalone test commands in README.md.
2. For a release, update the BepInPlugin version, `thunderstore/manifest.json`, changelog and package examples together.
3. Run `pwsh ./scripts/package.ps1`. The ZIP includes manifest, README, icon, changelog, license notices and `plugins/BetterQueue/BetterQueue.dll`.
4. Test native Tab hold/toggle/rebound controls, F7, no Queue header Close button, a scoreboard without search, lobby search, category sidebar scrolling and highlight, loop icon placement/green state per category, mouse tooltips disappearing on exit without returning while the last button stays selected, gamepad navigation details, LookingGlass 1.16.3 current/next-stack descriptions and fallback without it, stage changes, lobby/survivor autosave, permanent versus temporary/rental pickups, Command selection and host/client co-op. Keep old configs/builds backed up.
5. Upload the validated ZIP through Thunderstore's Risk of Rain 2 package submission under your chosen team. Review listing metadata and dependencies before submitting.

The repo/package contains neither proprietary game binaries nor user profiles. GitHub Actions uploads a build artifact; it does not publish a release automatically.
