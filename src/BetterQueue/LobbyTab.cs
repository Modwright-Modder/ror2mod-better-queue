// HGHeaderNavigationController integration adapted from the MIT-licensed
// CommandQueue lobby extension. Full notice in LICENSE-CommandQueue.txt.
using System.Linq;
using System.Reflection;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterQueue
{
    internal sealed class LobbyTab : MonoBehaviour
    {
        private HGHeaderNavigationController navigation;
        private GameObject panel, tab;
        private static bool IsLoadout(HGHeaderNavigationController.Header header, GameObject reference)
        {
            return header.headerButton && (header.headerButton.gameObject == reference ||
                reference && header.headerButton.transform.IsChildOf(reference.transform) ||
                header.headerName?.IndexOf("loadout", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
        internal static void Attach()
        {
            var select = Object.FindObjectOfType<CharacterSelectController>();
            if (select && !select.GetComponent<LobbyTab>())
                select.gameObject.AddComponent<LobbyTab>();
        }
        private void Start()
        {
            var select = GetComponent<CharacterSelectController>();
            navigation = GetComponentsInChildren<HGHeaderNavigationController>(true).FirstOrDefault(n => n.headers != null && n.headers.Any(h => IsLoadout(h, select.loadoutHeaderButton)));
            if (!navigation)
            {
                Debug.LogWarning("[BetterQueue] Lobby navigation unavailable. F7 editor remains available in a run.");
                Destroy(this);
                return;
            }
            int index = navigation.headers.FindIndex(h => IsLoadout(h, select.loadoutHeaderButton));
            var source = navigation.headers[index];
            var root = Widgets.Rect(source.headerRoot.transform.parent, "BetterQueue.BuildEditor");
            panel = root.gameObject;
            var original = (RectTransform)source.headerRoot.transform;
            root.anchorMin = original.anchorMin;
            root.anchorMax = original.anchorMax;
            root.pivot = original.pivot;
            root.sizeDelta = original.sizeDelta;
            root.anchoredPosition = original.anchoredPosition;
            root.gameObject.AddComponent<BuildEditor>().Initialize();
            panel.SetActive(false);
            tab = Instantiate(source.headerButton.gameObject, source.headerButton.transform.parent, false);
            tab.name = "BetterQueue.QueueTab";
            tab.SetActive(true);
            var button = tab.GetComponent<MPButton>();
            button.interactable = true;
            button.onClick = new Button.ButtonClickedEvent();
            if (navigation.headerHighlightObject && navigation.headerHighlightObject.transform.IsChildOf(source.headerButton.transform))
            {
                var highlight = tab.transform.Find(navigation.headerHighlightObject.name);
                if (highlight)
                    Destroy(highlight.gameObject);
            }
            foreach (var skin in tab.GetComponents<RoR2.UI.SkinControllers.ButtonSkinController>())
            {
                skin.useRecommendedButtonWidth = false;
                skin.useRecommendedLabel = false;
            }
            foreach (var language in tab.GetComponentsInChildren<LanguageTextMeshController>(true))
                language.enabled = false;
            foreach (var tag in tab.GetComponentsInChildren<ViewableTag>(true))
                Destroy(tag);
            var text = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            text.text = "Queue";
            tab.transform.SetSiblingIndex(source.headerButton.transform.GetSiblingIndex() + 1);
            navigation.headers.Insert(index + 1, new HGHeaderNavigationController.Header { headerName = "BetterQueue", headerButton = button, tmpHeaderText = text, headerRoot = panel, AreConsolePlatformsSupported = true, isPrimaryPlayerOnly = source.isPrimaryPlayerOnly });
            button.onClick.AddListener(() => navigation.ChooseHeader("BetterQueue"));
            typeof(HGHeaderNavigationController).GetMethod("RebuildHeaders", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(navigation, null);
            for (int i = 0; i < navigation.headers.Count; i++)
            {
                var header = navigation.headers[i].headerButton;
                if (!header)
                    continue;
                var rect = (RectTransform)header.transform;
                var sizing = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
                sizing.minWidth = 0;
                sizing.preferredWidth = 0;
                sizing.flexibleWidth = 1;
                if (!rect.parent.GetComponent<LayoutGroup>())
                    Widgets.Place(rect, i / (float)navigation.headers.Count, 0, (i + 1f) / navigation.headers.Count, 1);
                var label = header.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label)
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 12;
                    label.fontSizeMax = 20;
                }
            }
        }
        private void OnDestroy()
        {
            BetterQueuePlugin.Instance?.Store.Flush();
            if (navigation && navigation.headers != null)
                navigation.headers.RemoveAll(h => h.headerRoot == panel);
            if (panel)
                Destroy(panel);
            if (tab)
                Destroy(tab);
        }
    }
}
