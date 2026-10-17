using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;

namespace BetterQueue
{
    [BepInPlugin("modwright.betterqueue", "Better Queue", "1.3.1")]
    public sealed class BetterQueuePlugin : BaseUnityPlugin
    {
        internal static BetterQueuePlugin Instance;
        internal PlanStore Store;
        internal SurvivorPlan Selected;
        internal readonly RunPlan Live = new RunPlan();
        internal event Action Changed;
        internal ConfigEntry<bool> AutoSelect, Preview;
        private ConfigEntry<KeyboardShortcut> editorKey;
        private float nextAttach;
        private float nextImport;
        private bool imported, runStarted, enabledHooks;
        private QueueOverlay overlay;
        private static readonly FieldInfo displayInventory = typeof(RoR2.UI.ItemInventoryDisplay).GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo inventoryChanged = typeof(RoR2.UI.ItemInventoryDisplay).GetMethod("OnInventoryChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        private void Awake()
        {
            Instance = this;
            Store = new PlanStore(Paths.ConfigPath);
            AutoSelect = Config.Bind("Queue", "AutoSelect", true, "Automatically select planned permanent items from Command cubes.");
            Preview = Config.Bind("Interface", "ShowPreview", true, "Show the queue editor inside the native Tab scoreboard, using its existing blur and player list.");
            editorKey = Config.Bind("Interface", "EditorKey", new KeyboardShortcut(KeyCode.F7), "Optional shortcut to toggle the native scoreboard with its queue editor. Tab follows the game's own binding.");
        }
        private void OnEnable()
        {
            if (enabledHooks)
                return;
            enabledHooks = true;
            Run.onRunStartGlobal += StartRun;
            Run.onRunDestroyGlobal += EndRun;
            Stage.onStageStartGlobal += StageStarted;
            On.RoR2.PickupPickerController.OnDisplayBegin += CommandChoice.Display;
            On.RoR2.GenericPickupController.HandlePickupMessage += CommandChoice.Collected;
            On.RoR2.UI.ItemInventoryDisplay.OnInventoryChanged += SafeInventoryChanged;
        }
        private void SafeInventoryChanged(On.RoR2.UI.ItemInventoryDisplay.orig_OnInventoryChanged orig, RoR2.UI.ItemInventoryDisplay display)
        {
            // A destroyed HUD can remain subscribed during a scene transition.
            if (!display)
            {
                if (!ReferenceEquals(display, null) && displayInventory != null && inventoryChanged != null)
                {
                    var inventory = displayInventory.GetValue(display) as Inventory;
                    if (inventory)
                        inventory.onInventoryChanged -= (Action)Delegate.CreateDelegate(typeof(Action), display, inventoryChanged);
                }
                return;
            }
            orig(display);
        }
        private void Update()
        {
            ItemBrowser.Initialize();
            if (!ItemBrowser.Ready)
                return;
            if (!imported && Time.unscaledTime >= nextImport)
            {
                nextImport = Time.unscaledTime + 10;
                Store.ImportLegacy(Paths.ConfigPath);
                imported = Store.Document.legacyImported;
            }
            if (!Run.instance || Selected == null)
                SelectSurvivor();
            if (Run.instance && !runStarted)
                StartRun(Run.instance);
            Store.Tick();
            if (Time.unscaledTime >= nextAttach)
            {
                nextAttach = Time.unscaledTime + 1;
                if (!Run.instance)
                    LobbyTab.Attach();
                if (!overlay)
                    overlay = gameObject.AddComponent<QueueOverlay>();
            }
            if (Run.instance && editorKey.Value.IsDown())
                overlay?.Toggle();
        }
        internal void SelectSurvivor()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var body = user?.cachedBody;
            var prefab = Run.instance && body ? BodyCatalog.GetBodyPrefab(body.bodyIndex) : user?.currentNetworkUser ? BodyCatalog.GetBodyPrefab(user.currentNetworkUser.bodyIndexPreference) : null;
            if (!prefab || Selected?.body == prefab.name)
                return;
            Store.Flush();
            Selected = Store.Get(prefab.name);
            foreach (var entry in Selected.entries)
            {
                var tier = ItemBrowser.TierOf(entry.item);
                if (tier != "Missing")
                    entry.tier = tier;
            }
            if (Run.instance)
                Live.Start(Selected);
            Changed?.Invoke();
        }
        private void StartRun(Run run)
        {
            Store.Flush();
            runStarted = true;
            SelectSurvivor();
            if (Selected != null)
                Live.Start(Selected);
            Changed?.Invoke();
        }
        private void EndRun(Run run)
        {
            runStarted = false;
            Live.ClearReservations();
            overlay?.Close();
            Changed?.Invoke();
        }
        private void StageStarted(Stage stage)
        {
            Live.ClearReservations();
            Changed?.Invoke();
        }
        internal void Edit(string tier)
        {
            if (Selected == null)
                return;
            if (Run.instance)
                Live.Reconcile(Selected, tier);
            Store.Dirty();
            Changed?.Invoke();
        }
        internal void PickedUp(string item)
        {
            if (item != null && Live.Collect(item))
                Changed?.Invoke();
        }
        internal void Refresh() => Changed?.Invoke();
        internal void Add(ItemDef item, int amount)
        {
            if (Selected == null || !item)
                return;
            string tier = item.tier.ToString();
            var last = Selected.entries.LastOrDefault(e => e.tier == tier);
            if (last != null && last.item == item.name)
                last.quantity = Math.Min(999, last.quantity + amount);
            else
                Selected.entries.Add(new PlanEntry { item = item.name, tier = tier, quantity = Math.Min(999, amount) });
            Edit(tier);
        }
        private void OnDisable()
        {
            if (!enabledHooks)
                return;
            enabledHooks = false;
            Run.onRunStartGlobal -= StartRun;
            Run.onRunDestroyGlobal -= EndRun;
            Stage.onStageStartGlobal -= StageStarted;
            On.RoR2.PickupPickerController.OnDisplayBegin -= CommandChoice.Display;
            On.RoR2.GenericPickupController.HandlePickupMessage -= CommandChoice.Collected;
            On.RoR2.UI.ItemInventoryDisplay.OnInventoryChanged -= SafeInventoryChanged;
            CommandChoice.Clear();
            Store?.Flush();
            overlay?.Close();
        }
        private void OnDestroy()
        {
            if (overlay)
                Destroy(overlay);
            ItemArtwork.Release();
            if (Instance == this)
                Instance = null;
        }
    }
}
