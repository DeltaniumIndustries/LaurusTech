using System;
using System.Collections.Generic;
using System.Linq;
using LaurusTech.net.laurus;
using LaurusTech.Net.Laurus.Machine;
using XRL.UI;


namespace XRL.World.Parts
{
    [Serializable]

    public class Furnace : RecipeProcessor
    {
        public Furnace() { }

        protected override string GetRecipeFile()
        {
            return "furnace";
        }

        protected override int GetChargeUse()
        {
            return 1;
        }

        protected override void OnJobStarted(GameObject input, string output, int ticks)
        {
            GameMessage($"Furnace starts smelting {input.Blueprint} -> {output} ({ticks} turns)");
        }

        protected override void OnJobFinished(GameObject input, GameObject output)
        {
            GameMessage($"Furnace finished smelting {input.Blueprint} -> {output.Blueprint}");
        }

        protected override IEnumerable<MenuActionDef> GetMenuActions()
        {
            return new List<MenuActionDef>
                {
                    new(
                        display: "Load Furnace",
                        verb: "load inputs",
                        command: "LoadFurnace",
                        key: "1",
                        @default: '1',
                        worksTelekinetically: false,
                        handler: HandleLoadFurnaceAction
                    )
                };
        }

        private bool HandleLoadFurnaceAction(InventoryActionEvent e)
        {
            GameMessage("Loading Furnace");
            var pickedInput = ItemPickerUtils.PickFromInventory(
                e.Actor,
                "Select Input",
                go => MachineRecipeMap.Keys.Contains(go.Blueprint)
            );
            if (pickedInput != null)
            {
                int? depositAmount = Popup.AskNumber("How many? (1-"+pickedInput.Count+")", Start: 1, Min: 1, Max: pickedInput.Count);
                if (depositAmount != null)
                {
                    if (pickedInput.Count - depositAmount > 1)
                {
                    pickedInput.Count -= (int) depositAmount;
                }
                else
                {
                    e.Actor.Inventory.RemoveObject(pickedInput);
                }
                    return TryStartJob(pickedInput);
                }
                
            }
            return false;
        }

        protected override bool GeneratesHeat()
        {
            return true;
        }
    }
}
