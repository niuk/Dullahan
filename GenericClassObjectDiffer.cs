namespace Dullahan {
    public class GenericClassObjectDiffer<T> : IDiffer<T, T> where T : class {
        public bool Diff(T left, T right, out T diff) {
            diff = right;
            return !ReferenceEquals(left, right);
        }

        public void Patch(ref T diffable, T diff) {
            throw new System.NotImplementedException();
        }
    }
}