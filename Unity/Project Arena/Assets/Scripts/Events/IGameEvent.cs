namespace Events
{
    public interface IGameEvent<T>
    {
        void Raise(T value);
        public void AddListener(System.Action<T> action);
        public void RemoveListener(System.Action<T> action);
        void RemoveAll();
    }
    public interface IGameEvent
    {
        void Raise();
        public void AddListener(System.Action action);
        public void RemoveListener(System.Action action);
        void RemoveAll();
    }
}