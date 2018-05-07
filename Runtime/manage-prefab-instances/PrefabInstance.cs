using UnityEngine;
using System;

namespace BeatThat.ManagePrefabInstances
{
	public struct PrefabInstance 
	{
		public UnityEngine.Object prefab;
		public Type prefabType;
		public GameObject instance;
		public PrefabInstancePolicy instancePolicy;
	}
}
