# Third-party notices

The queue feature was inspired by [KubeRoot/CommandQueue](https://github.com/KubeRoot/CommandQueue) and [nickgg11/Nicks-Command-Queue-Flavour](https://github.com/nickgg11/Nicks-Command-Queue-Flavour).

`src/BetterQueue/CommandChoice.cs` uses their documented approach to submit the game's raw picker index, avoiding LookingGlass's stale sorted-panel remapping. `src/BetterQueue/LobbyTab.cs` adapts lobby-header insertion and sizing from the previous MIT-licensed CommandQueue lobby extension. The original copyright and MIT permission text are retained in `LICENSE-CommandQueue.txt` and included in packages.

The scheduler, stable-ID plan model, build/run reconciliation, persistence format, item browser, editor, artwork loading and HUD were newly implemented for Better Queue. This project does not claim that the underlying Command auto-selection concept originated here.

Game item icons are loaded through the installed game's item definitions/Addressables. They are not embedded or redistributed. NuGet dependencies are referenced, not vendored; their own licenses apply.

Package icon: original AI-generated artwork created with the built-in image_gen tool. The full-resolution source and prompt are in artwork/. No game assets are embedded in this icon.
