using UnityEngine;
using UnityEditor;

namespace BeatThat.ManagePrefabInstances
{
	public static class Ext 
	{
		
		public static void EditPrefabRecursive(this ManagesPrefabInstances p)
		{
			using (var pInstances = ListPool<PrefabInstance>.Get ())
			{
				p.GetPrefabInstances(pInstances, ensureCreated:true);

				foreach(var pInst in pInstances) {
					using (var nested = ListPool<ManagesPrefabInstances>.Get ()) 
					{
						pInst.instance.GetComponentsInChildren<ManagesPrefabInstances>(true, nested);
						foreach (var nestedP in nested) {
							nestedP.EditPrefabRecursive ();
						}
					}
				}
			}
		}

		public static void OnInspectorGUI_EditPrefabs(this ManagesPrefabInstances p)
		{
			var bkgColorSave = GUI.backgroundColor;

			using (var instances = ListPool<PrefabInstance>.Get ()) {
				p.GetPrefabInstances (instances, ensureCreated: false);

				if (instances.Count > 0) {
					switch (p.defaultInstancePolicy) {
					case PrefabInstancePolicy.AllowApplyInstanceToParentPrefab:

						EditorGUILayout.HelpBox (@"Be careful with Apply! 

Instance Policy 'AllowApplyInstanceToParentPrefab' means Apply will bake nested prefabs into the parent", MessageType.Warning);
						break;

					default:
						EditorGUILayout.HelpBox (@"About Apply... 

Instance Policy " + p.defaultInstancePolicy + @" means Apply find nested prefabs, apply their changes separately and then delete them before applying their parent prefab.

NOTE: this behaviour does NOT apply to nested prefabs that aren't managed, e.g. you just added one to the", MessageType.Info);
						break;


					}

					GUI.backgroundColor = Color.green;
					if(GUILayout.Button(p.numPrefabTypes > 1? "Delete Prefab[s]": "Delete Prefab")) {
						foreach (var pInst in instances) {
							if (pInst.instance == null) {
								continue;
							}
							Object.DestroyImmediate (pInst.instance, true);
						}
					}
				} 
				else {
					GUI.backgroundColor = Color.green;
					if(GUILayout.Button(p.numPrefabTypes > 1? "Edit Prefab[s]": "Edit Prefab")) {
						instances.Clear ();
						p.GetPrefabInstances(instances, ensureCreated:true);
					}
					if(GUILayout.Button(p.numPrefabTypes > 1? "Edit Prefab[s] and Nested Prefabs": "Edit Prefab and Nested Prefabs")) {
						p.EditPrefabRecursive ();
					}
				}

			}

			GUI.backgroundColor = bkgColorSave;
		}
	}
}
