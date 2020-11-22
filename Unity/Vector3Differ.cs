using UnityEngine;

namespace Dullahan.Unity {
    public class Vector3Differ : IDiffer<Vector3, Vector3?> {
        public bool Diff(Vector3 left, Vector3 right, out Vector3? diff) {
            if (left != right) {
                diff = right;
                return true;
            } else {
                diff = null;
                return false;
            }
        }

        public Vector3 Patch(Vector3 diffable, Vector3? diff) {
            throw new System.NotImplementedException();
        }
    }
}