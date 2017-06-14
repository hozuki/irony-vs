#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

// By: MIC

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Irony.Utilities {
    public class OrderedSet<T> : IList<T>, IReadOnlyList<T> {

        public OrderedSet() {
            _list = new List<T>();
        }

        public OrderedSet([NotNull] IEnumerable<T> collection) {
            _list = new List<T>(collection);
        }

        public IEnumerator<T> GetEnumerator() {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(T item) {
            if (_list.Contains(item)) {
                return;
            }
            _list.Add(item);
        }

        public void AddRange(params T[] items) {
            var l = _list;
            var notIn = items.Where(item => !l.Contains(item));
            l.AddRange(notIn);
        }

        public void AddRange([NotNull] IEnumerable<T> collection) {
            var l = _list;
            var notIn = collection.Where(item => !l.Contains(item));
            l.AddRange(notIn);
        }

        public void Clear() {
            _list.Clear();
        }

        public bool Contains(T item) {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item) {
            return _list.Remove(item);
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public int IndexOf(T item) {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item) {
            if (_list.Contains(item)) {
                return;
            }
            _list.Insert(index, item);
        }

        public void RemoveAt(int index) {
            _list.RemoveAt(index);
        }

        public T this[int index] {
            get => _list[index];
            set => _list[index] = value;
        }

        public T[] ToArray() {
            return _list.ToArray();
        }

        private readonly List<T> _list;

    }
}
