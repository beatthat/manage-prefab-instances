using System.Collections.Generic;
using UnityEngine;
using BeatThat.OnApplyPrefabBehaviours;

namespace BeatThat.ManagePrefabInstances
{
	public interface ManagesPrefabInstances  : OnApplyPrefabBehaviour
	{
		PrefabInstancePolicy defaultInstancePolicy { get; }

		bool supportsMultiplePrefabTypes { get; }

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

		public delegate GameObject AddInstanceDelegate(GameObject prefab);

		public static void FindPrefabInstances(
			this ManagesPrefabInstances manager, 
			ICollection<PrefabInstance> instances, 
			PrefabInstancePolicy defaultInstancePolicy,
			bool ensureCreated = false, 
			AddInstanceDelegate addInstanceDelegate = null,
			Transform parent = null
		)
		{
			using (var foundItems = ListPool<GameObject>.Get())
            using (var foundObjects = ListPool<GameObject>.Get())
            using (var prefabTypes = ListPool<PrefabType>.Get())
            {
				(parent ?? (manager as Component).transform).GetComponentsInDirectChildren(foundItems, true);
               
                manager.GetPrefabTypes(prefabTypes);

                if (ensureCreated && foundItems.Count == 0)
                {
                    foreach (var pt in prefabTypes)
                    {
                        if (pt.prefab == null)
                        {
                            Debug.LogWarning("[" + Time.frameCount + "] encountered null prefab for type " + pt.prefabType);
                            continue;
                        }

						var prefab = pt.prefab as GameObject ?? 
						               (pt.prefab is Component)?(pt.prefab as Component).gameObject: null;
						
                        if (prefab == null)
                        {
							Debug.LogWarning("[" + Time.frameCount + "] unable to find prefab GameObject");
                            continue;
                        }
						foundItems.Add(addInstanceDelegate(prefab));
                    }
                }

                foreach (var c in foundItems)
                {
                    foundObjects.Add(c.gameObject);
                }

				manager.ObjectsToPrefabInstances(foundObjects, prefabTypes, instances, defaultInstancePolicy);
            }
        }

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

