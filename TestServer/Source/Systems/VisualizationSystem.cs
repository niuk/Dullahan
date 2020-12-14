using Dullahan.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestGame {
    public abstract class VisualizationSystem : ISystem {
        protected abstract IEnumerable<(IPositionComponent, IViewComponent)> avatars { get; }

        protected abstract IConsoleBufferComponent consoleBuffer { get; }

        public void Tick() {
            var consoleBuffer = this.consoleBuffer.consoleBuffer;
            Console.SetBufferSize(consoleBuffer.GetLength(0), consoleBuffer.GetLength(1));
            for (int i = 0; i < consoleBuffer.GetLength(0); ++i) {
                for (int j = 0; j < consoleBuffer.GetLength(1); ++j) {
                    bool blank = true;
                    foreach (var (position, view) in avatars) {
                        if (i == (int)position.x && j == (int)position.y) {
                            blank = false;
                            if (consoleBuffer[i, j] != (byte)view.avatar) {
                                consoleBuffer[i, j] = (byte)view.avatar;
                                Console.SetCursorPosition(i, j);
                                Console.Out.Write((char)consoleBuffer[i, j]);
                            }
                        }
                    }

                    if (blank) {
                        if (consoleBuffer[i, j] != (byte)'.') {
                            consoleBuffer[i, j] = (byte)'.';
                            Console.SetCursorPosition(i, j);
                            Console.Out.Write((char)consoleBuffer[i, j]);
                        }
                    }
                }
            }
        }
    }
}