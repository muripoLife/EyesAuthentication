using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace FaceTrackerSample
{
	/// <summary>
	/// Face tracker AR サンプル.
	/// サンプルのリファレンス http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
	/// </summary>
	public class Eye : MonoBehaviour
	{
		/// <summary>
		/// 点を描画します.
		/// </summary>
		public bool isDrawPoints;

		/// <summary>
		/// 右目.
		/// </summary>
		public GameObject rightEye;

		/// <summary>
		/// 左目.
		/// </summary>
		public GameObject leftEye;

		/// <summary>
		/// rvecノイズフィルタ範囲.
		/// </summary>
		[Range(0, 50)]
		public float rvecNoiseFilterRange = 8;

		/// <summary>
		/// tvecノイズフィルタ範囲.
		/// </summary>
		[Range(0, 360)]
		public float tvecNoiseFilterRange = 90;

		/// <summary>
		/// webカメラテクスチャー.
		/// </summary>
		WebCamTexture webCamTexture;

		/// <summary>
		/// webカメラデバイス.
		/// </summary>
		WebCamDevice webCamDevice;

		/// <summary>
		/// 色の設定.
		/// </summary>
		Color32[] colors;

		/// <summary>
		/// フロントフェーシングを使用する必要がある.
		/// </summary>
		public bool shouldUseFrontFacing = true;

		/// <summary>
		/// 幅.
		/// </summary>
		int width = 640;

		/// <summary>
		/// 高さ.
		/// </summary>
		int height = 480;

		/// <summary>
		/// RGBA行列.
		/// </summary>
		Mat rgbaMat;

		/// <summary>
		/// gray行列.
		/// </summary>
		Mat grayMat;

		/// <summary>
		/// テクスチャ―.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// カスケード.
		/// </summary>
		CascadeClassifier cascade;

		/// <summary>
		/// The init done.
		/// </summary>
		bool initDone = false;

		/// <summary>
		/// 画面の向き.
		/// </summary>
		ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

		/// <summary>
		/// 顔の追跡.
		/// </summary>
		FaceTracker faceTracker;

		/// <summary>
		/// 顔の追跡のパラメータ.
		/// </summary>
		FaceTrackerParams faceTrackerParams;

		/// <summary>
		/// ARカメラ.
		/// </summary>
		public Camera ARCamera;

		/// <summary>
		/// カメラの座標変換行列.
		/// </summary>
		Mat camMatrix;

		/// <summary>
		/// 分配係数.
		/// </summary>
		MatOfDouble distCoeffs;

		/// <summary>
		/// The look at m.
		/// </summary>
		Matrix4x4 lookAtM;

		/// <summary>
		/// The transformation m.
		/// </summary>
		Matrix4x4 transformationM = new Matrix4x4();

		/// <summary>
		/// 反転Z.
		/// </summary>
		Matrix4x4 invertZM;

		/// <summary>
		/// モデル変換、ビュー変換行列X.
		/// </summary>
		Matrix4x4 modelViewMtrx;

		/// <summary>
		/// オブジェクトの座標.
		/// </summary>
		MatOfPoint3f objectPoints = new MatOfPoint3f(
			new Point3(-31, 72, 86), // 左目
			new Point3(31, 72, 86),  // 右目
			new Point3(0, 40, 114),  // 鼻
			new Point3(-23, 19, 76), // 左口
			new Point3(23, 19, 76)   // 右口
		);

		/// <summary>
		/// 画像座標.
		/// </summary>
		MatOfPoint2f imagePoints = new MatOfPoint2f();

		/// <summary>
		/// The rvec.
		/// </summary>
		Mat rvec = new Mat();

		/// <summary>
		/// The tvec.
		/// </summary>
		Mat tvec = new Mat();

		/// <summary>
		/// The rot m.
		/// </summary>
		Mat rotM = new Mat(3, 3, CvType.CV_64FC1);

		/// <summary>
		/// The old rvec.
		/// </summary>
		Mat oldRvec;

		/// <summary>
		/// The old tvec.
		/// </summary>
		Mat oldTvec;

		// 初期化
		void Start()
		{
			// 顔の追跡の初期化
			faceTracker = new FaceTracker(Utils.getFilePath("tracker_model.json"));
			// 顔の追跡パラメータの初期化
			faceTrackerParams = new FaceTrackerParams();

			StartCoroutine(init());
		}

		// プロセスを起動する
		private IEnumerator init()
		{
			rightEye.SetActive (false);
			leftEye.SetActive (false);

			Debug.Log("---------------------------------------------------------------Eye");
			Debug.Log(leftEye.transform.localPosition);
			Debug.Log(rightEye.transform.localPosition);
			Debug.Log("---------------------------------------------------------------Eye");


			if (webCamTexture != null)
			{
				faceTracker.reset();

				webCamTexture.Stop();
				initDone = false;

				rgbaMat.Dispose();
				grayMat.Dispose();
				cascade.Dispose();
				camMatrix.Dispose();
				distCoeffs.Dispose();
			}

			// カメラがデバイスで使用可能かチェック
			for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
			{
				if (WebCamTexture.devices[cameraIndex].isFrontFacing == shouldUseFrontFacing)
				{
					Debug.Log(cameraIndex + " name " + WebCamTexture.devices[cameraIndex].name + " isFrontFacing " + WebCamTexture.devices[cameraIndex].isFrontFacing);
					webCamDevice = WebCamTexture.devices[cameraIndex];
					webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
					break;
				}
			}

			if (webCamTexture == null)
			{
				webCamDevice = WebCamTexture.devices[0];
				webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
			}

			Debug.Log("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);

			// カメラを起動します
			webCamTexture.Play();
			while (true)
			{
				// iOSの上webcamTexture.widthとwebcamTexture.heightを使用する場合は、それ以外の場合はこれら2つの値が16に等しくなり、webcamTexture.didUpdateThisFrame== 1まで待つ必要があります.
				#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				if (webCamTexture.width > 16 && webCamTexture.height > 16)
				{
				#else
					if (webCamTexture.didUpdateThisFrame)
					{
					#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2
						while (webCamTexture.width <= 16)
						{
						webCamTexture.GetPixels32 ();
						yield return new WaitForEndOfFrame ();
						}
					#endif
				#endif
					Debug.Log("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
					Debug.Log("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

					colors = new Color32[webCamTexture.width * webCamTexture.height];

					rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
					grayMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);

					texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

					gameObject.GetComponent<Renderer>().material.mainTexture = texture;

					updateLayout();

					cascade = new CascadeClassifier(Utils.getFilePath("haarcascade_frontalface_alt.xml"));
					if (cascade.empty())
					{
						Debug.LogError ("cascade file is not loaded.Please copy from “FaceTrackerSample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
					}

					int max_d = Mathf.Max(rgbaMat.rows(), rgbaMat.cols());
					camMatrix = new Mat(3, 3, CvType.CV_64FC1);
					camMatrix.put(0, 0, max_d);
					camMatrix.put(0, 1, 0);
					camMatrix.put(0, 2, rgbaMat.cols() / 2.0f);
					camMatrix.put(1, 0, 0);
					camMatrix.put(1, 1, max_d);
					camMatrix.put(1, 2, rgbaMat.rows() / 2.0f);
					camMatrix.put(2, 0, 0);
					camMatrix.put(2, 1, 0);
					camMatrix.put(2, 2, 1.0f);

					Size imageSize = new Size(rgbaMat.cols(), rgbaMat.rows());
					double apertureWidth = 0;
					double apertureHeight = 0;
					double[] fovx = new double[1];
					double[] fovy = new double[1];
					double[] focalLength = new double[1];
					Point principalPoint = new Point(); // 主点
					double[] aspectratio = new double[1];

					Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

					Debug.Log("imageSize " + imageSize.ToString());
					Debug.Log("apertureWidth " + apertureWidth);
					Debug.Log("apertureHeight " + apertureHeight);
					Debug.Log("fovx " + fovx[0]);
					Debug.Log("fovy " + fovy[0]);
					Debug.Log("focalLength " + focalLength[0]);
					Debug.Log("--------------------------principalPoint");
					Debug.Log("principalPoint " + principalPoint.ToString());
					Debug.Log("--------------------------principalPoint");


					Debug.Log("aspectratio " + aspectratio[0]);

					ARCamera.fieldOfView = (float)fovy[0];

					Debug.Log("camMatrix " + camMatrix.dump());

					distCoeffs = new MatOfDouble(0, 0, 0, 0);
					Debug.Log("distCoeffs " + distCoeffs.dump());

					lookAtM = getLookAtMatrix(new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, -1, 0));
					Debug.Log("lookAt " + lookAtM.ToString());

					invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));

					screenOrientation = Screen.orientation;
					initDone = true;
					break;
					}
					else
					{
						yield return 0;
					}
				}
			}

		private void updateLayout()
		{
			gameObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
			gameObject.transform.localScale = new Vector3(webCamTexture.width, webCamTexture.height, 1);

			if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
			{
				gameObject.transform.eulerAngles = new Vector3(0, 0, -90);
			}

			float width = 0;
			float height = 0;
			if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
			{
				width = gameObject.transform.localScale.y;
				height = gameObject.transform.localScale.x;
			}
			else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180)
			{
				width = gameObject.transform.localScale.x;
				height = gameObject.transform.localScale.y;
			}

			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale < heightScale)
			{
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
			}
			else
			{
				Camera.main.orthographicSize = height / 2;
			}
		}

		// 更新は、フレームごとに呼ばれます
		void Update()
		{
			if (!initDone)
				return;

			if (screenOrientation != Screen.orientation)
			{
				screenOrientation = Screen.orientation;
				updateLayout();
			}

			#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				if (webCamTexture.width > 16 && webCamTexture.height > 16) {
			#else
				if (webCamTexture.didUpdateThisFrame)
				{
			#endif
				Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
				// 方向を修正するために反転.
				if (webCamDevice.isFrontFacing)
				{
					if (webCamTexture.videoRotationAngle == 0)
					{
						Core.flip(rgbaMat, rgbaMat, 1);
					}
					else if (webCamTexture.videoRotationAngle == 90)
					{
						Core.flip(rgbaMat, rgbaMat, 0);
					}
					if (webCamTexture.videoRotationAngle == 180)
					{
						Core.flip(rgbaMat, rgbaMat, 0);
					}
					else if (webCamTexture.videoRotationAngle == 270)
					{
						Core.flip(rgbaMat, rgbaMat, 1);
					}
				}
				else
				{
					if (webCamTexture.videoRotationAngle == 180)
					{
						Core.flip(rgbaMat, rgbaMat, -1);
					}
					else if (webCamTexture.videoRotationAngle == 270)
					{
						Core.flip(rgbaMat, rgbaMat, -1);
					}
				}
				// グレースケールにイメージを変換します
				Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

				if (faceTracker.getPoints().Count <= 0)
				{
					Debug.Log("detectFace");

					// グレースケールにイメージを変換します
					using (Mat equalizeHistMat = new Mat())
					using (MatOfRect faces = new MatOfRect())
					{
						Imgproc.equalizeHist(grayMat, equalizeHistMat);

						cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0
								| Objdetect.CASCADE_FIND_BIGGEST_OBJECT
								| Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size(equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), new Size());

						if (faces.rows() > 0)
						{
							Debug.Log("--------------------------faces");
							Debug.Log("faces " + faces.dump());
							Debug.Log("--------------------------faces");
							// MatOfRectから顔の初期座標を追加
							faceTracker.addPoints(faces);

							// 顔の四角形を描きます
							OpenCVForUnity.Rect[] rects = faces.toArray();
							for (int i = 0; i < rects.Length; i++)
							{
								Core.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
							}
						}
					}
				}

				// 顔の座標を追跡します.if face points <= 0, always return false.
				if (faceTracker.track(grayMat, faceTrackerParams))
				{
					if (isDrawPoints)
						//Debug.Log("--------------------------100-----------------------------------");
						faceTracker.draw(rgbaMat, new Scalar(255, 0, 0, 255), new Scalar(0, 255, 0, 255));
						//Debug.Log("--------------------------1000");
					//Core.putText(rgbaMat, "'Tap' or 'Space Key' to Reset", new Point(5, rgbaMat.rows() - 5), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(255, 255, 255, 255), 2, Core.LINE_AA, false);

					Point[] points = faceTracker.getPoints()[0];

                    //eyeWidth = points[36] - points[31];
/*
                    Debug.Log("--------------------------0");
                    Debug.Log(points[31]);
                    Debug.Log("--------------------------2");
                    Debug.Log(points[36]);
                    Debug.Log("--------------------------1");
*/
					if (points.Length > 0)
					{
						imagePoints.fromArray(
							points[31], //l eye
							points[36], //r eye
							points[67], // nose
							points[48], //l mouth
							points[54]  //r mouth
						);

						Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

						bool isRefresh = false;

						if (tvec.get(2, 0)[0] > 0 && tvec.get(2, 0)[0] < 1200 * ((float)webCamTexture.width / (float)width))
						{

							isRefresh = true;

							if (oldRvec == null)
							{
								oldRvec = new Mat();
								rvec.copyTo(oldRvec);
							}
							if (oldTvec == null)
							{
								oldTvec = new Mat();
								tvec.copyTo(oldTvec);
							}

							// Rvecノイズをフィルタリング.
							using (Mat absDiffRvec = new Mat())
							{
								Core.absdiff(rvec, oldRvec, absDiffRvec);

								using (Mat cmpRvec = new Mat())
								{
									Core.compare(absDiffRvec, new Scalar(rvecNoiseFilterRange), cmpRvec, Core.CMP_GT);

									if (Core.countNonZero(cmpRvec) > 0)
										isRefresh = false;
								}
							}

							// Tvecノイズをフィルタリング.
							using (Mat absDiffTvec = new Mat())
							{
								Core.absdiff(tvec, oldTvec, absDiffTvec);
								using (Mat cmpTvec = new Mat())
								{
									Core.compare(absDiffTvec, new Scalar(tvecNoiseFilterRange), cmpTvec, Core.CMP_GT);

									if (Core.countNonZero(cmpTvec) > 0)
										isRefresh = false;
								}
							}
						}

						if (isRefresh)
						{
							if (!rightEye.activeSelf)
								rightEye.SetActive (true);
							if (!leftEye.activeSelf)
								leftEye.SetActive (true);

							rvec.copyTo(oldRvec);
							tvec.copyTo(oldTvec);

							Calib3d.Rodrigues(rvec, rotM);

							transformationM.SetRow(0, new Vector4((float)rotM.get(0, 0)[0], (float)rotM.get(0, 1)[0], (float)rotM.get(0, 2)[0], (float)tvec.get(0, 0)[0]));
							transformationM.SetRow(1, new Vector4((float)rotM.get(1, 0)[0], (float)rotM.get(1, 1)[0], (float)rotM.get(1, 2)[0], (float)tvec.get(1, 0)[0]));
							transformationM.SetRow(2, new Vector4((float)rotM.get(2, 0)[0], (float)rotM.get(2, 1)[0], (float)rotM.get(2, 2)[0], (float)tvec.get(2, 0)[0]));
							transformationM.SetRow(3, new Vector4(0, 0, 0, 1));

							modelViewMtrx = lookAtM * transformationM * invertZM;
							ARCamera.worldToCameraMatrix = modelViewMtrx;
						}
					}
				}
				Utils.matToTexture2D(rgbaMat, texture, colors);
			}

			if (Input.GetKeyUp(KeyCode.Space) || Input.touchCount > 0)
			{
				faceTracker.reset();
				if (oldRvec != null)
				{
					oldRvec.Dispose();
					oldRvec = null;
				}
				if (oldTvec != null)
				{
					oldTvec.Dispose();
					oldTvec = null;
				}
				ARCamera.ResetWorldToCameraMatrix();

				rightEye.SetActive (false);
				leftEye.SetActive (false);

			}
		}

		void OnDisable()
		{
			webCamTexture.Stop();
		}

		private Matrix4x4 getLookAtMatrix(Vector3 pos, Vector3 target, Vector3 up)
		{
			Vector3 z = Vector3.Normalize(pos - target);
			Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
			Vector3 y = Vector3.Normalize(Vector3.Cross(z, x));

			Matrix4x4 result = new Matrix4x4();
			result.SetRow(0, new Vector4(x.x, x.y, x.z, -(Vector3.Dot(pos, x))));
			result.SetRow(1, new Vector4(y.x, y.y, y.z, -(Vector3.Dot(pos, y))));
			result.SetRow(2, new Vector4(z.x, z.y, z.z, -(Vector3.Dot(pos, z))));
			result.SetRow(3, new Vector4(0, 0, 0, 1));

			return result;
		}

		void OnGUI()
		{
			float screenScale = Screen.height / 240.0f;
			Matrix4x4 scaledMatrix = Matrix4x4.Scale(new Vector3(screenScale, screenScale, screenScale));
			GUI.matrix = scaledMatrix;
/*
			GUILayout.BeginVertical();
			if (GUILayout.Button("change camera"))
			{
				shouldUseFrontFacing = !shouldUseFrontFacing;
				StartCoroutine(init());
			}

			if (GUILayout.Button("drawPoints"))
			{
				if (isDrawPoints)
				{
					isDrawPoints = false;
				}
				else
				{
					isDrawPoints = true;
				}
			}
*/
			GUILayout.EndVertical();
		}
	}
}