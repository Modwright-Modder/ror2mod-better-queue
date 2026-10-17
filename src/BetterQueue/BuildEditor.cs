using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterQueue
{
    internal sealed class BuildEditor : MonoBehaviour
    {
        private BetterQueuePlugin plugin;
        private TextMeshProUGUI summary,saveStatus,queueHeading,libraryHeading;
        private RectTransform planList,itemList,categoryList;
        private RectTransform planView,itemView,categoryView,searchView,helpView;
        private Button repeat;
        private LoopGlyph repeatIcon;
        private ColorBlock repeatColors,categoryColors;
        private TMP_InputField search;
        private QueueOverlay scoreboardOwner;
        private readonly Dictionary<string,Button> categoryButtons=new Dictionary<string,Button>();
        private string[] displayedTiers=Array.Empty<string>();
        private string tier="Tier1",query="";
        private bool redraw=true;
        private float drawnWidth,drawnHeight;

        internal void Initialize(QueueOverlay owner=null)
        {
            plugin=BetterQueuePlugin.Instance;
            plugin.Changed+=Invalidate;
            // The HUD can be built while its scoreboard is inactive. Do not use
            // an active-parent lookup to decide whether this is the lobby editor.
            scoreboardOwner=owner;
            var root=(RectTransform)transform;
            Widgets.PrepareNativeStyle();
            summary=Widgets.Text(root,"Queue",24);
            saveStatus=Widgets.Text(root,"Autosave",16,Widgets.Muted,TextAlignmentOptions.MidlineRight);

            categoryList=Widgets.Scroll(root,"Categories",out categoryView,true);
            var tabs=categoryList.gameObject.AddComponent<VerticalLayoutGroup>();
            tabs.spacing=4;tabs.padding=new RectOffset(0,10,2,2);
            tabs.childControlWidth=tabs.childControlHeight=true;
            tabs.childForceExpandWidth=true;tabs.childForceExpandHeight=false;
            categoryList.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Widgets.Scrollbar(categoryView);

            repeat=Widgets.Button(root,"",ToggleRepeat);
            repeatColors=repeat.colors;
            var glyph=Widgets.Rect(repeat.transform,"Loop icon");Widgets.Place(glyph,.12f,.12f,.88f,.88f);
            repeatIcon=glyph.gameObject.AddComponent<LoopGlyph>();repeatIcon.raycastTarget=false;
            Widgets.Tip(repeat.gameObject,"Loop category","Repeat this category's item order. Green means enabled.",Color.white);

            planList=Widgets.Scroll(root,"Ordered build",out planView,true);
            AddGrid(planList);
            queueHeading=Widgets.Text(root,"Add items below",16,Widgets.Muted);
            libraryHeading=Widgets.Text(root,"Items",16,Widgets.Muted);
            itemList=Widgets.Scroll(root,"Item library",out itemView,true);
            AddGrid(itemList);
            if(!scoreboardOwner)CreateSearch(root);
            var help=Widgets.Text(root,"Click: add / remove   |   Drag: reorder   |   Ctrl: 10",14,Widgets.Muted,TextAlignmentOptions.Center);
            help.enableAutoSizing=true;help.fontSizeMin=11;help.fontSizeMax=14;helpView=help.rectTransform;
        }
        private static void AddGrid(RectTransform list)
        {
            var grid=list.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
            grid.spacing=new Vector2(6,6);grid.padding=new RectOffset(4,4,4,4);
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        }
        private void CreateSearch(RectTransform root)
        {
            searchView=Widgets.Rect(root,"Lobby item search");
            var image=searchView.gameObject.AddComponent<Image>();image.color=new Color(1,1,1,.12f);
            search=searchView.gameObject.AddComponent<TMP_InputField>();search.targetGraphic=image;
            search.lineType=TMP_InputField.LineType.SingleLine;search.characterLimit=64;
            var area=Widgets.Rect(searchView,"Text area");Widgets.Place(area,.025f,.08f,.98f,.92f);
            area.gameObject.AddComponent<RectMask2D>();
            var input=Widgets.Text(area,"",18);Widgets.Place(input.rectTransform,0,0,1,1);
            var placeholder=Widgets.Text(area,"Search items...",18,Widgets.Muted);Widgets.Place(placeholder.rectTransform,0,0,1,1);
            search.textViewport=area;search.textComponent=input;search.placeholder=placeholder;
            search.onValueChanged.AddListener(value=>{query=value;Invalidate();});
            libraryHeading.gameObject.SetActive(false);
        }
        private string[] Tiers()=>ItemBrowser.Items.Select(i=>i.tier.ToString()).Concat(plugin.Selected?.entries.Select(e=>e.tier)??Enumerable.Empty<string>()).Distinct().ToArray();
        private static void Box(RectTransform rect,float x,float top,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-top);rect.sizeDelta=new Vector2(width,height);
        }
        private void Layout(QueueEditorLayout ui)
        {
            Box(summary.rectTransform,8,0,ui.Width*.55f,32);
            Box(saveStatus.rectTransform,ui.Width*.63f,0,ui.Width*.37f-8,32);
            Box(categoryView,8,38,ui.SidebarWidth,ui.Height-62);
            Box(queueHeading.rectTransform,ui.ContentLeft,38,ui.ContentWidth,20);
            Box(planView,ui.ContentLeft,ui.PlanTop,ui.PlanWidth,ui.PlanHeight);
            Box((RectTransform)repeat.transform,ui.LoopLeft,ui.LoopTop,ui.Cell,ui.Cell);
            Box(libraryHeading.rectTransform,ui.ContentLeft,ui.LibraryHeadingTop,ui.ContentWidth,20);
            if(searchView)Box(searchView,ui.ContentLeft,ui.LibraryHeadingTop,ui.ContentWidth,30);
            Box(itemView,ui.ContentLeft,ui.LibraryTop,ui.ContentWidth,ui.LibraryHeight);
            Box(helpView,8,ui.Height-22,ui.Width-16,20);
            var items=itemList.GetComponent<GridLayoutGroup>();items.constraintCount=ui.Columns;items.cellSize=new Vector2(ui.Cell,ui.Cell);
            var plan=planList.GetComponent<GridLayoutGroup>();plan.constraintCount=ui.PlanColumns;plan.cellSize=items.cellSize;
        }
        private void RenderCategories()
        {
            var keys=Tiers();
            if(!displayedTiers.SequenceEqual(keys))
            {
                displayedTiers=keys;Widgets.Clear(categoryList);categoryButtons.Clear();
                foreach(string key in keys)
                {
                    string selectedTier=key;
                    var button=Widgets.Button(categoryList,ItemBrowser.TierName(key),()=>
                    {
                        tier=selectedTier;
                        planView.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
                        itemView.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
                        Invalidate();
                    });
                    categoryColors=button.colors;
                    var sizing=button.gameObject.AddComponent<LayoutElement>();sizing.minHeight=sizing.preferredHeight=34;
                    var stripe=Widgets.Rect(button.transform,"Tier color");Widgets.Place(stripe,.025f,.16f,.06f,.84f);
                    var color=stripe.gameObject.AddComponent<Image>();color.color=ItemBrowser.Tint(key);color.raycastTarget=false;
                    var label=button.GetComponentInChildren<TextMeshProUGUI>();Widgets.Place(label.rectTransform,.09f,.04f,.96f,.96f);
                    label.fontSizeMin=11;label.fontSizeMax=18;
                    Widgets.Tip(button.gameObject,ItemBrowser.TierName(key),"Edit this category's independent queue.",ItemBrowser.Tint(key));
                    categoryButtons[key]=button;
                }
            }
            foreach(var entry in categoryButtons)
            {
                var colors=categoryColors;
                if(entry.Key==tier)colors.normalColor=new Color(.68f,.61f,.30f);
                entry.Value.colors=colors;
            }
        }
        private void ToggleRepeat()
        {
            if (plugin.Selected == null)
                return;
            if (!plugin.Selected.repeat.Remove(tier))
                plugin.Selected.repeat.Add(tier);
            plugin.Edit(tier);
        }
        private void Invalidate() => redraw = true;
        private void Update()
        {
            if (plugin == null || !ItemBrowser.Ready)
                return;
            var bounds = ((RectTransform)transform).rect;
            if (Math.Abs(bounds.width - drawnWidth) > 2 || Math.Abs(bounds.height-drawnHeight)>2)
            {
                drawnWidth = bounds.width;drawnHeight=bounds.height;
                Layout(new QueueEditorLayout(drawnWidth,drawnHeight,search!=null));
                redraw = true;
            }
            if (redraw)
            {
                redraw = false;
                Render();
            }
            saveStatus.text = plugin.Store.Error != null ? "Save error" : plugin.Store.IsDirty ? "Saving..." : "Autosaved";
        }
        private void Render()
        {
            if (plugin.Selected == null)
            {
                summary.text = "Select a survivor to start a build";
                return;
            }
            var entries = plugin.Selected.entries.Where(e => e.tier == tier).ToArray();
            int total = entries.Sum(e => e.quantity);
            summary.text = "Queue";
            queueHeading.text = ItemBrowser.TierName(tier) + " - " + total + " picks";
            RenderCategories();
            bool looping=plugin.Selected.repeat.Contains(tier);
            repeatIcon.color=looping?new Color(.40f,1,.50f):Color.white;
            var loopColors=repeatColors;
            if(looping)
            {
                loopColors.normalColor=new Color(.20f,.55f,.27f);
                loopColors.highlightedColor=loopColors.selectedColor=new Color(.40f,.90f,.48f);
                loopColors.pressedColor=new Color(.15f,.40f,.20f);
            }
            repeat.colors=loopColors;
            Widgets.Clear(planList);
            for (int index = 0; index < entries.Length; index++)
                BuildRow(entries[index], index + 1);
            Widgets.Clear(itemList);
            foreach (var item in ItemBrowser.Items.Where(i => i.tier.ToString() == tier && (string.IsNullOrEmpty(query) || ItemBrowser.Name(i).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || i.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                var selected = item;
                var button = Widgets.ItemButton(itemList, () => plugin.Add(selected, Amount()));
                Widgets.Icon(button.transform, item, .09f, .14f, .91f, .93f);
                var rule = Widgets.Rect(button.transform, "Tier stripe");
                Widgets.Place(rule, .1f, .02f, .9f, .065f);
                rule.gameObject.AddComponent<Image>().color = ItemBrowser.Tint(tier);
                rule.GetComponent<Image>().raycastTarget = false;
                ItemTooltip.Attach(button.gameObject,item,"Click to add - Ctrl adds 10 - Right-click removes");
                button.gameObject.AddComponent<LibraryClick>().Initialize(item);
                if (!ItemBrowser.Usable(item.name))
                {
                    button.interactable = false;
                }
            }
        }
        internal static int Amount() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 10 : 1;
        private void BuildRow(PlanEntry entry, int order)
        {
            var tile = Widgets.ItemButton(planList, () => Resize(entry, -Amount()));
            var row = (RectTransform)tile.transform;
            var number = Widgets.Text(row, order.ToString(), 13, Widgets.Muted);
            Widgets.Place(number.rectTransform, .05f, .69f, .3f, 1);
            var item = ItemBrowser.Find(entry.item);
            Widgets.Icon(row, item, .1f, .15f, .9f, .93f);
            number.transform.SetAsLastSibling();
            var count = Widgets.Text(row, "×" + entry.quantity, 15, Color.white, TextAlignmentOptions.MidlineRight);
            count.enableAutoSizing=true;count.fontSizeMin=10;count.fontSizeMax=15;
            Widgets.Place(count.rectTransform, .22f, .01f, .94f, .29f);
            ItemTooltip.Attach(row.gameObject,item,"Click -1 - Right-click +1 - Ctrl changes 10\nDrag to reorder" + (Run.instance ? "\n" + plugin.Live.Remaining(entry.id) + " picks left in this run" : ""));
            row.gameObject.AddComponent<PlanDrag>().Initialize(this, entry);
            row.gameObject.AddComponent<QueueClick>().Initialize(this, entry);
        }
        internal void Resize(PlanEntry entry, int delta)
        {
            if (plugin.Selected == null || !plugin.Selected.entries.Contains(entry))
                return;
            entry.quantity = Math.Min(999, entry.quantity + delta);
            if (entry.quantity <= 0)
                plugin.Selected.entries.Remove(entry);
            plugin.Edit(entry.tier);
        }
        internal void Reorder(PlanEntry entry, PlanEntry target)
        {
            if (entry == target || entry.tier != target.tier)
                return;
            int destination = plugin.Selected.entries.IndexOf(target);
            plugin.Selected.entries.Remove(entry);
            plugin.Selected.entries.Insert(Math.Min(destination, plugin.Selected.entries.Count), entry);
            plugin.Edit(entry.tier);
        }
        private void OnDestroy()
        {
            if (plugin)
                plugin.Changed -= Invalidate;
        }
    }
    internal sealed class QueueClick : MonoBehaviour, IPointerClickHandler
    {
        private BuildEditor editor; private PlanEntry entry;
        internal void Initialize(BuildEditor owner, PlanEntry step)
        {
            editor = owner;
            entry = step;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Right)
                editor.Resize(entry, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 10 : 1);
        }
    }
    internal sealed class LibraryClick : MonoBehaviour, IPointerClickHandler
    {
        private ItemDef item;
        internal void Initialize(ItemDef target) => item = target;
        public void OnPointerClick(PointerEventData data)
        {
            var plugin = BetterQueuePlugin.Instance;
            if (data.button != PointerEventData.InputButton.Right || plugin?.Selected == null)
                return;
            var entry = plugin.Selected.entries.LastOrDefault(e => e.item == item.name);
            if (entry == null)
                return;
            entry.quantity -= BuildEditor.Amount();
            if (entry.quantity <= 0)
                plugin.Selected.entries.Remove(entry);
            plugin.Edit(entry.tier);
        }
    }
    internal sealed class PlanDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private BuildEditor editor;
        private PlanEntry entry;
        private CanvasGroup fading;
        internal void Initialize(BuildEditor owner, PlanEntry step)
        {
            editor = owner;
            entry = step;
        }
        public void OnBeginDrag(PointerEventData data)
        {
            fading = gameObject.AddComponent<CanvasGroup>();
            fading.alpha = .5f;
            fading.blocksRaycasts = false;
        }
        public void OnDrag(PointerEventData data)
        {
        }
        public void OnEndDrag(PointerEventData data)
        {
            if (fading)
                Destroy(fading);
        }
        public void OnDrop(PointerEventData data)
        {
            var source = data.pointerDrag ? data.pointerDrag.GetComponent<PlanDrag>() : null;
            if (source && source.editor == editor)
                editor.Reorder(source.entry, entry);
        }
    }
}
