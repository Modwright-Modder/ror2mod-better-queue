using System;
using System.Reflection;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterQueue
{
    // Optional formatter interop. LookingGlass is never a required dependency,
    // and no LookingGlass implementation or game ItemIcon behaviour is copied.
    internal static class ItemDescription
    {
        private static bool resolved,warned;
        private static Func<ItemDef,int,CharacterMaster,bool,bool,string> describe;
        internal static string For(ItemDef item)
        {
            if(!item)return "This item is not currently installed.";
            if(!resolved)
            {
                resolved=true;
                try
                {
                    var type=Type.GetType("LookingGlass.ItemStatsNameSpace.ItemStats, LookingGlass",false);
                    var method=type?.GetMethod("GetItemDescription",BindingFlags.Public|BindingFlags.Static,null,
                        new[]{typeof(ItemDef),typeof(int),typeof(CharacterMaster),typeof(bool),typeof(bool)},null);
                    if(method!=null&&method.ReturnType==typeof(string))
                        describe=(Func<ItemDef,int,CharacterMaster,bool,bool,string>)Delegate.CreateDelegate(typeof(Func<ItemDef,int,CharacterMaster,bool,bool,string>),method);
                }
                catch(Exception error){Warn(error);}
            }
            if(describe!=null)
            {
                try
                {
                    var master=LocalUserManager.GetFirstLocalUser()?.cachedMaster;
                    int owned=master&&master.inventory?master.inventory.GetItemCountEffective(item.itemIndex):0;
                    // Same inventory context as a Command choice: current stacks
                    // plus the next copy, rather than pretending the plan is owned.
                    string result=describe(item,owned,master,true,false);
                    if(!string.IsNullOrEmpty(result))return result;
                }
                catch(Exception error){Warn(error);}
            }
            string token=Language.IsTokenInvalid(item.descriptionToken)?item.pickupToken:item.descriptionToken;
            return Language.GetString(token);
        }
        private static void Warn(Exception error)
        {
            if(warned)return;warned=true;
            Debug.LogWarning("[BetterQueue] Optional LookingGlass tooltip unavailable; using the full item description. "+error.GetType().Name);
        }
    }

    internal sealed class ItemTooltip:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler
    {
        private ItemDef item;
        private string instructions;
        private TooltipProvider tip;
        internal static void Attach(GameObject target,ItemDef item,string instructions)
        {
            // Register the refresher before TooltipProvider receives hover/select.
            var binding=target.AddComponent<ItemTooltip>();
            binding.item=item;binding.instructions=instructions;
            binding.tip=target.AddComponent<TooltipProvider>();
            binding.tip.AllowTooltipOnNavigationSelect=false;
            binding.Refresh();
        }
        private void Refresh()
        {
            tip.titleToken=item?item.nameToken:"";
            tip.overrideTitleText=item?"":"Missing item";
            var tier=item?ItemTierCatalog.GetItemTierDef(item.tier):null;
            tip.titleColor=tier?ColorCatalog.GetColor(tier.darkColorIndex):Color.white;
            tip.bodyColor=Color.white;
            tip.overrideBodyText=ItemDescription.For(item)+"\n\n<size=85%><color=#BFC3CA>"+instructions+"</color></size>";
        }
        public void OnPointerEnter(PointerEventData data)
        {
            tip.AllowTooltipOnNavigationSelect=false;
            Refresh();
        }
        public void OnPointerExit(PointerEventData data)
        {
            tip.AllowTooltipOnNavigationSelect=false;
            TooltipController.RemoveTooltip(tip);
        }
        public void OnSelect(BaseEventData data)
        {
            var system=EventSystem.current as MPEventSystem;
            tip.AllowTooltipOnNavigationSelect=system&&system.currentInputSource==MPEventSystem.InputSource.Gamepad;
            Refresh();
        }
    }
}
