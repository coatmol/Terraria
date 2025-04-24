using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Terraria.utils;

namespace Terraria.render.UI
{
    public class Input : UIComponent, Drawable
    {
        public string Placeholder;
        public string Content = "";
        public bool IsFocused = false;
        public bool IsDisabled = false;
        public Color FillColor = Color.Black;
        public Color TextColor = Color.White;
        private bool isEntered = false;

        public Input(string placeholder)
            : this(placeholder, new UDim2(), new UDim2()) { }

        public Input(string placeholder, UDim2 size)
            : this(placeholder, new UDim2(), size) { }

        public Input(string placeholder, UDim2 pos, UDim2 size)
            : base(pos, size)
        {
            Placeholder = placeholder;

            InitEvents();
        }

        private void InitEvents()
        {
            EventManager.SubcribeToEvent(EventManager.EventType.MouseButtonPressed, (e) =>
            {
                if (IsDisabled)
                {
                    IsFocused = false;
                    return;
                }
                if (e.Data is MouseButtonEventArgs)
                {
                    if (GetRect().GetGlobalBounds().Contains(Utils.GetLocalMousePos()))
                        IsFocused = true;
                    else
                        IsFocused = false;
                }
            });

            EventManager.SubcribeToEvent(EventManager.EventType.Typed, (e) =>
            {
                if (IsDisabled) return;
                if (e.Data is TextEventArgs key && IsFocused)
                {
                    if (key.Unicode == "\b")
                    {
                        if (Content.Length == 0)
                            return;
                        Content = Content[..^1];
                    }
                    else if (key.Unicode == "\r")
                    {
                        isEntered = true;
                    }
                    else
                    {
                        Content += key.Unicode;
                    }
                }
            });
        }

        public RectangleShape GetRect()
        {
            return new RectangleShape(Size) { Position = Position, FillColor = FillColor };
        }

        public Text GetText()
        {
            Text text = new Text(Content == "" ? Placeholder : Content, Constants.MainFont);
            text.FillColor = TextColor;
            text.Origin = new Vector2f(0, text.GetGlobalBounds().Size.Y / 2 + text.GetLocalBounds().Position.Y);
            text.Position = new Vector2f(0, Position.Y + GetRect().Size.Y / 2);
            return text;
        }

        public new bool Update()
        {
            if (isEntered)
            {
                isEntered = false;
                return true;
            }
            return false;
        }

        public new void Draw(RenderTarget target, RenderStates states)
        {
            base.Update();
            if (IsDisabled) return;
            target.Draw(GetRect(), states);
            target.Draw(GetText());
        }
    }
}
