using System;

namespace MinorShift.Emuera.GameData.Expression
{
	/// <summary>
	/// CHECK_OVERFLOW命令の状態と、それに従う整数演算。
	///
	/// Emuera1824+v10+v3由来の命令。
	///  0:これまで通り、オーバーフローした場合は符号ビットがそのまま処理される
	///  1:オーバーフローした場合、正の方向ならINT64最大値、負の方向ならINT64最小値が代入される
	/// </summary>
	internal static class OverflowMode
	{
		public static bool Saturate;

		public static void Reset()
		{
			Saturate = false;
		}

		public static Int64 Add(Int64 a, Int64 b)
		{
			Int64 r = unchecked(a + b);
			//同符号同士の加算で結果の符号が変わった時だけ溢れている
			if (Saturate && ((a ^ r) & (b ^ r)) < 0)
				return a < 0 ? Int64.MinValue : Int64.MaxValue;
			return r;
		}

		public static Int64 Sub(Int64 a, Int64 b)
		{
			Int64 r = unchecked(a - b);
			//異符号同士の減算で結果の符号がaと変わった時だけ溢れている
			if (Saturate && ((a ^ b) & (a ^ r)) < 0)
				return a < 0 ? Int64.MinValue : Int64.MaxValue;
			return r;
		}

		public static Int64 Mul(Int64 a, Int64 b)
		{
			if (!Saturate)
				return unchecked(a * b);
			try
			{
				return checked(a * b);
			}
			catch (OverflowException)
			{
				return ((a < 0) == (b < 0)) ? Int64.MaxValue : Int64.MinValue;
			}
		}

		public static Int64 Negate(Int64 a)
		{
			if (Saturate && a == Int64.MinValue)
				return Int64.MaxValue;
			return unchecked(-a);
		}
	}
}
