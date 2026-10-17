using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BetterQueue
{
    internal sealed class PlanStore
    {
        private readonly string path;
        internal PlanDocument Document
        {
            get; private set;
        }
        internal string Error
        {
            get; private set;
        }
        internal bool IsDirty => dirty;
        private bool dirty;
        private float saveAfter;
        private bool damagedPrimary, unsupported;
        internal PlanStore(string configDirectory)
        {
            path = Path.Combine(configDirectory, "BetterQueue.plans.json");
            Document = Read(path) ?? Read(path + ".bak") ?? new PlanDocument();
            Normalize();
        }
        private PlanDocument Read(string file)
        {
            if (!File.Exists(file))
                return null;
            try
            {
                var json = File.ReadAllText(file);
                var value = JsonUtility.FromJson<PlanDocument>(json);
                if (value != null && value.version != 1)
                {
                    unsupported = true;
                    throw new InvalidDataException("Unsupported plan version; the file will be preserved.");
                }
                if (value == null || value.survivors == null || !json.Contains("\"survivors\""))
                    throw new InvalidDataException("Incomplete plan document.");
                return value;
            }
            catch (Exception error) { if (file == path) damagedPrimary = true; Error = error.Message; Debug.LogError("[BetterQueue] Could not read " + file + ": " + error.Message); return null; }
        }
        private void Normalize()
        {
            Document.survivors = Document.survivors.Where(p => p != null && !string.IsNullOrEmpty(p.body)).GroupBy(p => p.body).Select(g => g.First()).ToList();
            foreach (var plan in Document.survivors)
            {
                plan.entries = (plan.entries ?? new List<PlanEntry>()).Where(e => e != null && !string.IsNullOrEmpty(e.item) && e.quantity > 0).ToList();
                plan.repeat = plan.repeat ?? new List<string>();
                var ids = new HashSet<string>();
                foreach (var entry in plan.entries)
                {
                    if (string.IsNullOrEmpty(entry.id) || !ids.Add(entry.id))
                    {
                        entry.id = Guid.NewGuid().ToString("N");
                        ids.Add(entry.id);
                    }
                    entry.quantity = Math.Min(999, entry.quantity);
                }
            }
        }
        internal SurvivorPlan Get(string body)
        {
            var plan = Document.survivors.FirstOrDefault(p => p.body == body);
            if (plan == null)
            {
                plan = new SurvivorPlan { body = body };
                Document.survivors.Add(plan);
            }
            return plan;
        }
        internal void Dirty()
        {
            dirty = true;
            saveAfter = Time.unscaledTime + .35f;
        }
        internal void Tick()
        {
            if (dirty && Time.unscaledTime >= saveAfter)
                Flush();
        }
        internal void Flush()
        {
            if (!dirty)
                return;
            if (unsupported)
            {
                Error = "Unsupported save version; original file preserved.";
                saveAfter = Time.unscaledTime + 5;
                return;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (damagedPrimary && File.Exists(path))
                {
                    File.Copy(path, path + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
                    if (File.Exists(path + ".bak"))
                        File.Copy(path + ".bak", path + ".recovery-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
                    damagedPrimary = false;
                }
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(Document, true));
                if (File.Exists(path))
                    File.Replace(path + ".tmp", path, path + ".bak");
                else
                    File.Move(path + ".tmp", path);
                dirty = false;
                Error = null;
            }
            catch (Exception error) { Error = error.Message; saveAfter = Time.unscaledTime + 5; Debug.LogError("[BetterQueue] Save failed: " + error.Message); }
        }
#pragma warning disable CS0649 // Fields populated by Unity's JSON serializer.
        [Serializable]
        private sealed class LegacyDocument
        {
            public List<LegacyPlan> survivorPlans;
        }
        [Serializable]
        private sealed class LegacyPlan
        {
            public string survivor; public List<LegacyEntry> entries; public List<string> repeatTiers;
        }
        [Serializable]
        private sealed class LegacyEntry
        {
            public string pickup; public int count;
        }
#pragma warning restore CS0649
        internal void ImportLegacy(string configDirectory)
        {
            if (Document.legacyImported)
                return;
            var legacy = Path.Combine(configDirectory, "CommandQueue.saves.json");
            if (File.Exists(legacy))
            {
                try
                {
                    var source = JsonUtility.FromJson<LegacyDocument>(File.ReadAllText(legacy));
                    if (source?.survivorPlans == null)
                        throw new InvalidDataException("Legacy survivorPlans field is missing.");
                    foreach (var old in source.survivorPlans)
                    {
                        if (string.IsNullOrEmpty(old.survivor) || Document.survivors.Any(p => p.body == old.survivor))
                            continue;
                        var plan = Get(old.survivor);
                        foreach (var e in old.entries ?? new List<LegacyEntry>())
                        {
                            if (e.count <= 0 || string.IsNullOrEmpty(e.pickup) || !e.pickup.StartsWith("ItemIndex.", StringComparison.Ordinal))
                                continue;
                            string name = e.pickup.Substring(10);
                            plan.entries.Add(new PlanEntry { item = name, tier = ItemBrowser.TierOf(name), quantity = Math.Min(999, e.count) });
                        }
                        plan.repeat = (old.repeatTiers ?? new List<string>()).Select(ItemBrowser.LegacyTier).Distinct().ToList();
                    }
                    Debug.Log("[BetterQueue] Imported survivor plans; original CommandQueue.saves.json was preserved.");
                }
                catch (Exception error) { Error = error.Message; Debug.LogError("[BetterQueue] Legacy import failed: " + error); return; }
            }
            Document.legacyImported = true;
            Dirty();
            Flush();
        }
    }
}
