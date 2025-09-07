using System;
using LaurusTech.Net.Laurus.Machine;

namespace XRL.World.Parts
{
    /// <summary>
    /// Processor that applies a "charging" effect to all items in inventory.
    /// Inherits fixed-cycle, fixed-cost logic from BasicProcessor.
    /// </summary>
    [Serializable]
    public abstract class ItemCharger : BasicProcessor
    {
        protected override bool BatchMode => true;

        protected override bool ProcessItem(GameObject obj)
        {
            return ApplyCharge(obj);
        }

        /// <summary>
        /// Subclasses define how an individual item is charged.
        /// Return true if the item was modified.
        /// </summary>
        protected abstract bool ApplyCharge(GameObject obj);
    }
}
