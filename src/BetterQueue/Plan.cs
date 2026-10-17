using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterQueue
{
    [Serializable]
    public sealed class PlanEntry
    {
        public string id = Guid.NewGuid().ToString("N");
        public string item;
        public string tier;
        public int quantity = 1;
        public PlanEntry Copy() => new PlanEntry { id = id, item = item, tier = tier, quantity = quantity };
    }
    [Serializable]
    public sealed class SurvivorPlan
    {
        public string body;
        public List<PlanEntry> entries = new List<PlanEntry>();
        public List<string> repeat = new List<string>();
    }
    [Serializable]
    public sealed class PlanDocument
    {
        public int version = 1;
        public bool legacyImported;
        public List<SurvivorPlan> survivors = new List<SurvivorPlan>();
    }
    // Saved build and live run are separate. Collecting items never mutates the
    // saved build. Reservations belong to a specific step, even after an edit.
    public sealed class RunPlan
    {
        private sealed class Step
        {
            internal PlanEntry entry;
            internal int remaining;
        }
        public sealed class Reservation
        {
            public string Item
            {
                get; internal set;
            }
            private Step step;
            internal object Target
            {
                get => step; set => step = (Step)value;
            }
        }
        private readonly List<Step> steps = new List<Step>();
        private readonly List<Reservation> pending = new List<Reservation>();
        private readonly Dictionary<string, int> collected = new Dictionary<string, int>();
        private readonly HashSet<string> loops = new HashSet<string>();
        public int PendingCount => pending.Count;
        public void Start(SurvivorPlan plan)
        {
            steps.Clear();
            pending.Clear();
            collected.Clear();
            loops.Clear();
            foreach (var tier in plan.repeat)
                loops.Add(tier);
            foreach (var entry in plan.entries)
                Add(entry);
        }
        private void Add(PlanEntry entry)
        {
            collected.TryGetValue(entry.id, out var done);
            int left = loops.Contains(entry.tier) ? entry.quantity : Math.Max(0, entry.quantity - done);
            if (left > 0)
                steps.Add(new Step { entry = entry.Copy(), remaining = left });
        }
        public void Reconcile(SurvivorPlan plan, string tier)
        {
            // Preserve fulfillment of other tiers and non-repeating steps.
            // Old reservations remain receipts: a late ground pickup must not
            // accidentally consume a newly edited step with the same item.
            steps.RemoveAll(s => s.entry.tier == tier);
            if (plan.repeat.Contains(tier))
                loops.Add(tier);
            else
                loops.Remove(tier);
            foreach (var entry in plan.entries.Where(e => e.tier == tier))
                Add(entry);
            foreach (var receipt in pending)
            {
                var old = (Step)receipt.Target;
                if (old.entry.tier != tier)
                    continue;
                var replacement = steps.FirstOrDefault(s => s.entry.id == old.entry.id && s.entry.item == old.entry.item);
                if (replacement != null)
                    receipt.Target = replacement;
            }
        }
        public IEnumerable<PlanEntry> Next(Func<string, bool> usable)
        {
            var tiers = new HashSet<string>();
            foreach (var step in steps)
            {
                if (!usable(step.entry.item))
                    continue;
                int reserved = pending.Count(r => ReferenceEquals(r.Target, step));
                if (step.remaining <= reserved || !tiers.Add(step.entry.tier))
                    continue;
                var next = step.entry.Copy();
                next.quantity = step.remaining - reserved;
                yield return next;
            }
        }
        public Reservation Reserve(string entryId)
        {
            var step = steps.FirstOrDefault(s => s.entry.id == entryId && s.remaining > pending.Count(r => ReferenceEquals(r.Target, s)));
            if (step == null)
                return null;
            var receipt = new Reservation { Item = step.entry.item, Target = step };
            pending.Add(receipt);
            return receipt;
        }
        public void Cancel(Reservation reservation)
        {
            if (reservation != null)
                pending.Remove(reservation);
        }
        public void ClearReservations() => pending.Clear();
        public bool Collect(string item)
        {
            var receipt = pending.FirstOrDefault(r => r.Item == item);
            Step step;
            if (receipt != null)
            {
                pending.Remove(receipt);
                step = (Step)receipt.Target;
                if (!steps.Contains(step))
                    return false;
            }
            else
            {
                step = steps.FirstOrDefault(s => s.remaining > 0 && s.entry.item == item);
                if (step == null || steps.FirstOrDefault(s => s.remaining > 0 && s.entry.tier == step.entry.tier) != step)
                    return false;
            }
            collected.TryGetValue(step.entry.id, out var done);
            collected[step.entry.id] = done + 1;
            if (--step.remaining == 0)
            {
                if (!loops.Contains(step.entry.tier))
                    steps.Remove(step);
                else if (!steps.Any(s => s.entry.tier == step.entry.tier && s.remaining > 0))
                {
                    // A complete tier cycle restarts in planned order, even if
                    // reserved ground drops were collected out of order.
                    var cycle = steps.Where(s => s.entry.tier == step.entry.tier).Select(s => s.entry.Copy()).ToArray();
                    steps.RemoveAll(s => s.entry.tier == step.entry.tier);
                    foreach (var entry in cycle)
                        steps.Add(new Step { entry = entry, remaining = entry.quantity });
                }
            }
            return true;
        }
        public int Remaining(string id) => steps.Where(s => s.entry.id == id).Sum(s => s.remaining);
    }
}
