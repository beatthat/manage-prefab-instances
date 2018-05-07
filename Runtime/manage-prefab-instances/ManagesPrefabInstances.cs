using System.Collections.Generic;
using UnityEngine;
using BeatThat.OnApplyPrefabBehaviours;

namespace BeatThat.ManagePrefabInstances
{
	public interface ManagesPrefabInstances  : OnApplyPrefabBehaviour
	{
		PrefabInstancePolicy defaultInstancePolicy { get; }

		int numPrefabTypes { get; }

		void GetPrefabInstances(ICollection<PrefabInstance> instances, bool ensureCreated = false);

		void GetPrefabTypes(ICollection<PrefabType> types);
	}

	public static class ManagesPrefabInstancesExt 
	{
		
		#if UNITY_EDITOR
		public static void ApplyManagedPrefabInstancesThenRemoveFromParentPrefab(this ManagesPrefabInstances mpi)
		{
			using(var instances = ListPool<PrefabInstance>.Get()) {

				mpi.GetPrefabInstances(instances);

				for(var i = instances.Count - 1; i >= 0; i--) {
					if(instances[i].instancePolicy == PrefabInstancePolicy.AllowApplyInstanceToParentPrefab) {
						instances.RemoveAt(i);
						continue;
					}

					if(instances[i].prefab == null) {

						Debug.LogError ("[" + Time.frameCount + "][" + (mpi as Component).Path () 
							+ "] no prefab for instance with type " + instances[i].prefabType);

						instances.RemoveAt(i);
						continue;
					}

					UnityEditor.PrefabUtility.ReplacePrefab (instances[i].instance, instances[i].prefab, UnityEditor.ReplacePrefabOptions.ReplaceNameBased);
				}

				foreach(var pInst in instances) {

					Object.DestroyImmediate (pInst.instance.gameObject, true);
				}

			}
		}
		#endif

		public static void ObjectsToPrefabInstances(this ManagesPrefabInstances mpi, ICollection<GameObject> objects, IList<PrefabType> prefabTypes, ICollection<PrefabInstance> prefabInstances, PrefabInstancePolicy instancePolicy)
		{
			foreach(var i in objects) {
				PrefabType pt;
				MatchInstanceToPrefab(i, prefabTypes, out pt);

				prefabInstances.Add(new PrefabInstance {
					prefab = pt.prefab,
					instance = i.gameObject,
					instancePolicy = instancePolicy
				});
			}
		}

		private static bool MatchInstanceToPrefab(GameObject inst, IList<PrefabType> prefabTypes, out PrefabType type)
		{
			type = default(PrefabType);

			var instType = inst.GetType ();

			if (prefabTypes.Count == 1 && prefabTypes [0].GetType ().IsAssignableFrom (instType)) {
				type = prefabTypes [0];
				return true;
			}

			var matchIx = -1;
			for (var i = 0; i < prefabTypes.Count; i++) {
				if (inst.GetComponent(prefabTypes[i].prefabType) == null) {
					//					Debug.Log ("[" + Time.frameCount + "] " + prefabTypes [i].prefabType + " not found on instance " + inst.Path ());
					continue;
				}

				if (matchIx != -1) {
					Debug.LogWarning ("[" + Time.frameCount + "] found multiple prefab matches for instance " + inst.Path ());
					return false;
				}

				matchIx = i;
			}

			if (matchIx == -1) {
				Debug.LogWarning ("[" + Time.frameCount + "] found 0 prefab matches for instance " + inst.Path ());
				return false;
			}

			type = prefabTypes [matchIx];
			return true;

		}
	}
}

