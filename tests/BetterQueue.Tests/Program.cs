using System;
using System.Linq;
using BetterQueue;

static class Program
{
    static int assertions;
    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new Exception(message);
    }
    static PlanEntry Entry(string name, string tier = "Tier1", int quantity = 1) => new PlanEntry { item = name, tier = tier, quantity = quantity };
    static RunPlan Live(params PlanEntry[] entries)
    {
        var run = new RunPlan();
        run.Start(new SurvivorPlan { entries = entries.ToList() });
        return run;
    }
    static void Main()
    {
        // Cover compact lobby, minimum-height co-op scoreboard and wide layouts.
        // Assert usable hit targets and non-overlap rather than exact coordinates.
        foreach(float width in new[]{240f,320f,480f,640f,832f,1024f,1304f})
        foreach(float height in new[]{320f,400f,500f,700f})
        foreach(bool search in new[]{false,true})
        {
            var ui=new QueueEditorLayout(width,height,search);
            Check(ui.Cell>=48 && ui.Cell<=64.01f,"Native-size item hit targets");
            Check(ui.Columns*ui.Cell+(ui.Columns-1)*6+8<=ui.ContentWidth+.01f,"Item library fits beside sidebar");
            Check(ui.PlanColumns*ui.Cell+(ui.PlanColumns-1)*6+8<=ui.PlanWidth+.01f,"Plan row fits without covering loop");
            Check(ui.PlanHeight>=ui.Cell+8,"Queue shows a full icon row");
            Check(ui.ContentLeft>8+ui.SidebarWidth,"Category sidebar does not overlap items");
            Check(ui.LoopLeft>=ui.ContentLeft+ui.PlanWidth+6,"Loop has a separate target beside queue");
            Check(ui.LoopLeft+ui.Cell<=width-8+.01f,"Loop stays inside editor width");
            Check(Math.Abs(ui.LoopTop+ui.Cell*.5f-ui.PlanTop-ui.PlanHeight*.5f)<.01f,"Loop and queued icons share a center line");
            Check(ui.PlanTop+ui.PlanHeight<ui.LibraryHeadingTop,"Queue and item library heading do not overlap");
            Check(ui.LibraryHeadingTop+(search?30:20)<ui.LibraryTop,"Library clears its heading or lobby search");
            Check(ui.LibraryHeight>=ui.Cell+8,"Minimum-height library shows a full row");
            Check(ui.LibraryTop+ui.LibraryHeight<=height-24,"Library leaves footer clear");
        }
        var a = Entry("A", quantity: 2);
        var b = Entry("B");
        var green = Entry("G", "Tier2");
        var plan = new SurvivorPlan { entries = { a, b, green } };
        var run = new RunPlan();
        run.Start(plan);
        Check(run.Next(_ => true).Select(e => e.item).SequenceEqual(new[] { "A", "G" }), "Independent tier heads");
        var r1 = run.Reserve(a.id);
        var r2 = run.Reserve(a.id);
        Check(r1 != null && r2 != null && run.Reserve(a.id) == null, "No duplicate reservations");
        Check(run.Next(_ => true).Select(e => e.item).SequenceEqual(new[] { "B", "G" }), "Pending counts advance cube planning");
        var r3 = run.Reserve(b.id);
        Check(run.Collect("B") && run.Remaining(a.id) == 2 && run.Remaining(b.id) == 0, "Out-of-order ground pickup");
        Check(run.Collect("A") && run.Collect("A") && run.PendingCount == 0, "Ground collection releases receipts");
        Check(a.quantity == 2 && plan.entries.Count == 3, "Run progress never consumes saved build");
        run.Start(plan);
        r1 = run.Reserve(a.id);
        run.Cancel(r1);
        Check(run.Next(_ => true).First().quantity == 2, "Submit failure rolls back reservation");
        Check(!run.Collect("B") && run.Remaining(b.id) == 1, "Manual pickup cannot skip tier order");
        run.ClearReservations();
        Check(run.PendingCount == 0 && run.Remaining(a.id) == 2, "Stage clears receipts without advancing queue");
        Check(run.Next(i => i != "A").First().item == "B", "Unavailable catalog entries do not block usable ones");
        run.Collect("A");
        a.quantity = 3;
        run.Reconcile(plan, "Tier1");
        Check(run.Remaining(a.id) == 2, "Quantity edit preserves fulfilled units");
        run.Reserve(a.id);
        a.quantity = 4;
        run.Reconcile(plan, "Tier1");
        run.Collect("A");
        Check(run.Remaining(a.id) == 2, "Pending receipt survives edit of same stable entry");
        var old = Entry("X");
        var edited = new SurvivorPlan { entries = { old } };
        run.Start(edited);
        run.Reserve(old.id);
        edited.entries.Clear();
        var replacement = Entry("X");
        edited.entries.Add(replacement);
        run.Reconcile(edited, "Tier1");
        Check(!run.Collect("X") && run.Remaining(replacement.id) == 1, "Removed receipt cannot consume new same-name entry");
        var loopA = Entry("A");
        var loopB = Entry("B");
        var loops = new SurvivorPlan { entries = { loopA, loopB }, repeat = { "Tier1" } };
        run.Start(loops);
        run.Reserve(loopA.id);
        run.Reserve(loopB.id);
        run.Collect("B");
        run.Collect("A");
        Check(run.Next(_ => true).First().item == "A", "Repeated cycle keeps planned order despite reverse pickup arrival");
        for (int cycle = 0; cycle < 1000; cycle++)
        {
            var first = run.Next(_ => true).First();
            Check(first.item == "A", "Repeat A head");
            run.Reserve(first.id);
            Check(run.Next(_ => true).First().item == "B", "Repeat B head while A pending");
            run.Reserve(loopB.id);
            if (cycle % 2 == 0)
            {
                run.Collect("A");
                run.Collect("B");
            }
            else
            {
                run.Collect("B");
                run.Collect("A");
            }
            Check(run.PendingCount == 0 && run.Remaining(loopA.id) == 1 && run.Remaining(loopB.id) == 1, "Exactly one restart per cycle");
        }
        var rng = new Random(41014);
        for (int sample = 0; sample < 200; sample++)
        {
            var entries = Enumerable.Range(0, 8).Select(i => Entry("Item" + i, quantity: rng.Next(1, 7))).ToArray();
            run = Live(entries);
            int original = entries.Sum(e => e.quantity);
            var receipts = entries.SelectMany(e => Enumerable.Range(0, e.quantity).Select(_ => run.Reserve(e.id))).ToArray();
            Check(receipts.All(r => r != null) && !run.Next(_ => true).Any(), "Every planned unit reserved exactly once");
            foreach (var receipt in receipts.OrderBy(_ => rng.Next()))
                Check(run.Collect(receipt.Item), "Random pickup order settles reserved units");
            Check(run.PendingCount == 0 && !run.Next(_ => true).Any() && entries.Sum(e => e.quantity) == original, "Fully settled run leaves template unchanged");
        }
        Console.WriteLine("Better Queue: " + assertions + " assertions passed.");
    }
}
