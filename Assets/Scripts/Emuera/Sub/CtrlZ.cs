using System.Collections.Generic;
using System.IO;

namespace MinorShift.Emuera.Sub
{
	/// <summary>
	/// EEのCtrl-Z(直前の入力を取り消す)。
	///
	/// 基準点から後の入力を覚えておき、取り消す時は基準点まで戻して、
	/// 最後の1つを除いた入力をもう一度流す。乱数は基準点の状態に戻すので同じ結果になる。
	///
	/// EEの基準点はシステムのセーブ・ロード画面だけだが、多くのゲームは自前のメニューから
	/// LOADDATAで読むので、それでは一度も基準点ができない。こちらでは
	///  ・システムのロード、LOADDATA(読む前のセーブファイルをそのまま覚える)
	///  ・システムのセーブ(書いた後のファイルを覚える。EEと同じ)
	///  ・タイトル画面の始まり(まだ何も読んでいない時のため)
	/// を基準点にする。SAVEDATAは、書いた後もスクリプトの途中から続くので基準点にしない。
	/// セーブファイルは中身を覚えておくので、後で同じ番号に上書きされても戻れる。
	///
	/// EEは設定で切り替えるが、こちらは画面のボタンから使うので常に記録する
	/// </summary>
	internal sealed class CtrlZ
	{
		public enum CheckpointKind { None, Title, Save }

		public CheckpointKind Kind = CheckpointKind.None;
		/// <summary>基準点のセーブの番号(LastLoadNoに入れる)</summary>
		public int SaveNo = -1;
		/// <summary>基準点のセーブファイルの中身</summary>
		public byte[] SaveData = null;
		public long[] RandomState = null;
		public readonly List<string> Inputs = new List<string>(64);
		//上書きの確認があるので、セーブが本当に済むまで番号を預かる
		int saveExpected = -1;

		public volatile bool RewindInProgress = false;
		public volatile bool RepeatedUndoRequested = false;

		public bool CanUndo { get { return Kind != CheckpointKind.None && Inputs.Count > 0; } }

		public void Add(string s)
		{
			if (RewindInProgress)
				return;
			Inputs.Add(s);
		}

		/// <summary>タイトル画面を始める時</summary>
		public void OnTitle()
		{
			if (RewindInProgress)
				return;
			setCheckpoint(CheckpointKind.Title, -1, null);
		}

		/// <summary>セーブ先を選んだ時</summary>
		public void OnSavePrepare(int target)
		{
			if (RewindInProgress)
				return;
			saveExpected = target;
		}

		/// <summary>
		/// システムのセーブを書き終えた時。流し直しの途中のセーブは同じ物を書き直すだけなので、
		/// 記録を消さない(EEは消すので、そこで流し直しが止まってしまう)
		/// </summary>
		public void OnSave()
		{
			if (RewindInProgress || saveExpected < 0)
				return;
			setCheckpoint(CheckpointKind.Save, saveExpected, readSave(saveExpected));
		}

		/// <summary>システムのロードかLOADDATAで、セーブを読む直前</summary>
		public void OnLoad(int saveNo)
		{
			if (RewindInProgress)
				return;
			setCheckpoint(CheckpointKind.Save, saveNo, readSave(saveNo));
		}

		void setCheckpoint(CheckpointKind kind, int saveNo, byte[] data)
		{
			Inputs.Clear();
			if (kind == CheckpointKind.Save && data == null)
			{
				//読めなかったら戻りようがない
				Kind = CheckpointKind.None;
				return;
			}
			Kind = kind;
			SaveNo = saveNo;
			SaveData = data;
			var v = GlobalStatic.VEvaluator;
			if (v == null)
			{
				Kind = CheckpointKind.None;
				return;
			}
			int len = v.RANDDATA.Length;
			if (RandomState == null || RandomState.Length != len)
				RandomState = new long[len];
			v.GetRandState(RandomState);
		}

		static byte[] readSave(int saveNo)
		{
			try
			{
				var path = GlobalStatic.VEvaluator.SaveDataPath(saveNo);
				return File.Exists(path) ? File.ReadAllBytes(path) : null;
			}
			catch
			{
				return null;
			}
		}
	}
}
