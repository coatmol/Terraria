namespace Terraria
{
    public static class EventManager
    {
        public enum EventType
        {
            // Window Events
            WindowOpened,
            WindowClosed,
            WindowResized,
            WindowLostFocus,
            WindowGainedFocus,

            // Input Events
            KeyPressed,
            KeyReleased,
            MouseButtonPressed,
            MouseButtonReleased,
            MouseScrolled,
            MouseMoved,
            MouseEntered,
            MouseLeft,
        }

        public class Event
        {
            public EventType Type { get; }
            public object Data { get; }
            public Event(EventType type, object data)
            {
                Type = type;
                Data = data;
            }
        }

        private static List<(EventType eventType, Action<Event> callback)> eventListeners = new List<(EventType, Action<Event>)>();

        public static void SubcribeToEvent(EventType eventType, Action<Event> callback)
        {
            eventListeners.Add((eventType, callback));
        }

        public static void CallEvent(EventType eventType, object data)
        {
            foreach (var listener in eventListeners)
            {
                if(listener.eventType == eventType)
                {
                    listener.callback(new Event(eventType, data));
                }
            }
        }
    }
}
