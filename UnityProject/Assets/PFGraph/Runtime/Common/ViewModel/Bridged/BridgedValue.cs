using System;

namespace PFGraph
{
    public interface IBridgedValue<T>
    {
        T Value { get; set; }
    }

    [Serializable]
    public class BridgedValue<T> : IBridgedValue<T>
    {
        private T value;

        public T Value
        {
            get => value;
            set => this.value = value;
        }

        public BridgedValue(T value)
        {
            this.value = value;
        }
    }

    [Serializable]
    public class BridgedValueGetterSetter<T> : IBridgedValue<T>
    {
        private Func<T> valueGetter;
        private Action<T> valueSetter;

        public T Value
        {
            get => valueGetter();
            set => valueSetter(value);
        }

        public BridgedValueGetterSetter(Func<T> valueGetter, Action<T> valueSetter)
        {
            this.valueGetter = valueGetter;
            this.valueSetter = valueSetter;
        }
    }
}