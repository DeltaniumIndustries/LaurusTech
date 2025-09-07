using System;
using System.Collections.Generic;
using XRL.World;
using XRL.UI;
using XRL.World.Anatomy;

namespace LaurusTech.net.laurus
{
    public static class ItemPickerUtils
    {
        /// <summary>
        /// Pick a GameObject from a list with a prompt.
        /// </summary>
        public static GameObject PickItem(
            string prompt,
            IList<GameObject> candidates,
            bool allowEscape = true,
            bool preserveOrder = false)
        {
            if (candidates == null || candidates.Count == 0)
            {
                Popup.ShowFail("No valid items found.");
                return null;
            }

            return Popup.PickGameObject(prompt, candidates, allowEscape, preserveOrder);
        }

        /// <summary>
        /// Filter inventory/equipment by predicate.
        /// </summary>
        public static List<GameObject> FilterInventory(GameObject actor, Func<GameObject, bool> filter)
        {
            var results = new List<GameObject>();
            if (actor?.Inventory == null)
                return results;

            foreach (var item in actor.GetInventoryAndEquipment())
            {
                if (filter(item))
                    results.Add(item);
            }
            return results;
        }

        public static void SendHelpBroadCast(GameObject ParentObject, GameObject Actor)
        {
            AIHelpBroadcastEvent.Send(ParentObject, Actor, Cause: HelpCause.Trespass);
        }

        /// <summary>
        /// Pick directly from filtered inventory.
        /// </summary>
        public static GameObject PickFromInventory(
            GameObject actor,
            string prompt,
            Func<GameObject, bool> filter,
            bool allowEscape = true,
            bool preserveOrder = false)
        {
            var candidates = FilterInventory(actor, filter);
            return PickItem(prompt, candidates, allowEscape, preserveOrder);
        }

        /// <summary>
        /// Pick and apply an action on the chosen item.
        /// Returns true if action applied.
        /// </summary>
        public static bool PickAndApply(
            GameObject actor,
            string prompt,
            Func<GameObject, bool> filter,
            Action<GameObject> action,
            bool allowEscape = true)
        {
            var item = PickFromInventory(actor, prompt, filter, allowEscape);
            if (item == null)
                return false;

            action(item);
            return true;
        }

        /// <summary>
        /// Pick and fire an event on the chosen item.
        /// </summary>
        public static bool PickAndFireEvent(
            GameObject actor,
            string prompt,
            Func<GameObject, bool> filter,
            string eventName,
            params object[] args)
        {
            return PickAndApply(actor, prompt, filter, item =>
            {
                var evt = Event.New(eventName);
                for (int i = 0; i < args.Length; i += 2)
                {
                    evt.AddParameter((string)args[i], args[i + 1]);
                }
                actor.FireEvent(evt);
            });
        }

        /// <summary>
        /// Pick and add a part to the chosen item.
        /// </summary>
        public static bool PickAndAddPart<T>(GameObject actor, string prompt, Func<GameObject, bool> filter = null)
            where T : IPart, new()
        {
            return PickAndApply(actor, prompt, filter ?? (_ => true), item =>
            {
                item.AddPart(new T());
            });
        }

        /// <summary>
        /// Pick and remove a part from the chosen item.
        /// </summary>
        public static bool PickAndRemovePart<T>(GameObject actor, string prompt, Func<GameObject, bool> filter = null)
            where T : IPart
        {
            return PickAndApply(actor, prompt, filter ?? (_ => true), item =>
            {
                var part = item.GetPart<T>();
                if (part != null)
                    item.RemovePart(part);
            });
        }

        /// <summary>
        /// Pick and consume an item (remove from inventory).
        /// </summary>
        public static bool PickAndConsume(GameObject actor, string prompt, Func<GameObject, bool> filter)
        {
            return PickAndApply(actor, prompt, filter, item =>
            {
                actor.Inventory.RemoveObject(item);
                Popup.Show($"You used {item.DisplayName}.");
            });
        }

        /// <summary>
        /// Pick and equip an item to a body part.
        /// </summary>
        public static bool PickAndEquip(GameObject actor, string prompt, Func<GameObject, bool> filter, BodyPart bodyPart)
        {
            return PickAndApply(actor, prompt, filter, item =>
            {
                actor.FireEvent(Event.New("CommandEquipObject", "Object", item, "BodyPart", bodyPart));
            });
        }

        /// <summary>
        /// Pick and unequip an item from a body part.
        /// </summary>
        public static bool PickAndUnequip(GameObject actor, BodyPart bodyPart)
        {
            if (bodyPart?.Equipped == null)
            {
                Popup.ShowFail("Nothing to unequip.");
                return false;
            }

            actor.FireEvent(Event.New("CommandUnequipObject", "BodyPart", bodyPart));
            return true;
        }

        /// <summary>
        /// Convenience: pick any item and just return DisplayName.
        /// </summary>
        public static string PickItemName(GameObject actor, string prompt, Func<GameObject, bool> filter = null)
        {
            var item = PickFromInventory(actor, prompt, filter ?? (_ => true));
            return item?.DisplayName;
        }

        /// <summary>
        /// Pick multiple items (basic multi-select loop).
        /// </summary>
        public static List<GameObject> PickMultiple(GameObject actor, string prompt, Func<GameObject, bool> filter, int maxCount = int.MaxValue)
        {
            var results = new List<GameObject>();
            var candidates = FilterInventory(actor, filter);

            while (results.Count < maxCount && candidates.Count > 0)
            {
                var choice = PickItem($"{prompt} ({results.Count}/{maxCount})", candidates);
                if (choice == null)
                    break;

                results.Add(choice);
                candidates.Remove(choice);
            }

            return results;
        }
    }
}
