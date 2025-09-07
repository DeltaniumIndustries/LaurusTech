using System.Collections.Generic;
using XRL.World;
using XRL.World.Anatomy;

public abstract class AbstractMachineScreen : AbstractUIScreen
{
    protected GameObject Machine;
    protected List<BodyPart> BodyParts = new();
    protected int SelectedIndex;
    protected int ScrollOffset;

    protected override void Initialize(GameObject actor)
    {
        Machine = actor;
    }

}
