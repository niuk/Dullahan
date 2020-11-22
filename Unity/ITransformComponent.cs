using UnityEngine;

using Dullahan;

public interface ITransformComponent : IComponent {
    public Vector3 position { get; set; }
    public Quaternion rotation { get; set; }
    public Vector3 scale { get; set; }
}