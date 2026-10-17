using System.Linq;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BetterQueue
{
    // Queue lives inside the native scoreboard canvas. Native Tab/toggle input,
    // player strips, cursor lifetime, inspect UI and post-process blur remain
    // owned by HUD; no overlay canvas, black backdrop or extra blur is created.
    internal sealed class QueueOverlay : MonoBehaviour
    {
        private HUD hud;
        private RectTransform scoreboard,players,queue;
        private BetterQueuePlugin plugin;
        private RectSnapshot scoreboardOriginal;
        private bool expanded;
        private float nextAttach;
        private struct RectSnapshot
        {
            private Vector2 min,max,offsetMin,offsetMax;
            internal RectSnapshot(RectTransform rect){min=rect.anchorMin;max=rect.anchorMax;offsetMin=rect.offsetMin;offsetMax=rect.offsetMax;}
            internal void Restore(RectTransform rect){if(rect){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=offsetMin;rect.offsetMax=offsetMax;}}
        }
        private void Start(){plugin=BetterQueuePlugin.Instance;ScoreboardController.onScoreboardOpen+=ScoreboardOpened;}
        private void ScoreboardOpened()
        {
            if(!plugin||!plugin.isActiveAndEnabled||!Run.instance)return;
            var target=LocalHud();if(target&&(target!=hud||!queue))Attach(target);
            if(players)LayoutRebuilder.ForceRebuildLayoutImmediate(players);
        }
        private HUD LocalHud()
        {
            var user=LocalUserManager.GetFirstLocalUser();
            return HUD.readOnlyInstanceList.FirstOrDefault(h=>h&&h.localUserViewer==user&&h.scoreboardPanel);
        }
        private void Attach(HUD target)
        {
            Detach();hud=target;scoreboard=target.scoreboardPanel.transform as RectTransform;
            var controller=target.scoreboardPanel.GetComponent<ScoreboardController>();
            players=scoreboard.Find("Container") as RectTransform;
            if(!players&&controller&&controller.container)players=controller.container.parent as RectTransform;
            if(!scoreboard||!players){hud=null;return;}
            scoreboardOriginal=new RectSnapshot(scoreboard);
            Widgets.PrepareNativeStyle();
            // The native vertical layout owns headers, player rows and Queue.
            // Its horizontal padding and fixed-width columns must stay intact.
            queue=Widgets.Rect(players,"BetterQueue.Scoreboard");
            var sizing=queue.gameObject.AddComponent<LayoutElement>();
            sizing.minHeight=320;sizing.preferredHeight=500;sizing.flexibleHeight=1;
            if(controller&&controller.container)queue.SetSiblingIndex(controller.container.GetSiblingIndex()+1);
            queue.gameObject.AddComponent<ScoreboardQueueLifetime>().Owner=this;
            var editor=Widgets.Rect(queue,"Queue editor");Widgets.Place(editor,0,0,1,1);
            editor.gameObject.AddComponent<BuildEditor>().Initialize(this);
            SetExpanded(plugin.Preview.Value);
            queue.gameObject.SetActive(plugin.Preview.Value);
        }
        private void SetExpanded(bool value)
        {
            if(value==expanded)return;expanded=value;
            if(value)
            {
                // Expand vertically only. Narrowing Container breaks the native
                // Player / Key Item / Items / Equipment header alignment.
                scoreboard.anchorMin=new Vector2(scoreboard.anchorMin.x,.13f);
                scoreboard.anchorMax=new Vector2(scoreboard.anchorMax.x,.90f);
                scoreboard.offsetMin=new Vector2(scoreboard.offsetMin.x,0);
                scoreboard.offsetMax=new Vector2(scoreboard.offsetMax.x,0);
            }
            else scoreboardOriginal.Restore(scoreboard);
            LayoutRebuilder.MarkLayoutForRebuild(players);
        }
        internal void Toggle()
        {
            if(!Run.instance)return;
            if(!LocalUserManager.GetFirstLocalUser()?.cachedBody)return;
            var target=LocalHud();if(!target)return;
            if(target.scoreboardPanel.activeSelf)Close();
            else{target.shouldForceScoreboardToggle=true;target.ActivateScoreboard();}
        }
        internal void Close()
        {
            var target=hud?hud:LocalHud();
            if(target&&target.scoreboardPanel.activeSelf){target.shouldForceScoreboardToggle=false;target.DeactivateScoreboard();}
            plugin?.Store.Flush();
        }
        internal void PanelHidden()
        {
            if(queue)foreach(var system in MPEventSystem.readOnlyInstancesList)
            {
                if(!system)continue;var selected=system.currentSelectedGameObject;
                if(selected&&selected.transform.IsChildOf(queue))system.SetSelectedObject(null);
            }
            plugin?.Store.Flush();
        }
        private void Update()
        {
            if(!plugin||!plugin.isActiveAndEnabled){Detach();return;}
            if(Time.unscaledTime>=nextAttach)
            {
                nextAttach=Time.unscaledTime+.5f;
                var target=Run.instance?LocalHud():null;
                if(target!=hud||target&&!queue){if(target)Attach(target);else Detach();}
            }
            if(!queue||!scoreboard)return;
            SetExpanded(plugin.Preview.Value);queue.gameObject.SetActive(plugin.Preview.Value);
        }
        private void Detach()
        {
            if(expanded)scoreboardOriginal.Restore(scoreboard);
            expanded=false;if(queue){queue.gameObject.SetActive(false);Destroy(queue.gameObject);}
            hud=null;scoreboard=players=queue=null;
        }
        private void OnDestroy(){ScoreboardController.onScoreboardOpen-=ScoreboardOpened;Detach();}
    }
    internal sealed class ScoreboardQueueLifetime:MonoBehaviour
    {
        internal QueueOverlay Owner;
        private void OnDisable(){if(Owner)Owner.PanelHidden();}
    }
}
