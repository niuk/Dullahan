using UnityEngine;

namespace Dullahan.Unity {
    public interface ISphereColliderComponent : ECS.IComponent {
        Vector3 center { get; set; }
        float radius { get; set; }
    }
}
