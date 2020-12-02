/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace TestServer {
    public class EntityDiffer : IDiffer<(Entity, int), (byte[], int, int)> {


        public Maybe<(byte[], int, int)> Diff((Entity, int) left, (Entity, int) right) {

        }

        public (Entity, int) Patch((Entity, int) diffable, (byte[], int, int) diff) {
            int oldTick = diff.Item1[0] | diff.Item1[1] << 8 | diff.Item1[2] << 16 | diff.Item1[3] << 24;
            int newTick = diff.Item1[4] | diff.Item1[5] << 8 | diff.Item1[6] << 16 | diff.Item1[7] << 24;

            return (diffable.Item1, newTick);
        }
    }
}
