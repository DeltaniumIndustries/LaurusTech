using ConsoleLib.Console;
using XRL.UI;
using XRL.World;

public abstract class AbstractUIScreen : IScreen
{
    protected bool Done { get; set; }
    protected Keys CurrentKey { get; set; }

    public ScreenReturn Show(GameObject GO)
    {
        Initialize(GO);

        while (!Done)
        {
            HandleInput();
            Render();
        }

        return FinalizeScreen();
    }

    protected abstract void Initialize(GameObject GO);
    protected abstract void HandleInput();
    protected abstract void Render();
    protected abstract ScreenReturn FinalizeScreen();
}
