using System;
using ConsoleLib.Console;
using XRL.UI;
using XRL.World;
using XRL.World.Anatomy;

public class EquipmentScreen : AbstractMachineScreen
{


    protected override void HandleInput()
    {
        CurrentKey = Keyboard.getvk(Options.MapDirectionsToKeypad);

        switch (CurrentKey)
        {
            case Keys.Escape:
                Done = true;
                break;
            case Keys.NumPad8:
                SelectedIndex = Math.Max(0, SelectedIndex - 1);
                break;
            case Keys.NumPad2:
                SelectedIndex = Math.Min(BodyParts.Count - 1, SelectedIndex + 1);
                break;
            case Keys.Space:
                var selectedPart = BodyParts[SelectedIndex];
                break;
        }
    }

    protected override void Render()
    {
        var buffer = ScreenBuffer.GetScrapBuffer1();
        buffer.Clear();

        // Draw header
        buffer.Goto(35, 0);
        buffer.Write($"[ {{W|Furnace}} ]");

        // Draw body parts + items
        for (int i = 0; i < BodyParts.Count; i++)
        {
            var part = BodyParts[i];
            buffer.Goto(2, 2 + i);
            if (i == SelectedIndex) buffer.Write(">");
            buffer.Write(part.GetCardinalDescription());
        }

        Popup._TextConsole.DrawBuffer(buffer);
    }

    protected override ScreenReturn FinalizeScreen()
    {
        return ScreenReturn.Exit;
    }

}
