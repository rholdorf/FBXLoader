using System;

namespace MathAndCollisions;

internal class Maf
{
    public const float RADIANS_1 = (float)(3.1415926536 / 180.0);
    private const float RADIANS_90 = RADIANS_1 * 90.0f;
    private const float RADIANS_180 = RADIANS_1 * 180.0f;
    private const float RADIANS_270 = RADIANS_1 * 270.0f;
    private const float RADIANS_360 = RADIANS_1 * 360.0f;
    public const float RADIANS_QUARTER = RADIANS_1 / 4.0f;
    private const float EPSILON = 0.0001f;


    public static float Calculate2DAngleFromZero(float x, float y)
    {
        switch (x)
        {
            case 0.0f when y == 0.0f: return 0.0f;
            case > 0.0f:
            {
                x = Math.Max(x, EPSILON);
                switch (y)
                {
                    case > 0.0f:
                    {
                        y = MathF.Max(y, EPSILON); // +x,+y		
                        return MathF.Atan(y / x); // get angle (depends on quadrant so 4 conditions)
                    }
                    case > -EPSILON:
                        y = -EPSILON; // +x,-y
                        break;
                }

                return RADIANS_270 + MathF.Atan(x / -y);
            }
            case > -EPSILON:
                x = -EPSILON;
                break;
        }

        switch (y)
        {
            case > 0.0f:
            {
                y = Math.Max(y, EPSILON); // -x,+y
                return RADIANS_90 + MathF.Atan(-x / y);
            }
            case > -EPSILON:
                y = -EPSILON; // -x,-y ( = +y/+x)
                break;
        }

        return RADIANS_180 + MathF.Atan(y / x);
    }

    public static float ClampAngle(float angle)
    {
        while (angle > RADIANS_360) angle -= RADIANS_360;
        while (angle < 0) angle += RADIANS_360;
        return angle;
    }
}