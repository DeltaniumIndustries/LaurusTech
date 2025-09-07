using System;
using XRL.World;
using XRL.World.Parts;

namespace LaurusTech.Net.Laurus.Machine
{

    [Serializable]
    public class BatteryCharger : ItemCharger
    {
        protected override int CycleTicks => 5;
        protected override int CycleCharge => 2;

        protected override bool ApplyCharge(GameObject obj)
        {
            var battery = obj.GetPart<EnergyCell>();
            if (battery != null && battery.Charge < battery.MaxCharge)
            {
                battery.Charge++;
                if (Visible())
                {
                    GameMessage(
                        $"{ParentObject.The} charges {obj.The} ({battery.Charge}/{battery.MaxCharge})"
                    );
                }
                return true;
            }

            return false;
        }

    }
}