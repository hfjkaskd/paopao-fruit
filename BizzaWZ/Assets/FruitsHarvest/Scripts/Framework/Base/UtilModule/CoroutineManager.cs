using System.Collections;
using Framework.Base.Pattern;
using UnityEngine;

namespace Framework.Base.UtilModule
{
	[JGIPLDGINPK("CoroutineMgr")]
	public class CoroutineManager : MonoSingleton<CoroutineManager>
	{
		public Coroutine StartCor(IEnumerator InEnumerator, GameObject InBindGameObj)
		{
			if (InBindGameObj != null)
			{
				CoroutineExecutor executor = InBindGameObj.GetComponent<CoroutineExecutor>();
				if (executor == null)
				{
					executor = InBindGameObj.AddComponent<CoroutineExecutor>();
				}
				return executor.StartCoroutine(InEnumerator);
			}
			return StartCoroutine(InEnumerator);
		}

		public Coroutine StartCor(IEnumerator InEnumerator)
		{
			return StartCoroutine(InEnumerator);
		}

		public void StopCor(Coroutine InCoroutine)
		{
			if (InCoroutine != null)
			{
				StopCoroutine(InCoroutine);
			}
		}
	}
}
