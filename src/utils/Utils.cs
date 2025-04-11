using SFML.System;

namespace Terraria.utils
{
    class Utils
    {
        public static GameWindow mainWindow;

        public static Vector2f DivideVectors(Vector2f a, Vector2f b)
        {
            return new Vector2f(
                b.X != 0 ? a.X / b.X : 0,
                b.Y != 0 ? a.Y / b.Y : 0
            );
        }

        public static Vector2f MultiplyVectors(Vector2f a, Vector2f b)
        {
            return new Vector2f(a.X * b.X, a.Y * b.Y);
        }


        public static Vector2f DivideFloatByVector(float a, Vector2f b)
        {
            return new Vector2f(
                b.X != 0 ? a / b.X : 0,
                b.Y != 0 ? a / b.Y : 0
            );
        }

        public static bool IsNAN(float a)
        {
            return a != a;
        }

        public static Vector2f NormalizeVector(Vector2f vec)
        {
            float length = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
            if (length == 0)
                return new Vector2f(0, 0);
            return new Vector2f(vec.X / length, vec.Y / length);
        }

        public static Vector2f AddToVector(Vector2f vec, float n)
        {
            return new Vector2f(vec.X + n, vec.Y + n);
        }

        public static Vector2f LerpVector(Vector2f a, Vector2f b, float t)
        {
            return a * (1 - t) + b * t;
        }

        public static float LerpF(float a, float b, float t)
        {
            return a * (1 - t) + b * t;
        }
    }
}
