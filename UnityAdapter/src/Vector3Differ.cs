using UnityEngine;

namespace Dullahan.Unity {
    public class Vector3Differ : IDiffer<Vector3, Vector3> {
        public bool Diff(Vector3 left, Vector3 right, out Vector3 diff) {
            diff = right;
            return left != right;
        }

        public void Patch(ref Vector3 diffable, Vector3 diff) {
            throw new System.NotImplementedException();
        }
    }
}