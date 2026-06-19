using System;
using System.Collections.Generic;

namespace PFGraph
{
    public class BindableList<T> : BindableCollection<List<T>, T>
    {
        public BindableList() : base(new List<T>()) { }

        public BindableList(List<T> list) : base(list) { }

        public BindableList(Func<List<T>> getter, Action<List<T>> setter) : base(getter, setter) { }
    }
}