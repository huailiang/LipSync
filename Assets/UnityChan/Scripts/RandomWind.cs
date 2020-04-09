using System.Collections;
using UnityEngine;

namespace UnityChan
{
    public class RandomWind : MonoBehaviour
	{
		private SpringBone[] springBones;
		public bool isWindActive = true;

		private bool isMinus = false;				//風方向反転用.
		public float threshold = 0.5f;				// ランダム判定の閾値.
		public float interval = 5.0f;				// ランダム判定のインターバル.
		public float windPower = 1.0f;				//風の強さ.
		public float gravity = 0.98f;				//重力の強さ.


		// Use this for initialization
		void Start ()
		{
			springBones = GetComponent<SpringManager> ().springBones;
			StartCoroutine ("RandomChange");
		}
        
		// Update is called once per frame
		void Update ()
		{

			Vector3 force = Vector3.zero;
			if (isWindActive) {
				if(isMinus){
					force = new Vector3 (Mathf.PerlinNoise (Time.time, 0.0f) * windPower * -0.001f , gravity * -0.001f , 0);
				}else{
					force = new Vector3 (Mathf.PerlinNoise (Time.time, 0.0f) * windPower * 0.001f, gravity * -0.001f, 0);
				}

				for (int i = 0; i < springBones.Length; i++) {
					springBones [i].springForce = force;
				}
			
			}
		}

		IEnumerator RandomChange ()
		{
			while (true) {
				//ランダム判定用シード発生.
				float _seed = Random.Range (0.0f, 1.0f);

				if (_seed > threshold) {
					//_seedがthreshold以上の時、符号を反転する.
					isMinus = true;
				}else{
					isMinus = false;
				}

				// 次の判定までインターバルを置く.
				yield return new WaitForSeconds (interval);
			}
		}


	}
}