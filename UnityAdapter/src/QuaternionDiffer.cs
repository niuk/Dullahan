using UnityEngine;

namespace Dullahan.Unity {
    public class QuaternionDiffer : IDiffer<Quaternion, Quaternion> {
        public bool Diff(Quaternion left, Quaternion right, out Quaternion diff) {
            diff = right;
            return left != right;
        }

        public void Patch(ref Quaternion diffable, Quaternion diff) {
            diffable = diff;
        }
    }
}
