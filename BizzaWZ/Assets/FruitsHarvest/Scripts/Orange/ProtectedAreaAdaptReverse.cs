using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Orange
{
	public class ProtectedAreaAdaptReverse : MyMonoBehaviour
	{
		private sealed class BBHKECBMHPA : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public ProtectedAreaAdaptReverse _003C_003E4__this;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public BBHKECBMHPA(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[SerializeField]
		private bool m_IsNeedChangeSizeDelta;

		private bool IsReversed;

		private void Start()
		{
			Do();
		}

		/// <summary>父链已被缩进安全区时，把本节点（背景/全屏页根）反向扩回全屏。</summary>
		private void Do()
		{
			if (IsReversed)
			{
				return;
			}
			IsReversed = true;
			SafeAreaSim.ExpandRect(transform as RectTransform, m_IsNeedChangeSizeDelta);
		}

		[IteratorStateMachine(typeof(BBHKECBMHPA))]
		private IEnumerator Wait()
		{
			return null;
		}
	}
}
