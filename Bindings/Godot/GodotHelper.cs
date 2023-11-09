#if GODOT

using Godot.Collections;
using Godot;
using HiHi.Common;
using HiHi.Serialization;

namespace HiHi {
	public partial class GodotHelper : Node, IHelper {
		public static GodotHelper Instance { get; private set; }

		[ExportGroup("Spawning")]
		[Export] public Array<GodotSpawnData> SpawnDataRegistry = new Array<GodotSpawnData>();

		public override void _EnterTree() {
			base._EnterTree();

			Instance = this;
		}

        void IHelper.SerializeSpawnData(ISpawnData spawnData, BitBuffer buffer) {
			spawnData.Serialize(buffer);
		}

		ISpawnData IHelper.DeserializeSpawnData(BitBuffer buffer) {
			byte spawnDataIndex = buffer.ReadByte();

			if (SpawnDataRegistry.Count <= spawnDataIndex) {
				throw new HiHiException($"Received spawn message referencing spawn index {spawnDataIndex}. Which doesn't exist in the {nameof(GodotHelper)}.{nameof(SpawnDataRegistry)}. Make sure your spawndata is the same across peers.");
			}

			return SpawnDataRegistry[spawnDataIndex];
		}
	}
}

#endif
