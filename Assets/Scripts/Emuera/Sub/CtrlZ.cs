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
	/// EEは設定で切り替えるが、こちらは画面のボタンから使うので常に記録する。
	///
	/// これだけだと基準点からの入力が多いほど遅くなるので、SHOPやTRAINのように関数を
	/// 何も実行していないシステムの入力待ちごとに、状態を丸ごと覚えておく(最新の2つ)。
	/// 取り消す時はそこへ即座に戻し、その後の数個の入力だけを流し直す
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
		/// <summary>流し直しの最中に、ここまでの入力を流し終えたという数</summary>
		public int ReplayCursor = 0;

		/// <summary>システムの入力待ちで覚えた状態</summary>
		internal sealed class Snapshot
		{
			/// <summary>この状態の後に来る最初の入力の番号(Inputsの添字)</summary>
			public int InputIndex;
			public GameData.Variable.VariableData.UndoVariables Variables;
			public Dictionary<string, Dictionary<long, object>> Dictionaries;
			public long[] RandomState;
			public GameView.EmueraConsole.UndoConsole Console;
			public GameProc.Process.UndoProcess Process;
		}
		//古い順。覚えるのは最新の2つまで(直前の入力待ちと、その1つ前)
		readonly List<Snapshot> snapshots = new List<Snapshot>(2);
		const int kMaxSnapshots = 2;

		/// <summary>システムの入力待ちに入った時に呼ばれる</summary>
		public void TakeSnapshot(GameProc.Process process, GameView.EmueraConsole console)
		{
			if (Kind == CheckpointKind.None || Config.UseNewRandom)
				return;
			var v = GlobalStatic.VEvaluator;
			var data = GlobalStatic.VariableData;
			if (v == null || data == null)
				return;
			int index = RewindInProgress ? ReplayCursor : Inputs.Count;
			//入れ替わりで捨てる物の配列を使い回す。入力が来ないまま待ち直した(無効な値など)
			//なら最新の物を、そうでなく一杯なら一番古い物を置き換える
			Snapshot reuse = null;
			if (snapshots.Count > 0 && snapshots[snapshots.Count - 1].InputIndex == index)
				reuse = snapshots[snapshots.Count - 1];
			else if (snapshots.Count >= kMaxSnapshots)
				reuse = snapshots[0];
			if (reuse != null)
				snapshots.Remove(reuse);
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var vars = data.CaptureUndo(reuse == null ? null : reuse.Variables);
			var snap = new Snapshot
			{
				InputIndex = index,
				Variables = vars,
				Dictionaries = GameData.Function.FunctionMethodCreator.CaptureErbDictionaries(),
				RandomState = new long[v.RANDDATA.Length],
				Console = console.CaptureUndo(),
				Process = process.CaptureUndo(),
			};
			v.GetRandState(snap.RandomState);
			snapshots.Add(snap);
			//入力待ちのたびに走るので、重い時は報告で分かるよう残す
			if (watch.ElapsedMilliseconds > 100)
				UnityEngine.Debug.LogWarning("CtrlZ: 되돌리기용 상태 저장에 " + watch.ElapsedMilliseconds + "ms 걸렸습니다 (배열 "
					+ vars.Live.Count + "개, 캐릭터 " + vars.Charas.Count + "명)");
		}

		/// <summary>inputIndex番の入力より前の状態のうち、一番新しい物</summary>
		public Snapshot FindSnapshot(int inputIndex)
		{
			for (int i = snapshots.Count - 1; i >= 0; i--)
				if (snapshots[i].InputIndex <= inputIndex)
					return snapshots[i];
			return null;
		}

		/// <summary>戻った先より後(未来)の状態を捨てる</summary>
		public void DropSnapshotsAfter(int inputIndex)
		{
			snapshots.RemoveAll(s => s.InputIndex > inputIndex);
		}

		public void ClearSnapshots()
		{
			snapshots.Clear();
		}

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
			snapshots.Clear();
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
