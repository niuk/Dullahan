using UnityEngine;

namespace Dullahan.Unity {
    public interface IBoxColliderComponent : ECS.IComponent {
        Vector3 center { get; set; }
        Vector3 size { get; set; }
    }
}