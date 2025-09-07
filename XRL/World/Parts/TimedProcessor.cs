using System;

namespace XRL.World.Parts
{
    [Serializable]
    public abstract class TimedProcessor : IPoweredPart
    {
        /// <summary>
        /// How many turns the current job has taken.
        /// </summary>
        private int Progress;

        /// <summary>
        /// Total turns required for the current job.
        /// </summary>
        private int RequiredTicks;

        /// <summary>
        /// Item being processed.
        /// </summary>
        protected GameObject CurrentItem;

        /// <summary>
        /// Blueprint or recipe output target.
        /// </summary>
        protected string CurrentOutput;

        protected TimedProcessor()
        {
            ChargeUse = GetChargeUse();
            WorksOnInventory = true;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("EndTurn");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "EndTurn" && IsReady())
            {
                if (CurrentItem != null)
                {
                    ProcessTurn();
                }
                else
                {
                    // Look for a new job
                    ForeachActivePartSubjectWhile(TryStartJob, true);
                }
            }
            return base.FireEvent(E);
        }

        private void ProcessTurn()
        {
            Progress++;
            if (Progress >= RequiredTicks)
            {
                var old = CurrentItem;
                var newObj = old.ReplaceWith(CurrentOutput);

                if (ChargeUse > 0)
                    ParentObject.UseCharge(ChargeUse);

                OnJobFinished(old, newObj);

                ResetJob();
            }
        }

        private void ResetJob()
        {
            CurrentItem = null;
            CurrentOutput = null;
            Progress = 0;
            RequiredTicks = 0;
        }

        /// <summary>
        /// Called each turn when no job is running.
        /// Subclasses decide whether to start a job.
        /// </summary>
        protected bool TryStartJob(GameObject obj)
        {
            if (GetJob(obj, out var output, out var ticks))
            {
                CurrentItem = obj;
                CurrentOutput = output;
                RequiredTicks = ticks;
                Progress = 0;

                OnJobStarted(obj, output, ticks);
                return false; // reserve this item
            }
            return true; // keep looking
        }

        /// <summary>
        /// Subclasses provide job definition:
        /// if the object can be processed, return true with output + ticks.
        /// </summary>
        protected abstract bool GetJob(GameObject obj, out string output, out int ticks);

        /// <summary>
        /// Subclasses can react when a job starts.
        /// </summary>
        protected virtual void OnJobStarted(GameObject input, string output, int ticks) { }

        /// <summary>
        /// Subclasses can react when a job finishes.
        /// </summary>
        protected virtual void OnJobFinished(GameObject input, GameObject output) { }

        /// <summary>
        /// How much charge per finished job.
        /// </summary>
        protected abstract int GetChargeUse();
    }
}
