namespace Dullahan.Unity {
    public interface IRigidbodyComponent : ECS.IComponent {
        float mass { get; set; }
        float drag { get; set; }
        float angularDrag { get; set; }
        bool useGravity { get; set; }
    }
}
