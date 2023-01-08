using System.Collections.Generic;

namespace Events
{
    public abstract class GameEventBase<T> : IGameEvent<T>
    {
        private readonly List<System.Action<T>> _typedListeners = new();

        public void Raise(T value)
        {
            for (var i = _typedListeners.Count - 1; i >= 0; i--)
                _typedListeners[i].Invoke(value);
        }

        public void AddListener(System.Action<T> listener)
        {
            if (!_typedListeners.Contains(listener))
                _typedListeners.Add(listener);
        }

        public void RemoveListener(System.Action<T> listener)
        {
            if (_typedListeners.Contains(listener))
                _typedListeners.Remove(listener);
        }

        public virtual void RemoveAll()
        {
            _typedListeners.RemoveRange(0, _typedListeners.Count);
        }
    }
    public abstract class GameEventBase : IGameEvent
    {
        private readonly List<System.Action> _typedListeners = new();

        public void Raise()
        {
            for (var i = _typedListeners.Count - 1; i >= 0; i--)
                _typedListeners[i].Invoke();
        }

        public void AddListener(System.Action listener)
        {
            if (!_typedListeners.Contains(listener))
                _typedListeners.Add(listener);
        }

        public void RemoveListener(System.Action listener)
        {
            if (_typedListeners.Contains(listener))
                _typedListeners.Remove(listener);
        }

        public virtual void RemoveAll()
        {
            _typedListeners.RemoveRange(0, _typedListeners.Count);
        }

        public bool HasAnyListener()
        {
            return _typedListeners.Count != 0;
        }
    }
}