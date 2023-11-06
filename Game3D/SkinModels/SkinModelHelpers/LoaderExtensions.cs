using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Schema;
using Assimp;
using XNA = Microsoft.Xna.Framework;
// THIS IS BASED ON WORK BY:  WIL MOTIL  (a slightly older modified version)
// https://github.com/willmotil/MonoGameUtilityClasses

namespace Game3D.SkinModels.SkinModelHelpers;

// C L A S S  -  L O A D E R   E X T E N S I O N S   
public static class LoaderExtensions
{
    private const string DECIMALS_WITH_SIGN = "+0.000;-0.000"; // "0.00";
    private const string TAB = "   ";
    private const int PAD8 = 8;
    private const int PAD20 = 20;
    private const float TOLERANCE = 0.0001F;
    
    // T E S T   V A L  (check if value can be used [else return 0])
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float TestVal(float n)
    {
        if (float.IsNaN(n) || float.IsInfinity(n)) return 0.0f;
        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(float n)
    {
        return !float.IsNaN(n) && !float.IsInfinity(n);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(this XNA.Vector3 v)
    {
        return IsFinite(v.X) && IsFinite(v.Y) && IsFinite(v.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // (I don't know if this will actually inline but it's worth a shot) 
    public static XNA.Vector3 TestVec(XNA.Vector3 v)
    {
        if (float.IsNaN(v.X) || float.IsInfinity(v.X)) v.X = 0f;
        if (float.IsNaN(v.Y) || float.IsInfinity(v.Y)) v.Y = 0f;
        if (float.IsNaN(v.Z) || float.IsInfinity(v.Z)) v.Z = 0f;
        return v;
    }

    // T O   M G  (convert for use with MonoGame) - QUATERNION
    public static XNA.Quaternion ToMg(this Quaternion aq)
    {
        var m = aq.GetMatrix();
        var n = m.ToMgTransposed();
        var q = XNA.Quaternion.CreateFromRotationMatrix(n);
        return q;
    }

    // T O   M G  (convert for use with MonoGame) - MATRIX
    public static XNA.Matrix ToMg(this Matrix4x4 ma)
    {
        var m = XNA.Matrix.Identity;
        m.M11 = TestVal(ma.A1);
        m.M12 = TestVal(ma.A2);
        m.M13 = TestVal(ma.A3);
        m.M14 = TestVal(ma.A4);
        m.M21 = TestVal(ma.B1);
        m.M22 = TestVal(ma.B2);
        m.M23 = TestVal(ma.B3);
        m.M24 = TestVal(ma.B4);
        m.M31 = TestVal(ma.C1);
        m.M32 = TestVal(ma.C2);
        m.M33 = TestVal(ma.C3);
        m.M34 = TestVal(ma.C4);
        m.M41 = TestVal(ma.D1);
        m.M42 = TestVal(ma.D2);
        m.M43 = TestVal(ma.D3);
        m.M44 = TestVal(ma.D4);
        return m;
    }

    // T O   M G   T R A N S P O S E D  (convert for use with monogame and transpose it) - MATRIX TRANSPOSE (4x4)
    public static XNA.Matrix ToMgTransposed(this Matrix4x4 ma)
    {
        var m = XNA.Matrix.Identity;
        m.M11 = TestVal(ma.A1);
        m.M12 = TestVal(ma.A2);
        m.M13 = TestVal(ma.A3);
        m.M14 = TestVal(ma.A4);
        m.M21 = TestVal(ma.B1);
        m.M22 = TestVal(ma.B2);
        m.M23 = TestVal(ma.B3);
        m.M24 = TestVal(ma.B4);
        m.M31 = TestVal(ma.C1);
        m.M32 = TestVal(ma.C2);
        m.M33 = TestVal(ma.C3);
        m.M34 = TestVal(ma.C4);
        m.M41 = TestVal(ma.D1);
        m.M42 = TestVal(ma.D2);
        m.M43 = TestVal(ma.D3);
        m.M44 = TestVal(ma.D4);
        m = XNA.Matrix.Transpose(m);
        return m;
    }

    // T O   M G   T R A N S P O S E D  (convert for use with monogame and transpose it) - MATRIX TRANSPOSE (3x3)
    public static XNA.Matrix ToMgTransposed(this Matrix3x3 ma)
    {
        var m = XNA.Matrix.Identity;
        ma.Transpose();
        m.M11 = TestVal(ma.A1);
        m.M12 = TestVal(ma.A2);
        m.M13 = TestVal(ma.A3);
        m.M14 = 0;
        m.M21 = TestVal(ma.B1);
        m.M22 = TestVal(ma.B2);
        m.M23 = TestVal(ma.B3);
        m.M24 = 0;
        m.M31 = TestVal(ma.C1);
        m.M32 = TestVal(ma.C2);
        m.M33 = TestVal(ma.C3);
        m.M34 = 0;
        m.M41 = 0;
        m.M42 = 0;
        m.M43 = 0;
        m.M44 = 1;
        return m;
    }

    // T O   M G  (convert to use with MonoGame) - VECTOR3
    public static XNA.Vector3 ToMg(this Vector3D v)
    {
        return new XNA.Vector3(v.X, v.Y, v.Z);
    }

    // T O   M G  (convert to use with MonoGame) - VECTOR4
    public static XNA.Vector4 ToMg(this Color4D v)
    {
        return new XNA.Vector4(v.R, v.G, v.B, v.A);
    }

    // These are used by LoadDebugInfo to format certain types of console output
    public static string ToStringTrimmed(this Vector3D v)
    {
        return v.X.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8) + ", " + v.Y.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8) + ", " + v.Z.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8);
    }

    public static string ToStringTrimmed(this Quaternion q)
    {
        return q.X.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8) + ", " + q.Y.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8) + ", " + q.Z.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8) + "w " + q.W.ToString(DECIMALS_WITH_SIGN).PadRight(PAD8);
    }

    // SRT INFO TO STRING (for Assimp Matrix4x4)
    public static string SrtInfoToString(this Matrix4x4 m, string tabSpaces)
    {
        return QsrtInfoToString(m, tabSpaces, true);
    }

    // QSRT INFO TO STRING
    private static string QsrtInfoToString(this Matrix4x4 m, string tabSpaces, bool showQuaternions)
    {
        var deterministically = Math.Abs(m.Determinant()) < 1e-5;
        if (deterministically) 
            return string.Empty;        
        
        var str = new StringBuilder();
        m.Decompose(out var scale, out var rot, out var trans);
        QuatToEulerXyz(ref rot, out var rotAngles);
        var rotDeg = rotAngles * (float)(180d / Math.PI);

        if (showQuaternions)
            AddNewTabbedLine(str, tabSpaces, "As Quaternion", rot.ToStringTrimmed());            

        AddNewTabbedLine(str, tabSpaces, "Translation", trans.ToStringTrimmed());

        string scaleValue;
        if (MathF.Abs(scale.X - scale.Y) > TOLERANCE || MathF.Abs(scale.Y - scale.Z) > TOLERANCE || MathF.Abs(scale.Z - scale.X) > TOLERANCE)
            scaleValue = scale.ToStringTrimmed();
        else
            scaleValue = scale.X.ToString(CultureInfo.InvariantCulture);
        
        AddNewTabbedLine(str, tabSpaces, "Scale", scaleValue);
        AddNewTabbedLine(str, tabSpaces, "Rotation degrees", rotDeg.ToStringTrimmed());
        
        str.AppendLine();
        return str.ToString();
    }
    
    private static void AddNewTabbedLine(StringBuilder sb, string tabSpaces, string label, string value)
    {
        sb.AppendLine();
        sb.Append(tabSpaces);
        sb.Append(TAB);
        sb.Append(label.PadRight(PAD20));
        sb.Append(value);
    }

    // quat4 -> (roll, pitch, yaw)
    private static void QuatToEulerXyz(ref Quaternion q1, out Vector3D outVector)
    {
        // http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
        var squaredW = q1.W * q1.W;
        var squaredX = q1.X * q1.X;
        var squaredY = q1.Y * q1.Y;
        var squaredZ = q1.Z * q1.Z;
        var unit = squaredX + squaredY + squaredZ + squaredW; // if normalised is one, otherwise is correction factor
        var singularity = q1.X * q1.Y + q1.Z * q1.W;
        var pole = 0.499F * unit;
        if (singularity > pole)
        {
            // singularity at north pole
            outVector.Z = 2 * MathF.Atan2(q1.X, q1.W);
            outVector.Y = MathF.PI / 2;
            outVector.X = 0;
            return;
        }

        if (singularity < -pole)
        {
            // singularity at south pole
            outVector.Z = -2 * MathF.Atan2(q1.X, q1.W);
            outVector.Y = -MathF.PI / 2;
            outVector.X = 0;
            return;
        }

        outVector.Z = MathF.Atan2(2 * q1.Y * q1.W - 2 * q1.X * q1.Z, squaredX - squaredY - squaredZ + squaredW);
        outVector.Y = MathF.Asin(2 * singularity / unit);
        outVector.X = MathF.Atan2(2 * q1.X * q1.W - 2 * q1.Y * q1.Z, -squaredX + squaredY - squaredZ + squaredW);
    }
}