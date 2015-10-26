using UnityEngine;
using System.Collections;

public class Blackout : MonoBehaviour {

	//追跡対象
	[SerializeField] GameObject upBox;
	[SerializeField] GameObject downBox;
	[SerializeField] GameObject leftBox;
	[SerializeField] GameObject rightBox;
	//[SerializeField] GameObject EYE_L;
	//[SerializeField] GameObject EYE_R;

	// 初期化
	void Start()
	{
		Invoke("Box", 3.0f);
	}

	void Box()
	{
		Vector3 point = upBox.transform.localPosition;
		//point.y = EYE_L.transform.localPosition.y;
		point.z = 1;
		upBox.transform.localPosition = point;

		Vector3 point1 = downBox.transform.localPosition;
		//point1.y = EYE_L.transform.localPosition.y;
		point1.z = 1;
		downBox.transform.localPosition = point1;

		Vector3 point2 = leftBox.transform.localPosition;
		//point2.x = EYE_L.transform.localPosition.x;
		point2.z = 1;
		leftBox.transform.localPosition = point2;

		Vector3 point3 = rightBox.transform.localPosition;
		//point2.x = EYE_R.transform.localPosition.x;
		point3.z = 1;
		rightBox.transform.localPosition = point3;
	}
}