using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace BetterQueue
{
    internal static class ItemBrowser
    {
        internal static List<ItemDef> Items { get; private set; } = new List<ItemDef>();
        private static readonly Dictionary<string, ItemDef> byName = new Dictionary<string, ItemDef>();
        internal static bool Ready => Items.Count > 0;
        internal static void Initialize()
        {
            if (Ready || ItemCatalog.itemCount == 0)
                return;
            foreach (var item in ItemCatalog.allItemDefs)
                if (item)
                {
                    byName[item.name] = item;
                    if (!item.hidden && item.tier != ItemTier.NoTier && item.canRemove)
                        Items.Add(item);
                }
            Items = Items.OrderBy(i => (int)i.tier).ThenBy(Name).ToList();
        }
        internal static ItemDef Find(string key) => key != null && byName.TryGetValue(key, out var item) ? item : null;
        internal static string Name(ItemDef item) => item ? Language.GetString(item.nameToken) : "Missing item";
        internal static string TierOf(string key)
        {
            var item = Find(key);
            return item ? item.tier.ToString() : "Missing";
        }
        internal static string LegacyTier(string key)
        {
            foreach (var value in Items.Select(i => i.tier).Distinct())
            {
                var def = ItemTierCatalog.GetItemTierDef(value);
                if (def && def.name == key)
                    return value.ToString();
            }
            return key?.Replace("TierDef", "").Replace("Def", "") ?? "Missing";
        }
        internal static string TierName(string key)
        {
            switch (key)
            {
                case "Tier1":
                    return "White";
                case "Tier2":
                    return "Green";
                case "Tier3":
                    return "Red";
                case "VoidTier1":
                    return "Void common";
                case "VoidTier2":
                    return "Void uncommon";
                case "VoidTier3":
                    return "Void legendary";
                case "Cursed":
                    return "Hallowed";
                default:
                    return key;
            }
        }
        internal static Color Tint(string tier)
        {
            if (System.Enum.TryParse(tier, out ItemTier value))
            {
                var def = ItemTierCatalog.GetItemTierDef(value);
                if (def)
                    return ColorCatalog.GetColor(def.colorIndex);
            }
            return new Color(.6f, .65f, .7f);
        }
        internal static bool Usable(string key)
        {
            var def = Find(key);
            return def && (!Run.instance || Run.instance.IsPickupAvailable(PickupCatalog.FindPickupIndex(def.itemIndex)));
        }
        internal static string PickupName(PickupIndex index)
        {
            var pickup = PickupCatalog.GetPickupDef(index);
            var def = pickup != null ? ItemCatalog.GetItemDef(pickup.itemIndex) : null;
            return def ? def.name : null;
        }
    }
}
