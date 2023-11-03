using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game3D;

internal class Input
{
    public const float DEAD_ZONE = 0.12f; //"dead zone" for analog on peripheral devices

    public const ButtonState ButtonUp = ButtonState.Released;
    public const ButtonState ButtonDown = ButtonState.Pressed;

    // KEYBOARD STUFF
    public KeyboardState kb, okb;
    public bool shift_down, control_down, alt_down, shift_press, control_press, alt_press;
    public bool old_shift_down, old_control_down, old_alt_down;

    // MOUSE STUFF
    public MouseState ms, oms;
    public bool leftClick, midClick, rightClick, leftDown, midDown, rightDown;
    public int mosx, mosy;
    public Vector2 mosV;
    public Point mp;

    // GAMEPAD STUFF
    public GamePadState gp, ogp;
    public bool A_down, B_down, X_down, Y_down, RB_down, LB_down, start_down, back_down, leftStick_down, rightStick_down;
    public bool A_press, B_press, X_press, Y_press, RB_press, LB_press, start_press, back_press, leftStick_press, rightStick_press;

    float screenScaleX, screenScaleY; // used to scale desktop resolution mouse coordinates to match position in MainTarget (resolution of game)

    public Input(PresentationParameters pp, RenderTarget2D target)
    {
        screenScaleX = 1.0f / (pp.BackBufferWidth / (float)target.Width);
        screenScaleY = 1.0f / (pp.BackBufferHeight / (float)target.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool KeyPress(Keys k)
    {
        return kb.IsKeyDown(k) && okb.IsKeyUp(k);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool KeyDown(Keys k)
    {
        return kb.IsKeyDown(k);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ButtonPress(Buttons button)
    {
        return gp.IsButtonDown(button) && ogp.IsButtonUp(button);
    }

    //-------------
    // U P D A T E 
    //-------------
    public void Update()
    {
        old_alt_down = alt_down;
        old_shift_down = shift_down;
        old_control_down = control_down;
        okb = kb;
        oms = ms;
        ogp = gp;
        kb = Keyboard.GetState();
        ms = Mouse.GetState();
        gp = GamePad.GetState(0);

        // KEYBOARD STUFF
        shift_down = shift_press = control_down = control_press = alt_down = alt_press = false;
        shift_down = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
        control_down = kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);
        alt_down = kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt);
        shift_press = shift_down && !old_shift_down;
        control_press = control_down && !old_control_down;
        alt_press = alt_down && !old_alt_down;

        // MOUSE STUFF
        mosV = new Vector2(ms.Position.X * screenScaleX, ms.Position.Y * screenScaleY);
        mosx = (int)mosV.X;
        mosy = (int)mosV.Y;
        mp = new Point(mosx, mosy);
        leftClick = midClick = rightClick = leftDown = rightDown = midDown = false;
        leftDown = ms.LeftButton == ButtonDown;
        midDown = ms.MiddleButton == ButtonDown;
        rightDown = ms.RightButton == ButtonDown;
        leftClick = leftDown && oms.LeftButton == ButtonUp;
        midClick = midDown && oms.MiddleButton == ButtonUp;
        rightClick = rightDown && oms.RightButton == ButtonUp;
    }
}