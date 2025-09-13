using System;
using System.Collections.Generic;
using LaurusCoreLib.Net.Laurus.Enums;
using LaurusCoreLib.Net.Laurus.Logging;
using UnityEngine;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using GameObject = XRL.World.GameObject;

namespace LaurusTech.Net.Laurus.Machine
{


    /// <summary>
    /// Utility functions for TimedProcessor machines (inventory handling, queues, AI alerts, and sounds).
    /// </summary>
    public static class MachineUtils
    {

        /// <summary>
        /// Sends a message to the player, logs to console and LL logs.
        /// </summary>
        public static void GameMessage(string msg, LogCategory category = LogCategory.Info)
        {
            if (string.IsNullOrEmpty(msg)) return;
            Console.WriteLine(msg);
            MessageQueue.AddPlayerMessage(msg, "white", false);
            LL.Info(msg, category);
        }
        /// <summary>
        /// Attempts to pick an item from inventory using a predicate filter.
        /// </summary>
        /// <param name="actor">The actor performing the pick.</param>
        /// <param name="inventory">Inventory to pick from.</param>
        /// <param name="title">Title for the picker dialog.</param>
        /// <param name="predicate">Filter to allow only specific items.</param>
        /// <returns>True if an item was picked; false otherwise.</returns>
        public static bool PickFromInventory(GameObject actor, Inventory inventory, string title, Predicate<GameObject> predicate)
        {
            bool picked = false;
            if (inventory == null) return false;

            PickItem.ShowPicker(
                inventory.GetObjects(),
                ref picked,
                Style: PickItem.PickItemDialogStyle.GetItemDialog,
                Actor: actor,
                Container: null,
                Title: title,
                Regenerate: inventory.GetObjects,
                NotePlayerOwned: false
            );

            return picked;
        }

        /// <summary>
        /// Sends an AI help broadcast for trespass or theft detection.
        /// </summary>
        /// <param name="parent">The object being interacted with.</param>
        /// <param name="actor">The actor causing the event.</param>
        /// <param name="obj">Optional item involved.</param>
        /// <param name="magnitude">Magnitude of the alert.</param>
        /// <param name="cause">Cause of the alert.</param>
        public static void SendAIHelp(GameObject parent, GameObject actor, GameObject obj = null, float magnitude = 1f, HelpCause cause = HelpCause.Theft)
        {
            AIHelpBroadcastEvent.Send(parent, actor, obj, Magnitude: magnitude, Cause: cause);
        }

        /// <summary>
        /// Checks for newly added items and sends theft alerts if necessary.
        /// </summary>
        /// <param name="parent">The container object.</param>
        /// <param name="actor">The player interacting.</param>
        /// <param name="inventory">The inventory being checked.</param>
        /// <param name="originalObjects">Original state of objects before interaction.</param>
        public static void CheckForNewItems(GameObject parent, GameObject actor, Inventory inventory, List<GameObject> originalObjects)
        {
            GameObject highestValueItem = null;
            double totalValue = 0.0;
            double highestValue = -1.0;

            foreach (var obj in originalObjects)
            {
                if (!inventory.Objects.Contains(obj) && parent.IsOwned() && !obj.OwnedByPlayer)
                {
                    double valueEach = obj.ValueEach;
                    totalValue += valueEach * obj.Count;

                    if (valueEach > highestValue)
                    {
                        highestValue = valueEach;
                        highestValueItem = obj;
                    }
                }
            }

            if (highestValueItem != null)
            {
                float magnitude = Mathf.Max(1f, (float)(totalValue / 20.0));
                SendAIHelp(parent, actor, highestValueItem, magnitude, HelpCause.Theft);
            }
        }

        /// <summary>
        /// Ensures a container's inventory is verified and backups stored if player items exist.
        /// </summary>
        /// <param name="parent">The container object.</param>
        public static void BackupStoredItems(GameObject parent)
        {
            var inventory = parent.Inventory;
            if (inventory?.HasObjectDirect(x => x.HasIntProperty("StoredByPlayer")) ?? false)
            {
                inventory.TryStoreBackup();
            }
        }

        /// <summary>
        /// Plays a sound on the world if available.
        /// </summary>
        /// <param name="parent">The object producing the sound.</param>
        /// <param name="clip">Path to the sound clip.</param>
        public static void PlaySound(GameObject parent, string clip)
        {
            if (string.IsNullOrEmpty(clip)) return;
            parent.PlayWorldSound(clip);
        }

        /// <summary>
        /// Adds a job to the input queue if a job is already running.
        /// </summary>
        /// <param name="queue">Queue of items.</param>
        /// <param name="obj">Object to enqueue.</param>
        public static void EnqueueOrStart(List<GameObject> queue, GameObject obj)
        {
            queue ??= new List<GameObject>();
            queue.Add(obj);
        }

        /// <summary>
        /// Pops the next item from a queue or returns null if empty.
        /// </summary>
        public static GameObject Dequeue(List<GameObject> queue)
        {
            if (queue != null && queue.Count > 0)
            {
                var obj = queue[0];
                queue.RemoveAt(0);
                return obj;
            }
            return null;
        }

        public static void AttemptOpen(TimedProcessor obj, GameObject actor, IEvent parentEvent = null)
        {
            var parentObj = GetParentObject(obj);
            parentObj?.Inventory?.VerifyContents();
            if (!parentObj.IsValid() || !parentObj.FireEvent("BeforeOpen")) return;

            parentObj.FireEvent("Opening");

            if (parentObj.IsCreature)
            {
                HandleCreatureOpen(obj, actor);
            }
            else
            {
                HandleContainerOpen(obj, actor, parentEvent);
            }
        }


        /// <summary>Handle opening of creatures (trading).</summary>
        private static void HandleCreatureOpen(TimedProcessor obj, GameObject actor)
        {
            if (!actor.IsPlayer())
                return;

            if (!actor.PhaseMatches(GetParentObject(obj)) || GetParentObject(obj).HasPropertyOrTag("NoTrade") || GetParentObject(obj).HasPropertyOrTag("FugueCopy") || actor.DistanceTo(GetParentObject(obj)) > 1)
            {
                Popup.ShowFail($"You cannot trade with {GetParentObject(obj).t()}.");
                return;
            }

            PlayOpenSound(obj);

            if (GetParentObject(obj).IsPlayerLed())
                TradeUI.ShowTradeScreen(GetParentObject(obj), 0.0f);
            else if (GetParentObject(obj).IsPlayer())
            {
                Screens.CurrentScreen = 2;
                Screens.Show(The.Player);
            }
            else
                TradeUI.ShowTradeScreen(GetParentObject(obj));

        }

        /// <summary>Handle opening of containers (inventory items).</summary>
        private static void HandleContainerOpen(TimedProcessor obj, GameObject actor, IEvent parentEvent)
        {
            if (NeedsTrespassWarning(obj, actor) && Popup.ShowYesNoCancel($"That is not owned by you. Are you sure you want to open it?") != DialogResult.Yes)
                return;

            SendTrespassEvent(obj, actor);

            if (!actor.IsPlayer())
                return;

            var inventory = GetParentObject(obj).Inventory;
            if (inventory == null || inventory.GetObjectCount() == 0)
            {
                HandleEmptyContainer(obj, actor, inventory);
            }
            else
            {
                HandleNonEmptyContainer(obj, actor, parentEvent, inventory);
            }

            TryBackupStoredItems(obj);
        }


        /// <summary>Check if a warning about trespassing is needed.</summary>
        private static bool NeedsTrespassWarning(TimedProcessor obj, GameObject actor)
        {
            var parentObj = GetParentObject(obj);
            return !parentObj.HasTagOrProperty("DontWarnOnOpen") &&
                   actor.IsPlayer() &&
                   !string.IsNullOrEmpty(parentObj.Owner) &&
                    parentObj.Equipped != IComponent<XRL.World.GameObject>.ThePlayer &&
                    parentObj.InInventory != IComponent<XRL.World.GameObject>.ThePlayer;
        }

        /// <summary>Send AI help broadcast for trespassing.</summary>
        private static void SendTrespassEvent(TimedProcessor obj, GameObject actor)
        {
            AIHelpBroadcastEvent.Send(GetParentObject(obj), actor, Cause: HelpCause.Trespass);
        }

        /// <summary>Handle opening an empty container.</summary>
        private static void HandleEmptyContainer(TimedProcessor obj, GameObject actor, Inventory inventory)
        {
            Popup.Show($"There's nothing {obj.Preposition} that. Best way for processing to finish.");
        }

        /// <summary>Handle opening a container with items inside.</summary>
        private static void HandleNonEmptyContainer(TimedProcessor obj, GameObject actor, IEvent parentEvent, Inventory inventory)
        {
            var originalObjects = new List<GameObject>(inventory.GetObjectsDirect());
            bool itemPicked = ShowPickerDialog(obj, actor, inventory);

            if (itemPicked && parentEvent != null)
                parentEvent.RequestInterfaceExit();

            CheckForNewItems(obj, actor, inventory, originalObjects);
        }

        /// <summary>
        /// Displays the PickItem dialog and returns true if the player picked an item.
        /// </summary>
        private static bool ShowPickerDialog(TimedProcessor obj, GameObject actor, Inventory inventory)
        {
            bool itemPicked = false;

            string title = $"{{{{W|{"Examining"} {GetParentObject(obj).an(Stripped: true)}}}}}";

            PlayOpenSound(obj);

            PickItem.ShowPicker(
                inventory.GetObjects(),
                ref itemPicked,
                Style: PickItem.PickItemDialogStyle.GetItemDialog,
                Actor: actor,
                Container: GetParentObject(obj),
                Title: title,
                Regenerate: inventory.GetObjects,
                NotePlayerOwned: false
            );

            return itemPicked;
        }


        /// <summary>Check for newly added items and send AI help for theft detection.</summary>
        private static void CheckForNewItems(TimedProcessor objectA, GameObject actor, Inventory inventory, List<GameObject> originalObjects)
        {
            GameObject highestValueItem = null;
            double totalValue = 0.0;
            double highestValue = -1.0;

            for (int i = 0; i < originalObjects.Count; i++)
            {
                var obj = originalObjects[i];
                if (!inventory.Objects.Contains(obj) && GetParentObject(objectA).IsOwned() && !obj.OwnedByPlayer)
                {
                    double valueEach = obj.ValueEach;
                    totalValue += valueEach * obj.Count;

                    if (valueEach > highestValue)
                    {
                        highestValue = valueEach;
                        highestValueItem = obj;
                    }
                }
            }

            if (highestValueItem != null)
            {
                float magnitude = Mathf.Max(1f, (float)(totalValue / 20.0));
                AIHelpBroadcastEvent.Send(GetParentObject(objectA), actor, highestValueItem, Magnitude: magnitude, Cause: HelpCause.Theft);
            }
        }

        /// <summary>Attempt to store a backup of player-stored items.</summary>
        private static void TryBackupStoredItems(TimedProcessor obj)
        {
            var inventory = GetParentObject(obj).Inventory;
            bool hasStoredItems = inventory?.HasObjectDirect(x => x.HasIntProperty("StoredByPlayer")) ?? false;

            if (hasStoredItems)
                inventory.TryStoreBackup();
        }

        public static GameObject GetParentObject(TimedProcessor obj)
        {
            return obj.ParentObject;
        }

        public static void PlayOpenSound(TimedProcessor obj)
        {
            string Clip = obj.OpenSound ??= obj.ParentObject.GetTagOrStringProperty("OpenSound");
            if (Clip.IsNullOrEmpty())
                return;
            obj.PlayWorldSound(Clip);
        }
    }

}
