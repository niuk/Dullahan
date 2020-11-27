namespace Dullahan {
    public class PrimitiveDiffer<T> : IDiffer<T, T> where T : struct {
        public bool Diff(T left, T right, out T diff) {
            diff = right;
            return Equals(left, right);
        }

        public void Patch(ref T diffable, T diff) {
            diffable = diff;
        }
    }
}