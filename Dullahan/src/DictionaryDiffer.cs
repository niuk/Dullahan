using System.Collections.Generic;
using System.IO;

namespace Dullahan {
    public class DictionaryDiffer<TKey, TValue> : IDiffer<IDictionary<TKey, TValue>> {
        private readonly IDiffer<TKey> keyDiffer;
        private readonly IDiffer<TValue> valueDiffer;

        public DictionaryDiffer(IDiffer<TKey> keyDiffer, IDiffer<TValue> valueDiffer) {
            this.keyDiffer = keyDiffer;
            this.valueDiffer = valueDiffer;
        }

        public bool Diff(IDictionary<TKey, TValue> oldDictionary, IDictionary<TKey, TValue> newDictionary, BinaryWriter writer) {
            // reserve room for changed count
            int startPosition = writer.GetOffset();
            writer.Write(0);

            int changedCount = 0;
            var removed = new HashSet<TKey>();
            var added = new HashSet<TKey>();
            foreach (var pair in oldDictionary) {
                if (newDictionary.TryGetValue(pair.Key, out TValue newValue)) {
                    // preemptively write the key diff; we'll erase it if the value didn't change
                    int keyPosition = writer.GetOffset();
                    keyDiffer.Diff(default, pair.Key, writer);
                    if (valueDiffer.Diff(oldDictionary[pair.Key], newDictionary[pair.Key], writer)) {
                        ++changedCount;
                    } else {
                        writer.SetOffset(keyPosition);
                    }
                } else {
                    removed.Add(pair.Key);
                }
            }

            int savedPosition = writer.GetOffset();
            writer.SetOffset(startPosition);
            writer.Write(changedCount);
            writer.SetOffset(savedPosition);

            foreach (var pair in newDictionary) {
                if (oldDictionary.TryGetValue(pair.Key, out TValue oldValue)) {
                    // already diffed
                } else {
                    added.Add(pair.Key);
                }
            }

            writer.Write(removed.Count);
            foreach (var key in removed) {
                keyDiffer.Diff(default, key, writer);
            }

            writer.Write(added.Count);
            foreach (var key in added) {
                keyDiffer.Diff(default, key, writer);
                valueDiffer.Diff(default, newDictionary[key], writer);
            }

            return changedCount > 0 || removed.Count > 0 || added.Count > 0;
        }

        public void Patch(ref IDictionary<TKey, TValue> dictionary, BinaryReader reader) {
            int changedCount = reader.ReadInt32();
            for (int i = 0; i < changedCount; ++i) {
                TKey key = default;
                keyDiffer.Patch(ref key, reader);

                TValue value = dictionary[key];
                valueDiffer.Patch(ref value, reader);
                dictionary[key] = value;
            }

            int removedCount = reader.ReadInt32();
            for (int i = 0; i < removedCount; ++i) {
                TKey key = default;
                keyDiffer.Patch(ref key, reader);
                dictionary.Remove(key);
            }

            int addedCount = reader.ReadInt32();
            for (int i = 0; i < addedCount; ++i) {
                TKey key = default;
                TValue value = default;
                keyDiffer.Patch(ref key, reader);
                valueDiffer.Patch(ref value, reader);
                dictionary.Add(key, value);
            }
        }
    }
}
