using System;
using System.Collections.Generic;
using LaurusCoreLib.Net.Laurus.Enums;
using LaurusCoreLib.Net.Laurus.Logging;
using UnityEngine;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using GameObject = XRL.World.GameObject;

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
        /// Queue of items to be processed by the machine.
        /// Defaults to an empty list.
        /// </summary>
        public List<GameObject> InputQueue { get; set; } = new List<GameObject>();


        /// <summary>
        /// Item being processed.
        /// </summary>
        protected GameObject CurrentItem;

        /// <summary>
        /// Blueprint or recipe output target.
        /// </summary>
        protected string CurrentOutput;

        public string Preposition = "inside";
        public string OpenSound = "Sounds/Interact/sfx_interact_open_genericContainer";

        /// <summary>
        /// Registry of menu actions (command → handler).
        /// Populated from GetMenuActions().
        /// </summary>
        private readonly Dictionary<string, Func<InventoryActionEvent, bool>> MenuActions =
            new(StringComparer.OrdinalIgnoreCase);

        protected TimedProcessor()
        {
            ChargeUse = GetChargeUse();
            WorksOnInventory = true;
        }

        /// <summary>
        /// Sends a message to the player, logs to console and LL logs.
        /// </summary>
        public static void GameMessage(string msg, LogCategory category = LogCategory.Info)
        {
            MachineUtils.GameMessage(msg, category);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == EndTurnEvent.ID || ID == CanSmartUseEvent.ID || ID == CommandSmartUseEvent.ID || ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID;
        }
        public override bool HandleEvent(RadiatesHeatEvent E)
        {
            return GeneratesHeat() && base.HandleEvent(E);
        }

        public override bool HandleEvent(CanSmartUseEvent E)
        {
            //return !E.Actor.IsPlayer() && base.HandleEvent(E);
            return false;
        }
        public override bool HandleEvent(CommandSmartUseEvent E)
        {
            if (RequiredTicks > 0)
            {
                // We are working, show menu
            }
            else
            {
                if (ParentObject.Inventory != null && this.ParentObject.Inventory.Count(CurrentOutput) > 0)
                {
                    // Show Inventory, we have outputs
                    MachineUtils.AttemptOpen(this, E.Actor, E);
                }

                if (InputQueue.Count > 0)
                {
                    // We are processing multiple things
                }
                else
                {

                }
            }
            return base.HandleEvent(E);
        }

        /// <summary>
        /// Handles the "EndTurn" event: processes current job or looks for a new one.
        /// </summary>
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (!IsReady()) return base.HandleEvent(E);

            LL.Info("Firing End Turn Event", LogCategory.Info);

            if (CurrentItem != null)
            {
                LL.Info("Processing", LogCategory.Info);
                ProcessTurn();
            }
            else
            {
                LL.Info("Finding new job", LogCategory.Info);
                ForeachActivePartSubjectWhile(TryStartJob, true);
            }

            LL.Info("Fired End Turn Event", LogCategory.Info);
            return base.HandleEvent(E);
        }

        /// <summary>
        /// Handles the "GetInventoryActions" event: registers menu actions with the UI.
        /// </summary>

        public override bool HandleEvent(GetInventoryActionsEvent E)
        {
            E.AddAction("Check Output", "check output", "Check Output", Key: '2');
            foreach (var action in GetMenuActions())
            {
                LL.Info($"Registering action: {action.Display}", LogCategory.Debug);
                E.AddAction(
                    action.Display,
                    action.Verb,
                    action.Command,
                    action.Key,
                    action.Default,
                    WorksTelekinetically: action.WorksTelekinetically
                );

                if (action.Handler != null)
                    MenuActions[action.Command] = action.Handler;
            }

            return base.HandleEvent(E);
        }

        /// <summary>
        /// Handles the "InventoryAction" event: executes the corresponding menu action handler.
        /// </summary>

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E == null || E.Command == null)
            {
                return base.HandleEvent(E);
            }
            if (E.Command == "Check Output")
            {
                MachineUtils.AttemptOpen(this, E.Actor, E);
            }
            if (!MenuActions.TryGetValue(E.Command, out var handler))
            {
                return base.HandleEvent(E);
            }

            LL.Info($"Executing menu action: {E.Command}", LogCategory.Info);

            // Wrap Event as InventoryActionEvent if needed by the handler
            return handler?.Invoke(E) ?? false;
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
            CurrentOutput = null;
            Progress = 0;
            RequiredTicks = 0;
            CurrentItem = null;

            // If there’s anything in the input queue, start the next job
            if (InputQueue != null && InputQueue.Count > 0)
            {
                CurrentItem = InputQueue[0];
                InputQueue.RemoveAt(0);

                if (!TryStartJob(CurrentItem))
                {
                    LL.Info($"Started next job: {CurrentItem.DisplayNameOnlyDirect}", LogCategory.Info);
                }
                else
                {
                    LL.Info($"Failed to start queued job for: {CurrentItem.DisplayNameOnlyDirect}", LogCategory.Warning);
                }
            }
            LL.Info("Job reset", LogCategory.Info);
        }

        /// <summary>
        /// Called each turn when a job can be started or queued.
        /// If a job is already running, the object is added to the input queue.
        /// </summary>
        protected bool TryStartJob(GameObject obj)
        {
            LL.Info("Attempting to start or queue job", LogCategory.Debug);

            if (CurrentItem != null)
            {
                // Job is already in progress — enqueue the object for later processing
                if (InputQueue == null)
                    InputQueue = new List<GameObject>();

                InputQueue.Add(obj);
                LL.Info($"Job in progress. Queued {obj.DisplayNameOnlyDirect} for later. Queue Size: {InputQueue.Count}", LogCategory.Info);
                return true; // still searching/queuing
            }

            // No job running — attempt to start immediately
            if (GetJob(obj, out var output, out var ticks))
            {
                LL.Info($"Starting job for {obj.DisplayNameOnlyDirect}", LogCategory.Info);
                CurrentItem = obj;
                CurrentOutput = output;
                RequiredTicks = ticks;
                Progress = 0;

                LL.Info("Firing onJobStarted", LogCategory.Info);
                OnJobStarted(obj, output, ticks);
                LL.Info("Fired onJobStarted", LogCategory.Info);
                return false; // item reserved, stop searching
            }

            LL.Info($"Did not start job for {obj.DisplayNameOnlyDirect}", LogCategory.Info);
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

        /// <summary>
        /// Provide the list of custom inventory actions this machine exposes.
        /// </summary>
        protected abstract IEnumerable<MenuActionDef> GetMenuActions();



        protected abstract bool GeneratesHeat();
    }
}
