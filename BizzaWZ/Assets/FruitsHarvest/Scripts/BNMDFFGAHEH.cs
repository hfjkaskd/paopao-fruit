using UnityEngine;

public class BNMDFFGAHEH : global::JLBIJKJEKKB<Color>
{
	protected override Color CalValue(float y)
	{
		return startValue + dValue * y;
	}

	protected override void SetDValue(Color startValue, Color endValue)
	{
		dValue = endValue - startValue;
	}
}
