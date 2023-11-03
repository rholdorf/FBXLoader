using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Input;

namespace Game3D;

internal class InputMonitor
{
    private KeyboardState _kb;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool KeyDown(Keys k)
    {
        return _kb.IsKeyDown(k);
    }

    public void Update()
    {
        _kb = Keyboard.GetState();
    }
}