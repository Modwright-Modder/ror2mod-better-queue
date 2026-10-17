// RoR2 picker transport integration informed by KubeRoot's MIT-licensed
// CommandQueue and Nicks-Command-Queue-Flavour. See THIRD_PARTY_NOTICES.md
// and LICENSE-CommandQueue.txt. Build storage, scheduling and UI are separate.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterQueue
{
    internal static class CommandChoice
    {
        private static readonly HashSet<PickupPickerController> submitting = new HashSet<PickupPickerController>();
        private static readonly MethodInfo hostChoice = typeof(PickupPickerController).GetMethod("HandlePickupSelected", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static void Clear() => submitting.Clear();
        internal static void Display(On.RoR2.PickupPickerController.orig_OnDisplayBegin original, PickupPickerController picker, NetworkUIPromptController prompt, LocalUser user, CameraRigController camera)
        {
            var plugin = BetterQueuePlugin.Instance;
            var cube = RoR2.Artifacts.CommandArtifactManager.commandCubePrefab;
            var commandPanel = cube ? cube.GetComponent<PickupPickerController>()?.panelPrefab : null;
            // Do not intercept scrappers, cauldrons, equipment or other pickers.
            if (plugin && plugin.AutoSelect.Value && Run.instance && commandPanel && picker.panelPrefab == commandPanel && user == LocalUserManager.GetFirstLocalUser())
            {
                var desired = plugin.Live.Next(ItemBrowser.Usable).FirstOrDefault(entry => FindOption(picker, entry.item) >= 0);
                if (desired != null)
                {
                    if (submitting.Add(picker))
                        plugin.StartCoroutine(Submit(plugin, picker, prompt, user, camera, original, desired));
                    return;
                }
            }
            original(picker, prompt, user, camera);
        }
        private static int FindOption(PickupPickerController picker, string item)
        {
            if (!picker || picker.options == null)
                return -1;
            for (int i = 0; i < picker.options.Length; i++)
            {
                var option = picker.options[i];
                if (option.available && !option.pickup.isTempItem && !option.pickup.isRental && ItemBrowser.PickupName(option.pickup.pickupIndex) == item)
                    return i;
            }
            return -1;
        }
        private static IEnumerator Submit(BetterQueuePlugin plugin, PickupPickerController picker, NetworkUIPromptController prompt, LocalUser user, CameraRigController camera, On.RoR2.PickupPickerController.orig_OnDisplayBegin original, PlanEntry desired)
        {
            yield return null;
            RunPlan.Reservation receipt = null;
            try
            {
                if (!picker || !plugin || !plugin.isActiveAndEnabled)
                    yield break;
                var current = plugin.Live.Next(ItemBrowser.Usable).FirstOrDefault(e => e.id == desired.id);
                int index = current != null ? FindOption(picker, current.item) : -1;
                if (index < 0)
                {
                    original(picker, prompt, user, camera);
                    yield break;
                }
                receipt = plugin.Live.Reserve(current.id);
                if (receipt == null)
                {
                    original(picker, prompt, user, camera);
                    yield break;
                }
                // Submit the raw option index. LookingGlass can remap the public
                // SubmitChoice using stale sorted-panel data when no UI opened.
                try
                {
                    if (NetworkServer.active)
                    {
                        if (hostChoice == null)
                            throw new MissingMethodException("RoR2 Command choice API changed.");
                        hostChoice.Invoke(picker, new object[] { index });
                    }
                    else
                    {
                        var controller = prompt;
                        if (!controller)
                            throw new InvalidOperationException("Command cube has no network prompt.");
                        var writer = controller.BeginMessageToServer();
                        writer.Write(PickupPickerController.msgSubmit);
                        writer.Write(index);
                        controller.FinishMessageToServer(writer);
                    }
                    plugin.Refresh();
                }
                catch (Exception error)
                {
                    plugin.Live.Cancel(receipt);
                    Debug.LogWarning("[BetterQueue] Selection failed; restoring manual picker: " + error.Message);
                    if (picker)
                        original(picker, prompt, user, camera);
                }
            }
            finally { submitting.Remove(picker); }
        }
        private static readonly Type pickupMessageType=typeof(GenericPickupController).GetNestedType("PickupMessage",BindingFlags.NonPublic);
        private static readonly FieldInfo pickupMaster=pickupMessageType?.GetField("masterGameObject"),pickupState=pickupMessageType?.GetField("pickupState"),pickupAdded=pickupMessageType?.GetField("addedQuantity");
        private static bool pickupWarning;
        internal static void Collected(On.RoR2.GenericPickupController.orig_HandlePickupMessage original,NetworkMessage netMsg)
        {
            GameObject owner=null;UniquePickup pickup=UniquePickup.none;uint added=0;
            // The native nested PickupMessage and its HookGen class are private.
            // Decode a copy via the public MessageBase contract; leave the live
            // reader exactly at its original position for the native handler.
            var reader=netMsg.reader;uint position=reader.Position;
            try
            {
                byte[] bytes;
                try{bytes=reader.ReadBytes(reader.Length-(int)position);}
                finally{reader.SeekZero();if(position>0)reader.ReadBytes((int)position);}
                var message=(MessageBase)Activator.CreateInstance(pickupMessageType,true);
                message.Deserialize(new NetworkReader(bytes));
                owner=pickupMaster.GetValue(message) as GameObject;
                pickup=(UniquePickup)pickupState.GetValue(message);added=(uint)pickupAdded.GetValue(message);
            }
            catch(Exception error){if(!pickupWarning){pickupWarning=true;Debug.LogWarning("[BetterQueue] Pickup receipt could not be decoded; native collection is unchanged: "+error.Message);}}
            original(netMsg);
            var master = LocalUserManager.GetFirstLocalUser()?.cachedMaster;
            if(master&&owner==master.gameObject&&!pickup.isTempItem&&!pickup.isRental)
            {
                string item=ItemBrowser.PickupName(pickup.pickupIndex);
                for(uint i=0;i<added;i++)if(BetterQueuePlugin.Instance?.Live.Collect(item)==true)BetterQueuePlugin.Instance.Refresh();else break;
            }
        }
    }
}
