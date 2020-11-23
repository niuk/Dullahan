namespace Dullahan {
    public class SingleDiffer : IDiffer<float, float> {
        public bool Diff(float left, float right, out float diff) {
            diff = right;
            return left == right;
        }

        public void Patch(ref float diffable, float diff) {
            diffable = diff;
        }
    }

    public class BoolDiffer : IDiffer<bool, bool> {
        public bool Diff(bool left, bool right, out bool diff) {
            diff = right;
            return left != right;
        }

        public void Patch(ref bool diffable, bool diff) {
            diffable = diff;
        }
    }
}