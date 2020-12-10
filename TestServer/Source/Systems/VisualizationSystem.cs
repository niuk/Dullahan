using Dullahan.ECS;
using System;
using System.Collections.Generic;

namespace TestGame {
    public abstract class VisualizationSystem : ISystem {
        protected abstract IEnumerable<IPositionComponent> positionComponents { get; }

        public void Tick() {
            /*Console.Clear();
            foreach (var positionComponent in positionComponents) {
                Console.SetCursorPosition(positionComponent.x, positionComponent.y);
                Console.Write("@");
            }*/
        }
    }
}