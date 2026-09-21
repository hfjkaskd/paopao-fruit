using UnityEngine;

public class MyTweenEase : MonoBehaviour
{
	public AnimationCurve linner;

	public AnimationCurve out1;

	public AnimationCurve in1;

	public AnimationCurve inOut1;

	public static MyTweenEase Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
	}
}
