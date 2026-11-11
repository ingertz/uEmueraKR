using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MinorShift.Emuera.Sub
{
	internal sealed class EraStreamReader : IDisposable
	{
		public EraStreamReader(bool useRename)
		{
			this.useRename = useRename;
		}

		string filepath;
		string filename;
        readonly bool useRename = false;
		int curNo = 0;
		int nextNo = 0;
		//文字コードの判別のため一旦すべて読み込む。ERB/CSVは小さいので問題無い。
		//StreamReaderに独自Encodingを渡す形にすると、読み込みの区切りに
		//2バイト文字が跨った時の状態管理が要るため、こちらの方が安全
		TextReader reader;
		FileStream stream;

		public bool Open(string path)
		{
			return Open(path, Path.GetFileName(path));
		}

		public bool Open(string path, string name)
		{
			//そんなお行儀の悪いことはしていない
			//if (disposed)
			//    throw new ExeEE("破棄したオブジェクトを再利用しようとした");
			//if ((reader != null) || (stream != null) || (filepath != null))
			//    throw new ExeEE("使用中のオブジェクトを別用途に再利用しようとした");
			filepath = path;
			filename = name;
			nextNo = 0;
			curNo = 0;
			try
			{
				//先に読んでもらっている分があればそれを使う
				string prefetched;
				reader = uEmuera.FilePrefetch.TryTake(filepath, out prefetched)
					? new StringReader(prefetched)
					: uEmuera.TextFileReader.OpenText(filepath);
				if (reader == null)
				{
					this.Dispose();
					return false;
				}
			}
			catch
			{
				this.Dispose();
				return false;
			}
			return true;
		}

		/// <summary>読み込み済みの文字列から読む(遅延読み込みで同じファイルを何度も開かないため)</summary>
		public bool OpenText(string text, string name)
		{
			return OpenText(text, name, 0, 1);
		}

		/// <summary>start文字目(lineNo行目の頭)から読む。行番号はlineNoから数える</summary>
		public bool OpenText(string text, string name, int start, int lineNo)
		{
			filepath = name;
			filename = name;
			nextNo = lineNo - 1;
			curNo = lineNo - 1;
			reader = new OffsetStringReader(text ?? "", start);
			return true;
		}

		/// <summary>文字列の途中から読むTextReader。Substringで写さずに済ませる</summary>
		sealed class OffsetStringReader : TextReader
		{
			readonly string s;
			int pos;
			public int Position { get { return pos; } }
			public OffsetStringReader(string s, int start)
			{
				this.s = s;
				pos = Math.Min(Math.Max(0, start), s.Length);
			}
			public override int Peek() { return pos < s.Length ? s[pos] : -1; }
			public override int Read() { return pos < s.Length ? s[pos++] : -1; }
			public override string ReadLine()
			{
				if (pos >= s.Length)
					return null;
				int i = pos;
				while (i < s.Length && s[i] != '\r' && s[i] != '\n')
					i++;
				string line = s.Substring(pos, i - pos);
				if (i < s.Length && s[i] == '\r')
					i++;
				if (i < s.Length && s[i] == '\n')
					i++;
				pos = i;
				return line;
			}
		}

		public string ReadLine()
		{
			nextNo++;
			curNo = nextNo;
			return reader.ReadLine();
		}

		/// <summary>
		/// 次の有効な行を読む。LexicalAnalyzer経由でConfigを参照するのでConfig完成までつかわないこと。
		/// </summary>
		/// <summary>
		/// 直前にReadEnabledLineが返した行の頭が、文字列の何文字目だったか。
		/// OpenTextで開いた時だけ分かり、それ以外は-1
		/// </summary>
		public int LastLineStart { get; private set; } = -1;

		public StringStream ReadEnabledLine(bool disabled = false)
		{
			string line = null;
			StringStream st = new StringStream();
			curNo = nextNo;
			var offsetReader = reader as OffsetStringReader;
			while (true)
			{
				LastLineStart = offsetReader != null ? offsetReader.Position : -1;
				line = reader.ReadLine();
				curNo++;
				nextNo++;
				if (line == null)
					return null;
				if (line.Length == 0)
					continue;

				if (useRename && (line.IndexOf("[[") >= 0) && (line.IndexOf("]]") >= 0))
				{
					foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
						line = line.Replace(pair.Key, pair.Value);
				}
                st.Set(line);
				LexicalAnalyzer.SkipWhiteSpace(st);
				if (st.EOS)
					continue;
				//[SKIPSTART]～[SKIPEND]中にここが誤爆するので無効化
				if (!disabled)
				{
					if (st.Current == '}')
						throw new CodeEE("予期しない行連結終端記号'}'が見つかりました", new ScriptPosition(filename, curNo));
					if (st.Current == '{')
					{
						if (line.Trim() != "{")
							throw new CodeEE("行連結始端記号'{'の行に'{'以外の文字を含めることはできません", new ScriptPosition(filename, curNo));
						break;
					}
				}
				return st;
			}
			//curNoはこの後加算しない(始端記号の行を行番号とする)
			StringBuilder b = new StringBuilder();
			while (true)
			{
				line = reader.ReadLine();
				nextNo++;
				if (line == null)
				{
					throw new CodeEE("行連結始端記号'{'が使われましたが終端記号'}'が見つかりません", new ScriptPosition(filename, curNo));
				}

				if (useRename && (line.IndexOf("[[") >= 0) && (line.IndexOf("]]") >= 0))
				{
					foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
						line = line.Replace(pair.Key, pair.Value);
				}
				string test = line.TrimStart();
				if (test.Length > 0)
				{
					if (test[0] == '}')
					{
						if (test.Trim() != "}")
							throw new CodeEE("行連結終端記号'}'の行に'}'以外の文字を含めることはできません", new ScriptPosition(filename, nextNo));
						break;
					}
                    //行連結文字なら1字でないとおかしい、というか、こうしないとFORMの数値変数処理が誤爆する。
                    //{
                    //A}
                    //みたいなどうしようもないコードは知ったこっちゃない
					if (test[0] == '{' && test.Length == 1)
						throw new CodeEE("予期しない行連結始端記号'{'が見つかりました", new ScriptPosition(filename, nextNo));
				}
				b.Append(line);
				b.Append(" ");
			}
			st.Set(b.ToString());
			LexicalAnalyzer.SkipWhiteSpace(st);
			return st;
		}

		/// <summary>
		/// 直前に読んだ行の行番号
		/// </summary>
		public int LineNo
		{ get { return curNo; } }
		public string Filename
		{
			get
			{
				return filename;
			}
		}
		//public string Filepath
		//{
		//    get
		//    {
		//        return filepath;
		//    }
		//}

		public void Close() { this.Dispose(); }
		bool disposed = false;
		#region IDisposable メンバ

		public void Dispose()
		{
			if (disposed)
				return;
			if (reader != null)
				reader.Close();
			else if (stream != null)
				stream.Close();
			filepath = null;
			filename = null;
			reader = null;
			stream = null;
			disposed = true;
		}

		#endregion
	}
}
