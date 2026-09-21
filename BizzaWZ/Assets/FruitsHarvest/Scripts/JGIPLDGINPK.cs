using System;

[AttributeUsage(AttributeTargets.Class)]
public class JGIPLDGINPK : Attribute
{
	public readonly string BindGameObjName;

	public JGIPLDGINPK(string InBindGameObjName)
	{
		BindGameObjName = InBindGameObjName;
	}
}
