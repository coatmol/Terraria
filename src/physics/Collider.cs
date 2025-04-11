using SFML.Graphics;
using SFML.System;
using Terraria.utils;

namespace Terraria.physics
{
    public class Collider : RectangleShape
    {
        public Collider(Vector2f pos, Vector2f size)
        {
            Position = pos;
            Size = size;
        }

        public Collider(IntRect rect)
        {
            Position = (Vector2f)rect.Position;
            Size = (Vector2f)rect.Size;
        }

        public IntRect GetBoundingBox()
        {
            return new IntRect((Vector2i)Position, (Vector2i)Size);
        }

        public bool CheckCollisionAgainstPoint(Vector2f point)
        {
            return point.X >= Position.X && point.Y >= Position.Y && point.X < Position.X + Size.X && point.Y < Position.Y + Size.Y;
        }

        public bool CheckCollisionAgainstRect(FloatRect rect)
        {
            return rect.Position.X < Position.X + Size.X && rect.Position.X + rect.Size.X > Position.X &&
                    rect.Position.Y < Position.Y + Size.Y && rect.Position.Y + rect.Size.Y > Position.Y;
        }

        private bool CheckCollisionAgainstRect(IntRect rect)
        {
            return rect.Position.X < Position.X + Size.X && rect.Position.X + rect.Size.X > Position.X &&
                    rect.Position.Y < Position.Y + Size.Y && rect.Position.Y + rect.Size.Y > Position.Y;
        }

        public bool CheckCollisionAgainstRect(RectangleShape rect)
        {
            return rect.Position.X < Position.X + Size.X && rect.Position.X + rect.Size.X > Position.X &&
                    rect.Position.Y < Position.Y + Size.Y && rect.Position.Y + rect.Size.Y > Position.Y;
        }

        public bool CheckCollisionAgainstRay(Vector2f rayOrigin, Vector2f rayDir, RectangleShape target,
                                            ref Vector2f contactPoint, ref Vector2f contactNormal, ref float tHitNear)
        {
            Vector2f invdir = Utils.DivideFloatByVector(1, rayDir);

            Vector2f tNear = Utils.MultiplyVectors(target.Position - rayOrigin, invdir);
            Vector2f tFar = Utils.MultiplyVectors(target.Position + target.Size - rayOrigin, invdir);

            if (Utils.IsNAN(tFar.X) || Utils.IsNAN(tFar.Y) || Utils.IsNAN(tNear.X) || Utils.IsNAN(tNear.Y)) return false;

            if (tNear.X > tFar.X)
            {
                float temp = tNear.X;
                tNear.X = tFar.X;
                tFar.X = temp;
            }
            if (tNear.Y > tFar.Y)
            {
                float temp = tNear.Y;
                tNear.Y = tFar.Y;
                tFar.Y = temp;
            }

            if (tNear.X > tFar.Y || tNear.Y > tFar.X) return false;

            tHitNear = Math.Max(tNear.X, tNear.Y);
            float tHitFar = Math.Min(tFar.X, tFar.Y);

            if (tHitFar < 0) return false;

            contactPoint = rayOrigin + tHitNear * rayDir;

            if (tNear.X > tNear.Y)
            {
                if (invdir.X < 0)
                {
                    contactNormal = new Vector2f(1, 0);
                }
                else
                {
                    contactNormal = new Vector2f(-1, 0);
                }
            }
            else if (tNear.X < tNear.Y)
            {
                if (invdir.Y < 0)
                {
                    contactNormal = new Vector2f(0, 1);
                }
                else
                {
                    contactNormal = new Vector2f(0, -1);
                }
            }

            return true;
        }

        public bool CheckCollisionAgainstRay(Vector2f rayOrigin, Vector2f rayDir,
                                            ref Vector2f contactPoint, ref Vector2f contactNormal, ref float tHitNear)
        {
            return CheckCollisionAgainstRay(rayOrigin, rayDir, this, ref contactPoint, ref contactNormal, ref tHitNear);
        }
    }
}
