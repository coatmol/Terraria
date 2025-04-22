using ProtoBuf;
using SFML.Graphics;

[ProtoContract]
public struct IntRectSurrogate
{
    [ProtoMember(1)] public int Left;
    [ProtoMember(2)] public int Top;
    [ProtoMember(3)] public int Width;
    [ProtoMember(4)] public int Height;

    public static implicit operator IntRectSurrogate(IntRect r)
      => new IntRectSurrogate { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };

    public static implicit operator IntRect(IntRectSurrogate s)
      => new IntRect(s.Left, s.Top, s.Width, s.Height);
}
