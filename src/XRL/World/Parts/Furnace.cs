using System;
using LaurusTech.Net.Laurus.Machine;
using XRL.World.Parts;


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
    }
}
