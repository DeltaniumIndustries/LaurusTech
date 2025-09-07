using System;

namespace XRL.World.Parts
{
    /// <summary>
    /// Abstract processor with fixed-cycle, fixed-charge logic.
    /// Can optionally process all items in inventory per cycle (batch mode).
    /// </summary>
    [Serializable]
    public abstract class BasicProcessor : TimedProcessor
    {
        private int TickCounter;

        /// <summary>
        /// How many turns each processing cycle takes.
        /// </summary>
        protected abstract int CycleTicks { get; }

        /// <summary>
        /// How much charge the machine consumes per cycle.
        /// </summary>
        protected abstract int CycleCharge { get; }

        /// <summary>
        /// If true, all items in inventory are processed each cycle.
        /// If false, stops after first successful item processed.
        /// </summary>
        protected virtual bool BatchMode => false;

        public override bool FireEvent(Event E)
        {
            if (E.ID == "EndTurn" && IsReady())
            {
                TickCounter++;

                if (TickCounter >= CycleTicks)
                {
                    TickCounter = 0;

                    // Consume power first
                    if (CycleCharge <= 0 || ParentObject.UseCharge(CycleCharge))
                    {
                        if (BatchMode)
                        {
                            ForeachActivePartSubjectWhile(ProcessItem, false); // process all items
                        }
                        else
                        {
                            ForeachActivePartSubjectWhile(ProcessItem, true); // stop after first
                        }
                    }
                }
            }

            return base.FireEvent(E);
        }

        /// <summary>
        /// Subclasses implement the actual logic applied to each item.
        /// Return true if the item was modified.
        /// </summary>
        protected abstract bool ProcessItem(GameObject obj);

        // TimedProcessor still requires these overrides
        protected override bool GetJob(GameObject obj, out string output, out int ticks)
        {
            // Not used in BasicProcessor
            output = null;
            ticks = 0;
            return false;
        }

        protected override int GetChargeUse() => CycleCharge;
    }
}
