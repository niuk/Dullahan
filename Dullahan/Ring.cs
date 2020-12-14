using System;
using System.Collections;
using System.Collections.Generic;

namespace Dullahan {
    public class Ring<T> : IList<T> {
        public int bufferStart = 0;
        public T[] buffer = new T[1];

        public int Start { get; private set; } = 0;

        public int Count { get; private set; } = 0;

        public int End => Start + Count;

        bool ICollection<T>.IsReadOnly => false;

        public void Clear() {
            bufferStart = 0;
            Start = 0;
            Count = 0;
        }

        public T PeekStart() {
            if (Count > 0) {
                return buffer[bufferStart];
            } else {
                throw new InvalidOperationException();
            }
        }

        public T PeekEnd() {
            if (Count > 0) {
                return buffer[(bufferStart + Count - 1) % buffer.Length];
            } else {
                throw new InvalidOperationException();
            }
        }

        public T PopStart() {
            if (Count > 0) {
                T item = PeekStart();
                bufferStart = (bufferStart + 1) % buffer.Length;
                ++Start;
                --Count;
                return item;
            } else {
                throw new InvalidOperationException();
            }
        }

        public T PopEnd() {
            if (Count > 0) {
                T item = PeekEnd();
                --Count;
                return item;
            } else {
                throw new InvalidOperationException();
            }
        }

        public void PushStart(T item) {
            if (Start == 0) {
                throw new IndexOutOfRangeException();
            }

            EnsureSize();
            bufferStart = bufferStart > 0 ? bufferStart - 1 : buffer.Length - 1;
            buffer[bufferStart] = item;
            --Start;
            ++Count;
        }

        public void PushEnd(T item) {
            EnsureSize();
            buffer[(bufferStart + Count) % buffer.Length] = item;
            ++Count;
        }

        public void Insert(int index, T item) {
            if (Start <= index && index < End) {
                EnsureSize();
                int sourceIndex = (bufferStart + (index - Start)) % buffer.Length;
                int destinationIndex = (sourceIndex + 1) % buffer.Length;
                int tail = End - index;
                int length = Math.Min(tail, Math.Min(buffer.Length - sourceIndex, buffer.Length - destinationIndex));
                Array.Copy(buffer, sourceIndex, buffer, destinationIndex, length);
                Array.Copy(buffer, (sourceIndex + length) % buffer.Length, buffer, (destinationIndex + length) % buffer.Length, tail - length);
                buffer[sourceIndex] = item;
                ++Count;
            } else if (index == Start - 1) {
                PushStart(item);
            } else if (index == End) {
                PushEnd(item);
            } else {
                throw new IndexOutOfRangeException();
            }
        }

        public void RemoveAt(int index) {
            if (index < Start || End <= index) {
                throw new IndexOutOfRangeException();
            }

            int destinationIndex = (bufferStart + (index - Start)) % buffer.Length;
            int sourceIndex = (destinationIndex + 1) % buffer.Length;
            int tail = End - index - 1;
            int length = Math.Min(tail, Math.Min(buffer.Length - sourceIndex, buffer.Length - destinationIndex));
            Array.Copy(buffer, sourceIndex, buffer, destinationIndex, length);
            Array.Copy(buffer, (sourceIndex + length) % buffer.Length, buffer, (destinationIndex + length) % buffer.Length, tail - length);
            --Count;
        }

        private void EnsureSize() {
            if (Count == buffer.Length) {
                var newBuffer = new T[buffer.Length * 2];
                for (int i = 0; i < Count; ++i) {
                    newBuffer[i] = buffer[(bufferStart + i) % buffer.Length];
                }

                buffer = newBuffer;
                bufferStart = 0;
            }
        }

        public T this[int index] {
            get {
                if (Start <= index && index < End) {
                    return buffer[(bufferStart + (index - Start)) % buffer.Length];
                } else {
                    throw new IndexOutOfRangeException();
                }
            }

            set {
                if (Start <= index && index < End) {
                    buffer[(bufferStart + (index - Start)) % buffer.Length] = value;
                } else if (index == Start - 1) {
                    PushStart(value);
                } else if (index == End) {
                    PushEnd(value);
                } else {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public int BinarySearch(T item) {
            if (!typeof(IComparable<T>).IsAssignableFrom(typeof(T))) {
                throw new InvalidOperationException($"{typeof(T)} must implement {typeof(IComparable<T>)}");
            }

            return BinarySearch(item, Comparer<T>.Default);
        }

        public int BinarySearch(T item, IComparer<T> comparer) {
            int length = Math.Min(buffer.Length - bufferStart, Count);
            int result = Array.BinarySearch(buffer, bufferStart, length, item, comparer);
            if (result >= 0) {
                return Start + result - bufferStart;
            }

            if (~result < bufferStart + length) {
                return ~(Start + ~result - bufferStart);
            }

            // item is greater than all elements in range
            int searchStart = (bufferStart + length) % buffer.Length;
            result = Array.BinarySearch(buffer, searchStart, Count - length, item, comparer);
            if (result < 0) {
                return ~(Start + length + ~result - searchStart);
            } else {
                return Start + length + result - searchStart;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int IndexOf(T item) {
            for (int i = Start; i < End; ++i) {
                if (Equals(this[i], item)) {
                    return i;
                }
            }

            return -1;
        }

        public void Add(T item) {
            PushEnd(item);
        }

        public bool Contains(T item) {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            for (int i = Start; i < End; ++i) {
                array[arrayIndex + i] = this[i];
            }
        }

        public bool Remove(T item) {
            int index = IndexOf(item);
            if (index >= 0) {
                RemoveAt(index);
            }

            return index >= 0;
        }

        private class Enumerator : IEnumerator<T> {
            private readonly Ring<T> ring;
            private int index;

            public Enumerator(Ring<T> ring) {
                this.ring = ring;
                index = -1;
            }

            public T Current => ring.buffer[(ring.bufferStart + index) % ring.buffer.Length];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext() {
                ++index;
                return index < ring.Count;
            }

            public void Reset() {
                index = -1;
            }
        }
    }
}