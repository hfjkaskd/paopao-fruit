using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Orange
{
	public class ProtectedAreaAdapt : MyMonoBehaviour
	{
		private sealed class CNGCPHKMFMG : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public ProtectedAreaAdapt _003C_003E4__this;

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
			public CNGCPHKMFMG(int _003C_003E1__state)
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

		private bool hasChanged;

		private void Start()
		{
			Do();
		}

		/// <summary>
		/// 把本节点缩进顶部安全区（用于全屏页内的内容容器，如 ShopUI/SafeAera）。
		/// 录屏表明底部不缩进：商店“更多优惠”栏保持贴近屏幕底部。
		/// </summary>
		private void Do()
		{
			if (hasChanged)
			{
				return;
			}
			hasChanged = true;
			SafeAreaSim.InsetRect(transform as RectTransform, true);
		}

		[IteratorStateMachine(typeof(CNGCPHKMFMG))]
		private IEnumerator Wait()
		{
			return null;
		}
	}
}
