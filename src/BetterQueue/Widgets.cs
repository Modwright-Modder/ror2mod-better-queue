using System.Linq;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterQueue
{
    internal static class Widgets
    {
        internal static readonly Color Muted = new Color(.77f, .79f, .82f);
        private static TMP_FontAsset font;
        private static HGButton nativeButton;
        internal static void PrepareNativeStyle()
        {
            if(nativeButton)return;
            var cube=RoR2.Artifacts.CommandArtifactManager.commandCubePrefab;
            var panel=cube?cube.GetComponent<PickupPickerController>()?.panelPrefab:null;
            var source=panel?panel.GetComponentsInChildren<HGButton>(true).FirstOrDefault(b=>b.name=="CancelButton"):null;
            if(!source)return;
            nativeButton=source;
            font=source.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
        }
        internal static RectTransform Rect(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }
        internal static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        internal static TextMeshProUGUI Text(Transform parent, string value, int size = 16, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            PrepareNativeStyle();
            var text = Rect(parent, "Text").gameObject.AddComponent<HGTextMeshProUGUI>();
            // HGTextMeshProUGUI follows RoR2's language-specific default font.
            text.font = HGTextMeshProUGUI.defaultLanguageFont ? HGTextMeshProUGUI.defaultLanguageFont : font ? font : TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color ?? Color.white;
            text.alignment = align;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        internal static Button Button(Transform parent, string title, UnityEngine.Events.UnityAction action)
        {
            var root = Rect(parent, "Button " + title);
            var image = root.gameObject.AddComponent<Image>();
            var button = root.gameObject.AddComponent<MPButton>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            PrepareNativeStyle();
            image.color=Color.white;
            if(nativeButton&&nativeButton.image)
            {
                image.sprite=nativeButton.image.sprite;image.material=nativeButton.image.material;
                image.type=nativeButton.image.type;
                image.pixelsPerUnitMultiplier=nativeButton.image.pixelsPerUnitMultiplier;
                button.transition=nativeButton.transition;button.colors=nativeButton.colors;
                button.spriteState=nativeButton.spriteState;
            }
            else
            {
                image.color=new Color(.25f,.28f,.34f,.8f);
                var colors=button.colors;colors.highlightedColor=new Color(1,.92f,.63f);
                colors.selectedColor=colors.highlightedColor;button.colors=colors;
            }
            var label = Text(root, title, 20, Color.white, TextAlignmentOptions.Center);
            Place(label.rectTransform, .04f, .04f, .96f, .96f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 14;
            label.fontSizeMax = 20;
            return button;
        }
        internal static Button ItemButton(Transform parent,UnityEngine.Events.UnityAction action)
        {
            var button=Button(parent,"",action);
            // Native sliced button frame and hover, with a quiet item surface.
            // Do not clone picker behaviours or the scoreboard ItemIcon controller.
            return button;
        }
        internal static void Icon(Transform parent, ItemDef item, float x0, float y0, float x1, float y1)
        {
            var icon = Rect(parent, "Item artwork").gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Place(icon.rectTransform, x0, y0, x1, y1);
            ItemArtwork.Assign(icon, item);
        }
        internal static void Tip(GameObject target, string title, string body, Color color)
        {
            var tip = target.AddComponent<TooltipProvider>();
            tip.overrideTitleText = title;
            tip.overrideBodyText = body;
            tip.titleColor = color;
            tip.bodyColor = Color.white;
        }
        internal static RectTransform Scroll(Transform parent, string name, out RectTransform viewport,bool transparent=false)
        {
            var root = Rect(parent, name);
            viewport = root;
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = transparent?Color.clear:new Color(.07f, .1f, .14f, .5f);
            root.gameObject.AddComponent<RectMask2D>();
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            scroll.viewport = root;
            var content = Rect(root, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            scroll.content = content;
            return content;
        }
        internal static void Scrollbar(RectTransform viewport)
        {
            var track=Rect(viewport,"Vertical scrollbar");
            track.anchorMin=new Vector2(1,0);track.anchorMax=new Vector2(1,1);
            track.sizeDelta=new Vector2(6,-8);track.anchoredPosition=new Vector2(-4,0);
            track.gameObject.AddComponent<Image>().color=new Color(1,1,1,.12f);
            var handle=Rect(track,"Handle");Place(handle,0,0,1,1);
            var image=handle.gameObject.AddComponent<Image>();image.color=new Color(1,1,1,.65f);
            var scrollbar=track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            scrollbar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic=image;scrollbar.handleRect=handle;
            var scroll=viewport.GetComponent<ScrollRect>();
            scroll.verticalScrollbar=scrollbar;
            scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        }
        internal static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
        }
    }
}
