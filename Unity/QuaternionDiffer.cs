using UnityEngine;

namespace Dullahan.Unity {
    public class QuaternionDiffer : IDiffer<Quaternion, Quaternion?> {
        public bool Diff(Quaternion left, Quaternion right, out Quaternion? diff) {
            if (left != right) {
                diff = right;
                return true;
            } else {
                diff = null;
                return false;
            }
        }

        public void Patch(ref Quaternion diffable, Quaternion? diff) {
            throw new System.NotImplementedException();
        }
    }
}
