using System;
using LaurusCoreLib.Net.Laurus.Enums;
using LaurusCoreLib.Net.Laurus.Logging;
using XRL;
using XRL.World;
using XRL.World.Parts;

namespace LaurusTech.Net.Laurus.Machine
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

        protected void GameMessage(string msg)
        {
            Console.WriteLine(msg);
            AddPlayerMessage(msg);
            LL.Info(msg, LogCategory.Info);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "EndTurn" && IsReady())
            {
                LL.Info("Firing End Turn Event", LogCategory.Debug);
                if (CurrentItem != null)
                {
                    LL.Info("Processing", LogCategory.Info);
                    ProcessTurn();
                }
                else
                {
                    // Look for a new job
                    LL.Info("Finding new job", LogCategory.Debug);
                    ForeachActivePartSubjectWhile(TryStartJob, true);
                }
                LL.Info("Fired End Turn Event", LogCategory.Debug);
            }
            return base.FireEvent(E);
        }

        private void ProcessTurn()
        {
            Progress++;
            if (Progress % 5 == 0)
            {
                LL.Info("Progress: " + Progress + "/" + RequiredTicks + "", LogCategory.Info);
            }
            if (Progress >= RequiredTicks)
            {
                LL.Info("Job done", LogCategory.Info);
                var old = CurrentItem;
                var newObj = old.ReplaceWith(CurrentOutput);

                if (ChargeUse > 0)
                {
                    LL.Info("Consuming " + ChargeUse + " Energy", LogCategory.Info);
                    ParentObject.UseCharge(ChargeUse);
                }
                OnJobFinished(old, newObj);
                LL.Info("Fired onJobFinished", LogCategory.Debug);

                ResetJob();
            }
        }

        private void ResetJob()
        {
            LL.Info("Resetting Job", LogCategory.Debug);
            CurrentItem = null;
            CurrentOutput = null;
            Progress = 0;
            RequiredTicks = 0;
            LL.Info("Job reset", LogCategory.Info);
        }

        /// <summary>
        /// Called each turn when no job is running.
        /// Subclasses decide whether to start a job.
        /// </summary>
        protected bool TryStartJob(GameObject obj)
        {
            LL.Info("Try start Job", LogCategory.Debug);
            if (GetJob(obj, out var output, out var ticks))
            {
                LL.Info("Starting Job", LogCategory.Info);
                CurrentItem = obj;
                CurrentOutput = output;
                RequiredTicks = ticks;
                Progress = 0;

                LL.Info("Firing onJobStarted", LogCategory.Info);
                OnJobStarted(obj, output, ticks);
                LL.Info("Fired onJobStarted", LogCategory.Info);
                return false; // reserve this item
            }
            LL.Info("Did not start Job", LogCategory.Debug);
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
