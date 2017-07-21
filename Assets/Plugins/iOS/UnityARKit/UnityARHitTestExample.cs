using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;


namespace UnityEngine.XR.iOS
{
	// 必要なコンポーネントの列記
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(CapsuleCollider))]

	public class UnityARHitTestExample : MonoBehaviour
	{
		public float animSpeed = 1.5f;              // アニメーション再生速度設定
		public float lookSmoother = 3.0f;           // a smoothing setting for camera motion
		public bool useCurves = true;               // Mecanimでカーブ調整を使うか設定する
													// このスイッチが入っていないとカーブは使われない
		public float useCurvesHeight = 0.5f;        // カーブ補正の有効高さ（地面をすり抜けやすい時には大きくする）

		// 以下キャラクターコントローラ用パラメタ
		// 前進速度
		public float forwardSpeed = 0.4f;
		// 後退速度
		public float backwardSpeed = 2.0f;
		// 旋回速度
		public float rotateSpeed = 2.0f;
		// ジャンプ威力
		public float jumpPower = 3.0f;
		// キャラクターコントローラ（カプセルコライダ）の参照
		private CapsuleCollider col;
		// キャラクターコントローラ（カプセルコライダ）の移動量
		private Vector3 velocity;
		// CapsuleColliderで設定されているコライダのHeiht、Centerの初期値を収める変数
		private float orgColHight;
		private Vector3 orgVectColCenter;

		private Animator anim;                          // キャラにアタッチされるアニメーターへの参照
		private AnimatorStateInfo currentBaseState;         // base layerで使われる、アニメーターの現在の状態の参照

		private GameObject cameraObject;    // メインカメラへの参照

		// アニメーター各ステートへの参照
		static int idleState = Animator.StringToHash("Base Layer.Idle");
		static int locoState = Animator.StringToHash("Base Layer.Locomotion");
		static int jumpState = Animator.StringToHash("Base Layer.Jump");
		static int restState = Animator.StringToHash("Base Layer.Rest");

		public Transform m_HitTransform;
        private bool existed = false;
        private bool movable = false;
        private Vector3 hitARPosition;
        private Quaternion hitARRotation;
     

        private void Start()
        {
			anim = GetComponent<Animator>();
			// CapsuleColliderコンポーネントを取得する（カプセル型コリジョン）
			col = GetComponent<CapsuleCollider>();
			//メインカメラを取得する
			cameraObject = GameObject.FindWithTag("MainCamera");
			// CapsuleColliderコンポーネントのHeight、Centerの初期値を保存する
			orgColHight = col.height;
			orgVectColCenter = col.center;
            Debug.Log("Start");
        }

		void Update()
		{
			if (Input.touchCount > 0 && m_HitTransform != null)
			{
				var touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Began)
				{
					var screenPosition = Camera.main.ScreenToViewportPoint(touch.position);
					ARPoint point = new ARPoint
					{
						x = screenPosition.x,
						y = screenPosition.y
					};

					// prioritize reults types
					ARHitTestResultType[] resultTypes = {
						ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, 
                        // if you want to use infinite planes use this:
                        //ARHitTestResultType.ARHitTestResultTypeExistingPlane,
                        //ARHitTestResultType.ARHitTestResultTypeHorizontalPlane,
						//ARHitTestResultType.ARHitTestResultTypeFeaturePoint
					};

					foreach (ARHitTestResultType resultType in resultTypes)
					{
						if (HitTestWithResultType(point, resultType))
						{
							return;
						}
					}
				}
			}
            if(existed && movable) {
                TransformMovement();    
            }
		}

        bool HitTestWithResultType (ARPoint point, ARHitTestResultType resultTypes)
        {
            List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface ().HitTest (point, resultTypes);
            if (hitResults.Count > 0) {
                foreach (var hitResult in hitResults) {
                    Debug.Log ("Got hit!");
                    if(!existed) {
						m_HitTransform.position = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
						m_HitTransform.rotation = UnityARMatrixOps.GetRotation(hitResult.worldTransform);
						m_HitTransform.LookAt(Camera.main.transform.position);
						m_HitTransform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                        existed = true;
						Debug.Log(string.Format("x:{0:0.######} y:{1:0.######} z:{2:0.######}", m_HitTransform.position.x, m_HitTransform.position.y, m_HitTransform.position.z));
						return true; 
                    } else {
                        hitARPosition = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
                        hitARRotation = UnityARMatrixOps.GetRotation(hitResult.worldTransform);
                        movable = true;
						return true;
                    }

                }
            }
            return false;
        }
		
        bool TransformMovement () {
            
			float angle = Quaternion.Angle(m_HitTransform.rotation, Quaternion.LookRotation(hitARPosition - m_HitTransform.position));              // 入力デバイスの水平軸をhで定義
			float direction = angle.Equals(0) ? 0 : angle / 90;
			anim.SetFloat("Speed", forwardSpeed);                          // Animator側で設定している"Speed"パラメタにvを渡す
			anim.SetFloat("Direction", angle);                      // Animator側で設定している"Direction"パラメタにhを渡す
			anim.speed = animSpeed;                             // Animatorのモーション再生速度に animSpeedを設定する
			currentBaseState = anim.GetCurrentAnimatorStateInfo(0); // 参照用のステート変数にBase Layer (0)の現在のステートを設定す

			// 左右のキー入力でキャラクタをY軸で旋回させる
			Quaternion neededRotation = Quaternion.LookRotation(hitARPosition - m_HitTransform.position);
			//Quaternion.RotateTowards(m_HitTransform.rotation, neededRotation, Time.deltaTime * rotateSpeed);
            m_HitTransform.rotation = neededRotation;

			// 上下のキー入力でキャラクターを移動させる
            m_HitTransform.position = Vector3.Lerp(m_HitTransform.position, hitARPosition, forwardSpeed * Time.deltaTime);
			Debug.Log(string.Format("x:{0:0.######} y:{1:0.######} z:{2:0.######}", hitARPosition.x, hitARPosition.y, hitARPosition.z));
			Debug.Log(string.Format("x:{0:0.######} y:{1:0.######} z:{2:0.######}", m_HitTransform.position.x, m_HitTransform.position.y, m_HitTransform.position.z));
		

			// 以下、Animatorの各ステート中での処理
			// Locomotion中
			// 現在のベースレイヤーがlocoStateの時
			if (currentBaseState.nameHash == locoState)
			{
				//カーブでコライダ調整をしている時は、念のためにリセットする
				if (useCurves)
				{
					resetCollider();
				}
			}


			// IDLE中の処理
			// 現在のベースレイヤーがidleStateの時
			else if (currentBaseState.nameHash == idleState)
			{
				//カーブでコライダ調整をしている時は、念のためにリセットする
				if (useCurves)
				{
					resetCollider();
				}
			}
			// REST中の処理
			// 現在のベースレイヤーがrestStateの時
			else if (currentBaseState.nameHash == restState)
			{
				//cameraObject.SendMessage("setCameraPositionFrontView");       // カメラを正面に切り替える
				// ステートが遷移中でない場合、Rest bool値をリセットする（ループしないようにする）
				if (!anim.IsInTransition(0))
				{
					anim.SetBool("Rest", false);
				}
			}
            return true;
        }
		void resetCollider()
		{
			// コンポーネントのHeight、Centerの初期値を戻す
			col.height = orgColHight;
			col.center = orgVectColCenter;
		}
	
	}
}

