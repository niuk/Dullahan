using System;
using System.Collections.Generic;
using Dullahan.ECS;

namespace TestGame {
    public abstract class MovementSystem : ISystem {
        protected abstract IEnumerable<(IVelocityComponent, IPositionComponent)> controllables { get; }

        protected abstract ITimeComponent timeComponent { get; }

        public virtual void Tick() {
            foreach (var (velocityComponent, positionComponent) in controllables) {
                var root = Math.Sqrt(velocityComponent.deltaX * velocityComponent.deltaX + velocityComponent.deltaY * velocityComponent.deltaY);
                if (root > double.Epsilon) {
                    var speedAdjustment = timeComponent.deltaTime * velocityComponent.speed / root;
                    positionComponent.x += (float)(speedAdjustment * velocityComponent.deltaX);
                    positionComponent.x = Math.Max(0, Math.Min(Console.WindowWidth - 1, positionComponent.x));
                    positionComponent.y += (float)(speedAdjustment * velocityComponent.deltaY);
                    positionComponent.y = Math.Max(0, Math.Min(Console.WindowHeight - 1, positionComponent.y));
                }
            }
        }
    }
}
