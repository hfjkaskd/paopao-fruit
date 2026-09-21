public class FOLJNEPEKCA<T> where T : class, new()
{
	private static T t;

	public static T Instance
	{
		get
		{
			if (t == null)
			{
				t = new T();
			}
			return t;
		}
	}
}
