public class PIINPFGBHJN : global::JLBIJKJEKKB<float>
{
	protected override float CalValue(float y)
	{
		return startValue + dValue * y;
	}

	protected override void SetDValue(float startValue, float endValue)
	{
		dValue = endValue - startValue;
	}
}
