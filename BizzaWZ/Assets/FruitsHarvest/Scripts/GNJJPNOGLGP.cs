using UnityEngine;

public class GNJJPNOGLGP : global::JLBIJKJEKKB<Vector3>
{
	protected override Vector3 CalValue(float y)
	{
		return startValue + dValue * y;
	}

	protected override void SetDValue(Vector3 startValue, Vector3 endValue)
	{
		dValue = endValue - startValue;
	}
}
