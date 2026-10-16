using System.Collections.Generic;

namespace MinorShift.Emuera.Sub
{
	/// <summary>
	/// EEのCtrl-Z(直前の入力を取り消す)。
	///
	/// 最後にセーブかロードをした所から後の入力を覚えておき、取り消す時は
	/// タイトルからそのセーブを読み直して、最後の1つを除いた入力をもう一度流す。
	/// 乱数はセーブ・ロードの時点の状態に戻すので、同じ結果になる。
	///
	/// EEは設定(Ctrl-Zで元に戻す機能を有効にする)で切り替えるが、
	/// こちらは画面のボタンから使うので常に記録する。入力の文字列を覚えるだけなので軽い
	/// </summary>
	internal sealed class CtrlZ
	{
		/// <summary>読み直すセーブの番号。まだセーブもロードもしていなければ -1</summary>
		public int LastSave = -1;
		//上書きの確認があるので、セーブが本当に済むまで番号を預かる
		int lastSaveExpected = -1;
		public readonly List<string> Inputs = new List<string>(64);
		public long[] RandomState = null;

		public volatile bool RewindInProgress = false;
		public volatile bool RepeatedUndoRequested = false;

		public bool CanUndo { get { return LastSave >= 0 && Inputs.Count > 0; } }

		public void Add(string s)
		{
			if (RewindInProgress)
				return;
			Inputs.Add(s);
		}

		/// <summary>セーブ先を選んだ時</summary>
		public void OnSavePrepare(int target)
		{
			if (RewindInProgress)
				return;
			lastSaveExpected = target;
		}

		/// <summary>
		/// セーブし終えた時。流し直しの途中のセーブは同じ物を書き直すだけなので、
		/// 記録を消さない(EEは消すので、そこで流し直しが止まってしまう)
		/// </summary>
		public void OnSave()
		{
			if (RewindInProgress)
				return;
			LastSave = lastSaveExpected;
			Inputs.Clear();
			saveRandom();
		}

		/// <summary>ロードするセーブを選んだ時(読む前)</summary>
		public void OnLoad(int saveFile)
		{
			if (RewindInProgress)
				return;
			LastSave = saveFile;
			Inputs.Clear();
			saveRandom();
		}

		void saveRandom()
		{
			var v = GlobalStatic.VEvaluator;
			if (v == null)
				return;
			int len = v.RANDDATA.Length;
			if (RandomState == null || RandomState.Length != len)
				RandomState = new long[len];
			v.GetRandState(RandomState);
		}
	}
}
