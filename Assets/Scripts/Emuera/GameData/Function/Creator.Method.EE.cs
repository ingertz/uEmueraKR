using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc;
using System.Linq;
using System.IO;
namespace MinorShift.Emuera.GameData.Function
{
    internal static class Utils {
        public static string GetValidPath(string p) { return p; }
        public static string GetRelativePath(string relativeTo, string path) { return path.Replace(relativeTo, "").TrimStart('\\', '/'); }
        public static class DataTable {
            private static readonly Type[] builtInDTTypes = new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(string) };
            private static readonly Dictionary<Type, string> builtInDictDTTypeNames = new Dictionary<Type, string>()
            {
                { typeof(sbyte), "int8" },
                { typeof(short), "int16" },
                { typeof(int), "int32" },
                { typeof(long), "int64" },
                { typeof(string), "string" },
            };
            private static readonly Dictionary<string, Type> builtInDictDTTypeNames_R = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "int8", typeof(sbyte) },
                { "int16", typeof(short) },
                { "int32", typeof(int) },
                { "int64", typeof(long) },
                { "string", typeof(string) },
            };
            public static long TypeToInt(Type t)
            {
                if (t == typeof(sbyte)) return 1;
                if (t == typeof(short)) return 2;
                if (t == typeof(int)) return 3;
                if (t == typeof(long)) return 4;
                if (t == typeof(string)) return 5;
                return long.MaxValue;
            }
            public static Type IntToType(long i)
            {
                if (i > 0 && i <= builtInDTTypes.Length) return builtInDTTypes[i - 1];
                return null;
            }
            public static Type NameToType(string n)
            {
                if (n == null) return null;
                if (builtInDictDTTypeNames_R.TryGetValue(n, out Type t)) return t;
                return null;
            }
            public static string TypeToName(Type t)
            {
                if (t == null) return null;
                if (builtInDictDTTypeNames.TryGetValue(t, out string n)) return n;
                return null;
            }
            public static object ConvertInt(long v, Type t)
            {
                if (t == typeof(sbyte)) return (sbyte)Math.Min(Math.Max(v, sbyte.MinValue), sbyte.MaxValue);
                if (t == typeof(short)) return (short)Math.Min(Math.Max(v, short.MinValue), short.MaxValue);
                if (t == typeof(int)) return (int)Math.Min(Math.Max(v, int.MinValue), int.MaxValue);
                return v;
            }
            public static object ConvertInt(string s, Type t) { return s; }
        }
    }
    internal static class RegexFactory {
        public static System.Text.RegularExpressions.Regex GetRegex(string s) { return new System.Text.RegularExpressions.Regex(s); }
    }
	internal static partial class FunctionMethodCreator
	{
		private sealed class DummyGraphicsMethod : FunctionMethod
		{
			public DummyGraphicsMethod()
			{
				ReturnType = typeof(long);
				argumentTypeArrayEx = new ArgTypeList[] {
					new ArgTypeList { ArgTypesEnum = new List<ArgType> { ArgType.Any | ArgType.Variadic }, OmitStart = 0 }
				};
				CanRestructure = false;
			}
			public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return 0;
			}
		}

		private sealed class HtmlStringLenMethod : FunctionMethod
			{
				public HtmlStringLenMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int }, OmitStart = 1 }
						};
					CanRestructure = true;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					int len = MinorShift.Emuera.GameView.HtmlManager.HtmlLength(arguments[0].GetStrValue(exm));
					if (arguments.Length == 1 || arguments[1].GetIntValue(exm) == 0)
					{
						if (len >= 0)
							return 2 * len / Config.FontSize + ((2 * len % Config.FontSize != 0) ? 1 : 0);
						else
							return 2 * len / Config.FontSize - ((2 * len % Config.FontSize != 0) ? 1 : 0);
					}
					return len;
				}
			}

		private sealed class XmlGetMethod : FunctionMethod
			{
				public XmlGetMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 2 },
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any, ArgType.String, ArgType.RefString1D, ArgType.Int }, OmitStart = 3 },
						};
					CanRestructure = false;
				}
				public XmlGetMethod(bool byname) : this()
				{
					byName = byname;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 2 },
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any, ArgType.String, ArgType.RefString1D, ArgType.Int }, OmitStart = 3 },
						};
				}
				private bool byName;
				private static void OutPutNode(XmlNode node, string[] array, int i, long style)
				{
					switch (style)
					{
						case 1: array[i] = node.InnerText; break;
						case 2: array[i] = node.InnerXml; break;
						case 3: array[i] = node.OuterXml; break;
						case 4: array[i] = node.Name; break;
						default: array[i] = node.Value; break;
					}
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					XmlDocument doc = null;
					XmlNodeList nodes = null;
					if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
					{
						var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
						var dict = exm.VEvaluator.VariableData.DataXmlDocument;
						if (dict.ContainsKey(idx)) doc = dict[idx];
						else return -1;
					}
					else
					{
						doc = new XmlDocument();
						var xml = arguments[0].GetStrValue(exm);
						try
						{
							doc.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlGetError", xml, e.Message));
						}
					}
					string path = arguments[1].GetStrValue(exm);
					try
					{
						nodes = doc.SelectNodes(path);
					}
					catch (System.Xml.XPath.XPathException e)
					{
						throw new CodeEE(string.Format("{0}: XmlGetPathError", path, e.Message));
					}
					long outputStyle = arguments.Length == 4 ? arguments[3].GetIntValue(exm) : 0;
		
					if (arguments.Length >= 3)
					{
						if (arguments[2].GetOperandType() == typeof(long) && arguments[2].GetIntValue(exm) != 0)
						{
							for (int i = 0; i < Math.Min(nodes.Count, exm.VEvaluator.RESULTS_ARRAY.Length); i++)
								OutPutNode(nodes[i], exm.VEvaluator.RESULTS_ARRAY, i, outputStyle);
						}
						else
						{
							var arr = (arguments[2] as VariableTerm).Identifier.GetArray() as string[];
							for (int i = 0; i < Math.Min(nodes.Count, arr.Length); i++)
								OutPutNode(nodes[i], arr, i, outputStyle);
						}
					}
					return nodes.Count;
				}
			}

		private sealed class IsDefinedMethod : FunctionMethod
			{
				public IsDefinedMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = true;
				}
		
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					return (GlobalStatic.IdentifierDictionary.GetMacro(arguments[0].GetStrValue(exm)) != null) ? 1 : 0;
				}
			}

		private sealed class EnumNameMethod : FunctionMethod
			{
				public enum EType
				{
					Function,
					Variable,
					Macro
				}
				public enum EAction
				{
					BeginsWith,
					EndsWith,
					With
				}
				private EType type;
				private EAction action;
				public EnumNameMethod(EType type, EAction act)
				{
					ReturnType = typeof(long);
					CanRestructure = false;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefString1D  }, OmitStart = 1 },
						};
					this.type = type;
					action = act;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string arg = arguments[0].GetStrValue(exm).ToUpper();
					string[] array = null;
					switch (type)
					{
						case EType.Function:
							array = GlobalStatic.LabelDictionary.NoneventKeys;
							break;
						case EType.Variable:
							array = GlobalStatic.IdentifierDictionary.VarKeys;
							break;
						case EType.Macro:
							array = GlobalStatic.IdentifierDictionary.MacroKeys;
							break;
					}
					List<string> strs = new List<string>();
					if (arg.Length > 0)
						foreach (string item in array)
						{
							if (item.Length < arg.Length) continue;
							switch (action)
							{
								case EAction.BeginsWith:
									if (item.ToUpper().IndexOf(arg, StringComparison.Ordinal) == 0) strs.Add(item);
									break;
								case EAction.EndsWith:
									if (item.ToUpper().LastIndexOf(arg, StringComparison.Ordinal) == item.Length - arg.Length) strs.Add(item);
									break;
								case EAction.With:
									if (item.ToUpper().IndexOf(arg, StringComparison.Ordinal) >= 0) strs.Add(item);
									break;
							}
						}
					// strs.Sort();
					string[] output;
					if (arguments.Length == 2)
						output = (arguments[1] as VariableTerm).Identifier.GetArray() as string[];
					else
						output = exm.VEvaluator.RESULTS_ARRAY;
					string[] ret = strs.ToArray();
					int outputlength = Math.Min(output.Length, ret.Length);
					Array.Copy(ret, output, outputlength);
					return outputlength;
				}
			}

		private sealed class EnumFilesMethod : FunctionMethod
			{
				public EnumFilesMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.Int, ArgType.RefString1D  }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var dir = Utils.GetValidPath(arguments[0].GetStrValue(exm));
					if (!Path.IsPathRooted(dir))
						dir = Path.Combine(Program.ExeDir, dir);
					dir = uEmuera.Utils.ResolvePath(dir);
					if (dir == null || !Directory.Exists(dir)) return -1;
					var pattern = arguments.Length > 1 ? arguments[1].GetStrValue(exm) : "*";
					var option = arguments.Length > 2
						? (arguments[2].GetIntValue(exm) == 0 ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories)
						: SearchOption.TopDirectoryOnly;
					//ERB側は繰り返しの中からここを何百回も呼ぶ。
					//その度にフォルダを数え直すと、それだけで数分かかる事がある
					var all = uEmuera.Utils.GetDirFiles(dir, option == SearchOption.AllDirectories);
					if (all == null) return -1;
					string[] files;
					try
					{
						var hit = new List<string>();
						for (int i = 0; i < all.Length; i++)
						{
							if (uEmuera.Utils.MatchWildcard(Path.GetFileName(all[i]), pattern))
								hit.Add(all[i]);
						}
						files = hit.ToArray();
						for (int i = 0; i < files.Length; i++)
						{
							//区切りは円記号に揃える。本体はWindowsなので結果も円記号区切りで、
							//ERB側は「SPLIT RESULTS, "\"」でファイル名を取り出すのが定番。
							//Androidの区切りは斜線なので、そのまま返すとSPLITが何も切れず、
							//パス全体がファイル名として使われて画像が出なくなる
							files[i] = Utils.GetRelativePath(Program.ExeDir, files[i]).Replace('/', '\\');
						}

					}
					catch
					{
						return -1;
					}
					string[] output;
					if (arguments.Length == 4)
						output = (arguments[3] as VariableTerm).Identifier.GetArray() as string[];
					else
						output = exm.VEvaluator.RESULTS_ARRAY;
					var ret = Math.Min(files.Length, output.Length);
					Array.Copy(files, output, ret);
					return ret;
				}
			}

		private sealed class GetVarMethod : FunctionMethod
			{
				public GetVarMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
				}
		
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					WordCollection wc = LexicalAnalyzer.Analyse(new StringStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
					IOperandTerm term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
		
					if (term is VariableTerm)
					{
						VariableTerm var = (VariableTerm)term;
		
						if (var.Identifier == null)
							throw new CodeEE(string.Format("{0}: IsNotVar", name));
						if (!var.IsInteger)
							throw new CodeEE(string.Format("{0}: IsNotInt", name));
						return var.GetIntValue(exm);
					}
					else
						throw new CodeEE(string.Format("{0}: IsNotVar", name));
				}
			}

		private sealed class GetVarsMethod : FunctionMethod
			{
				public GetVarsMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					WordCollection wc = LexicalAnalyzer.Analyse(new StringStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
					IOperandTerm term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
		
					if (term is VariableTerm)
					{
						VariableTerm var = (VariableTerm)term;
		
						if (var.Identifier == null)
							throw new CodeEE(string.Format("{0}: IsNotVar", name));
						if (!var.IsString)
							throw new CodeEE(string.Format("{0}: IsNotStr", name));
						return var.GetStrValue(exm);
					}
					else
						throw new CodeEE(string.Format("{0}: IsNotVar", name));
				}
			}

		private sealed class ExistVarMethod : FunctionMethod
			{
				public ExistVarMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = true;
				}
		
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					VariableToken token = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
					if (token != null)
					{
						long res = 0;
						if (token.IsInteger) res |= 1;
						if (token.IsString) res |= 2;
						if (token.IsConst) res |= 4;
						if (token.IsArray2D) res |= 8;
						if (token.IsArray3D) res |= 16;
						return res;
					}
					return 0;
				}
			}

		private sealed class ArrayMultiSortExMethod : FunctionMethod
			{
				public ArrayMultiSortExMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefString1D, ArgType.Int, ArgType.Int | ArgType.DisallowVoid  }, OmitStart = 2 },
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefInt1D, ArgType.RefString1D, ArgType.Int, ArgType.Int | ArgType.DisallowVoid  }, OmitStart = 2 },
						};
					CanRestructure = false;
				}
				private string CheckVariableTerm(IOperandTerm arg, string v)
				{
					var vname = v == null ? "{0}: FirstArg" : v;
					if (!(arg is VariableTerm varTerm) || varTerm.Identifier.IsCalc || varTerm.Identifier.IsConst)
						return string.Format("{0}: NotVarFunc", Name, vname);
					if (v == null && !varTerm.Identifier.IsArray1D)
						return string.Format("{0}: Not1DFuncArg", Name, "1");
					if (varTerm.Identifier.IsCharacterData)
						return string.Format("{0}: IsCharaVarFunc", Name, vname);
					if (!varTerm.Identifier.IsArray1D && !varTerm.Identifier.IsArray2D && !varTerm.Identifier.IsArray3D)
						return string.Format("{0}: NotDimVarFunc", Name, vname);
					return null;
				}
				private VariableTerm GetConvertedTerm(ExpressionMediator exm, string name)
				{
					WordCollection wc = LexicalAnalyzer.Analyse(new StringStream(name), LexEndWith.EoL, LexAnalyzeFlag.None);
					var term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
					var err = CheckVariableTerm(term, name);
					if (err != null)
						throw new CodeEE(err);
					return term as VariableTerm;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					bool isAscending = arguments.Length < 3 || arguments[2] == null || arguments[2].GetIntValue(exm) != 0;
					long fixedLength = arguments.Length < 4 ? -1 : arguments[3].GetIntValue(exm);
					if (fixedLength == 0) return 0;
					VariableTerm varTerm = arguments[0] is VariableTerm ? arguments[0] as VariableTerm : GetConvertedTerm(exm, arguments[0].GetStrValue(exm));
					int[] sortedArray;
					if (varTerm.Identifier.IsInteger)
					{
						List<KeyValuePair<long, int>> sortList = new List<KeyValuePair<long, int>>();
						long[] array = (long[])varTerm.Identifier.GetArray();
						var length = fixedLength > 0 ? Math.Min(fixedLength, array.Length) : array.Length;
						for (int i = 0; i < length; i++)
						{
							if (fixedLength == -1 && array[i] == 0)
								break;
							if (array[i] < long.MinValue || array[i] > long.MaxValue)
								return 0;
							sortList.Add(new KeyValuePair<long, int>(array[i], i));
						}
						//素ではintの範囲しか扱えないので一工夫
						sortList.Sort((a, b) => { return (isAscending ? 1 : -1) * Math.Sign(a.Key - b.Key); });
						sortedArray = sortList.Select(p => p.Value).ToArray();
					}
					else
					{
						List<KeyValuePair<string, int>> sortList = new List<KeyValuePair<string, int>>();
						string[] array = (string[])varTerm.Identifier.GetArray();
						var length = fixedLength > 0 ? Math.Min(fixedLength, array.Length) : array.Length;
						for (int i = 0; i < length; i++)
						{
							if (fixedLength == -1 && string.IsNullOrEmpty(array[i]))
								return 0;
							sortList.Add(new KeyValuePair<string, int>(array[i], i));
						}
						sortList.Sort((a, b) => { return (isAscending ? 1 : -1) * a.Key.CompareTo(b.Key); });
						sortedArray = sortList.Select(p => p.Value).ToArray();
					}
					List<VariableTerm> varTerms = new List<VariableTerm>();
					foreach (var nTerm in (string[])(arguments[1] as VariableTerm).Identifier.GetArray())
						varTerms.Add(GetConvertedTerm(exm, nTerm));
					foreach (var term in varTerms)
					{
						if (term.Identifier.IsArray1D)
						{
							if (term.IsInteger)
							{
								var array = (long[])term.Identifier.GetArray();
								var clone = (long[])array.Clone();
								if (array.Length < sortedArray.Length)
									return 0;
								for (int i = 0; i < sortedArray.Length; i++)
									array[i] = clone[sortedArray[i]];
							}
							else
							{
								var array = (string[])term.Identifier.GetArray();
								var clone = (string[])array.Clone();
								if (array.Length < sortedArray.Length)
									return 0;
								for (int i = 0; i < sortedArray.Length; i++)
									array[i] = clone[sortedArray[i]];
							}
						}
						else if (term.Identifier.IsArray2D)
						{
							if (term.IsInteger)
							{
								var array = (long[,])term.Identifier.GetArray();
								var clone = (long[,])array.Clone();
								if (array.GetLength(0) < sortedArray.Length)
									return 0;
								for (int i = 0; i < sortedArray.Length; i++)
									for (int x = 0; x < array.GetLength(1); x++)
										array[i, x] = clone[sortedArray[i], x];
							}
							else
							{
								var array = (string[,])term.Identifier.GetArray();
								var clone = (string[,])array.Clone();
								if (array.GetLength(0) < sortedArray.Length)
									return 0;
								for (int i = 0; i < sortedArray.Length; i++)
									for (int x = 0; x < array.GetLength(1); x++)
										array[i, x] = clone[sortedArray[i], x];
							}
						}
						else if (term.Identifier.IsArray3D)
						{
							if (term.IsInteger)
							{
								var array = (long[,,])term.Identifier.GetArray();
								var clone = (long[,,])array.Clone();
								if (array.GetLength(0) < sortedArray.Length)
									return 0;
								for (int i = 0; i < sortedArray.Length; i++)
									for (int x = 0; x < array.GetLength(1); x++)
										for (int y = 0; y < array.GetLength(2); y++)
											array[i, x, y] = clone[sortedArray[i], x, y];
							}
							else
							{
								var array = (string[,,])term.Identifier.GetArray();
								var clone = (string[,,])array.Clone();
								if (array.GetLength(0) < sortedArray.Length)
									return 0;
								for (int i = 0; i < sortedArray.Length; i++)
									for (int x = 0; x < array.GetLength(1); x++)
										for (int y = 0; y < array.GetLength(2); y++)
											array[i, x, y] = clone[sortedArray[i], x, y];
							}
						}
						else { throw new ExeEE("{0}: AbnormalArray"); }
					}
					return 1;
				}
				public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					for (int i = 0; i < arguments.Length; i++)
						arguments[i] = arguments[i].Restructure(exm);
					return false;
				}
			}

		private sealed class SetVarMethod : FunctionMethod
			{
				public SetVarMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Any  } },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					WordCollection wc = LexicalAnalyzer.Analyse(new StringStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
					IOperandTerm term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
		
					if (term is VariableTerm var)
					{
						if (var.Identifier == null || var.Identifier.IsConst)
							throw new CodeEE(string.Format("{0}: IsNotVar", name));
						if (var.IsString)
						{
							if (arguments[1].GetOperandType() != typeof(string))
								throw new CodeEE(string.Format("{0}: IsNotInt", name));
							var.SetValue(arguments[1].GetStrValue(exm), exm);
						}
						else
						{
							if (arguments[1].GetOperandType() != typeof(long))
								throw new CodeEE(string.Format("{0}: IsNotStr", name));
							var.SetValue(arguments[1].GetIntValue(exm), exm);
						}
						return 1;
					}
					else
						throw new CodeEE(string.Format("{0}: IsNotVar", name));
				}
			}

		private sealed class VarSetExMethod : FunctionMethod
			{
				public VarSetExMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Any, ArgType.Int, ArgType.Int, ArgType.Int  }, OmitStart = 2 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					WordCollection wc = LexicalAnalyzer.Analyse(new StringStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
					IOperandTerm term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
		
					if (term is VariableTerm var)
					{
						if (var.Identifier == null || var.Identifier.IsConst)
							throw new CodeEE(string.Format("{0}: IsNotVar", name));
		
						int start = (int)(arguments.Length >= 4 ? arguments[3].GetIntValue(exm) : 0);
						int end = (int)(arguments.Length == 5 ? arguments[4].GetIntValue(exm)
							: (var.Identifier.IsArray1D ? var.Identifier.GetLength()
							: (var.Identifier.IsArray2D ? var.Identifier.GetLength(1)
							: (var.Identifier.IsArray2D ? var.Identifier.GetLength(2) : 0))));
						bool setAllDims = arguments.Length >= 3 ? arguments[2].GetIntValue(exm) != 0 : true;
						if (var.IsString)
						{
							var val = string.Empty;
							if (arguments.Length > 1 && arguments[1].GetOperandType() != typeof(string))
								throw new CodeEE(string.Format("{0}: SetStrToInt", name));
							if (arguments.Length > 1)
								val = arguments[1].GetStrValue(exm);
							if (var.Identifier.IsArray1D)
								var.Identifier.SetValueAll(val, start, end, 0);
							else if (var.Identifier.IsArray2D)
							{
								var array = var.Identifier.GetArray() as string[,];
								var idx1 = var.GetElementInt(0, exm);
								var idx2 = var.GetElementInt(1, exm);
								for (int i = Math.Max(start, (int)idx2); i < end; i++)
									array[idx1, i] = val;
							}
							if (var.Identifier.IsArray3D)
							{
								var idx1 = var.GetElementInt(0, exm);
								var idx2 = var.GetElementInt(1, exm);
								var idx3 = var.GetElementInt(2, exm);
								var array = var.Identifier.GetArray() as string[,,];
								for (int i = Math.Max(start, (int)idx3); i < end; i++)
									array[idx2, idx1, i] = val;
							}
						}
						else
						{
							long val = 0;
							if (arguments.Length > 1 && arguments[1].GetOperandType() != typeof(long))
								throw new CodeEE(string.Format("{0}: SetIntToStr", name));
							if (arguments.Length > 1)
								val = arguments[1].GetIntValue(exm);
							if (var.Identifier.IsArray1D)
								var.Identifier.SetValueAll(val, start, end, 0);
							else if (var.Identifier.IsArray2D)
							{
								var array = var.Identifier.GetArray() as long[,];
								var idx1 = var.GetElementInt(0, exm);
								var idx2 = var.GetElementInt(1, exm);
								if (setAllDims)
								{
									for (int j = 0; j < array.GetLength(0); j++)
										for (int i = Math.Max(start, (int)idx2); i < end; i++)
											array[j, i] = val;
								}
								else
								{
									for (int i = Math.Max(start, (int)idx2); i < end; i++)
										array[idx1, i] = val;
								}
							}
							if (var.Identifier.IsArray3D)
							{
								var idx1 = var.GetElementInt(0, exm);
								var idx2 = var.GetElementInt(1, exm);
								var idx3 = var.GetElementInt(2, exm);
								var array = var.Identifier.GetArray() as long[,,];
								if (setAllDims)
								{
									for (int k = 0; k < array.GetLength(0); k++)
										for (int j = 0; j < array.GetLength(1); j++)
											for (int i = Math.Max(start, (int)idx3); i < end; i++)
												array[k, j, i] = val;
								}
								else
								{
									for (int i = Math.Max(start, (int)idx3); i < end; i++)
										array[idx2, idx1, i] = val;
								}
							}
						}
						return 1;
					}
					else
						throw new CodeEE(string.Format("{0}: IsNotVar", name));
				}
			}

		private sealed class HtmlSubStringMethod : FunctionMethod
			{
				public HtmlSubStringMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArray = new Type[] { typeof(string), typeof(long) };
					CanRestructure = false;
				}
		
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string str = arguments[0].GetStrValue(exm);
					string[] strs = MinorShift.Emuera.GameView.HtmlManager.HtmlSubString(str, (int)arguments[1].GetIntValue(exm));
					string[] output = GlobalStatic.Process.VEvaluator.RESULTS_ARRAY;
					int outputlength = Math.Min(output.Length, strs.Length);
					Array.Copy(strs, output, outputlength);
					return output[0];
				}
			}

		private sealed class HtmlStringLinesMethod : FunctionMethod
			{
				public HtmlStringLinesMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string), typeof(long) };
					CanRestructure = false;
				}
		
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string str = arguments[0].GetStrValue(exm);
					if (string.IsNullOrEmpty(str)) return 0;
					var ret = 0;
					do
					{
						string[] strs = MinorShift.Emuera.GameView.HtmlManager.HtmlSubString(str, (int)arguments[1].GetIntValue(exm));
						str = strs[1];
						ret++;
					} while (!string.IsNullOrEmpty(str));
					return ret;
				}
			}

		private sealed class RegexpMatchMethod : FunctionMethod
			{
				public RegexpMatchMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.Int  }, OmitStart = 2 },
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.RefInt, ArgType.RefString1D  } },
						};
					CanRestructure = false;
				}
		
				static void Output(MatchCollection matches, Regex reg, string[] values)
				{
					var idx = 0;
					foreach (Match match in matches)
						foreach (var name in reg.GetGroupNames())
						{
							if (idx >= values.Length) return;
							values[idx] = match.Groups[name].Value;
							idx++;
						}
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string baseString = arguments[0].GetStrValue(exm);
					Regex reg;
					try
					{
						reg = RegexFactory.GetRegex(arguments[1].GetStrValue(exm));
					}
					catch (ArgumentException e)
					{
						throw new CodeEE(string.Format("{0}: InvalidRegexArg", Name, 2, e.Message));
					}
					var matches = reg.Matches(baseString);
					var ret = matches.Count;
					if (arguments.Length == 3 && arguments[2].GetIntValue(exm) != 0)
					{
						exm.VEvaluator.RESULT_ARRAY[1] = reg.GetGroupNumbers().Length;
						if (ret > 0) Output(matches, reg, exm.VEvaluator.RESULTS_ARRAY);
					}
					if (arguments.Length == 4)
					{
						(arguments[2] as VariableTerm).SetValue(reg.GetGroupNumbers().Length, exm);
						if (ret > 0) Output(matches, reg, (arguments[3] as VariableTerm).Identifier.GetArray() as string[]);
					}
					return ret;
				}
			}

		private sealed class XmlDocumentMethod : FunctionMethod
			{
				public enum Operation { Create, Check, Release };
				public XmlDocumentMethod(Operation type)
				{
					op = type;
					ReturnType = typeof(long);
					if (op == Operation.Create)
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any, ArgType.String  } },
							};
					else
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any  } },
							};
					CanRestructure = false;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
					var xmlDict = exm.VEvaluator.VariableData.DataXmlDocument;
					if (op == Operation.Create)
					{
						string xml = arguments[1].GetStrValue(exm);
						if (xmlDict.ContainsKey(idx))
						{
							return 0;
						}
						XmlDocument doc = new XmlDocument();
						try
						{
							doc.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlGetError", xml, e.Message));
						}
						xmlDict.Add(idx, doc);
					}
					else
					{
						if (xmlDict.ContainsKey(idx))
						{
							if (op == Operation.Check) return 1;
							xmlDict.Remove(idx);
						}
						else return 0;
					}
					return 1;
				}
			}

		private sealed class XmlSetMethod : FunctionMethod
			{
				public XmlSetMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefString, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
						};
					CanRestructure = false;
				}
				public XmlSetMethod(bool byname) : this()
				{
					byName = byname;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
						};
				}
				private bool byName;
				private static void SetNode(XmlNode node, string val, long style)
				{
					switch (style)
					{
						case 1: node.InnerText = val; break;
						case 2: node.InnerXml = val; break;
						default: node.Value = val; break;
					}
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					XmlDocument doc;
					bool saveToArg0 = true;
					if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
					{
						saveToArg0 = false;
						var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
						var dict = exm.VEvaluator.VariableData.DataXmlDocument;
						if (dict.ContainsKey(idx)) doc = dict[idx];
						else return -1;
					}
					else
					{
						string xml = arguments[0].GetStrValue(exm);
						doc = new XmlDocument();
						try
						{
							doc.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
						}
					}
		
					string path = arguments[1].GetStrValue(exm);
					XmlNodeList nodes = null;
					try
					{
						nodes = doc.SelectNodes(path);
					}
					catch (System.Xml.XPath.XPathException e)
					{
						throw new CodeEE(string.Format("{0}: XmlXPathParseError", Name, path, e.Message));
					}
					bool setAllNodes = arguments.Length >= 4 ? arguments[3].GetIntValue(exm) != 0 : false;
					var style = arguments.Length == 5 ? arguments[4].GetIntValue(exm) : 0;
					if (style > 2 || style < 0) style = 0;
					var val = arguments[2].GetStrValue(exm);
					if (nodes.Count > 0)
					{
						if (nodes.Count != 1)
						{
							if (setAllNodes)
								for (int i = 0; i < nodes.Count; i++) SetNode(nodes[i], val, style);
						}
						else SetNode(nodes[0], val, style);
						if (saveToArg0)
						{
							(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
						}
					}
					return nodes.Count;
				}
			}

		private sealed class XmlToStrMethod : FunctionMethod
			{
				public XmlToStrMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any  } },
						};
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
					var xmlDict = exm.VEvaluator.VariableData.DataXmlDocument;
					if (!xmlDict.ContainsKey(idx)) return string.Empty;
					return xmlDict[idx].OuterXml;
				}
			}

		private sealed class XmlAddNodeMethod : FunctionMethod
			{
				public enum Operation { Node, Attribute };
				public XmlAddNodeMethod(Operation op)
				{
					ReturnType = typeof(long);
					if (op == Operation.Node)
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefString, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 }
							};
					else
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int, ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefString, ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 }
							};
					CanRestructure = false;
					this.op = op;
				}
				public XmlAddNodeMethod(Operation op, bool byname) : this(op)
				{
					byName = byname;
					if (op == Operation.Node)
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
							};
					else
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int  }, OmitStart = 3 },
							};
				}
				private bool byName;
				Operation op;
				bool Insert(XmlNode node, XmlNode child, int method)
				{
					if (op == Operation.Node)
					{
						switch (method)
						{
							case 0: node.AppendChild(child); break;
							case 1:
								if (node.ParentNode == null) return false;
								node.ParentNode.InsertBefore(child, node);
								break;
							case 2:
								if (node.ParentNode == null) return false;
								node.ParentNode.InsertAfter(child, node);
								break;
						}
						return true;
					}
					else
					{
						if (child is XmlAttribute newAttr)
						{
							XmlAttribute attr;
							if (method > 0 && !(node is XmlAttribute)) return false;
							attr = method == 0 ? null : node as XmlAttribute;
							switch (method)
							{
								case 0: node.Attributes.Append(newAttr); break;
								case 1: attr.OwnerElement.Attributes.InsertBefore(newAttr, attr); break;
								case 2: attr.OwnerElement.Attributes.InsertAfter(newAttr, attr); break;
							}
							return true;
						}
					}
					return false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					XmlDocument doc;
					int methodPos = op == Operation.Node ? 4 : 5;
					int method = arguments.Length >= methodPos ? (int)arguments[methodPos - 1].GetIntValue(exm) : 0;
					if (method > 2 || method < 0) method = 0;
					bool saveToArg0 = true;
					if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
					{
						saveToArg0 = false;
						var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
						var dict = exm.VEvaluator.VariableData.DataXmlDocument;
						if (dict.ContainsKey(idx)) doc = dict[idx];
						else return -1;
					}
					else
					{
						string xml = arguments[0].GetStrValue(exm);
						doc = new XmlDocument();
						try
						{
							doc.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
						}
					}
		
					string path = arguments[1].GetStrValue(exm);
					XmlNodeList nodes;
					try
					{
						nodes = doc.SelectNodes(path);
					}
					catch (System.Xml.XPath.XPathException e)
					{
						throw new CodeEE(string.Format("{0}: XmlXPathParseError", Name, path, e.Message));
					}
					if (nodes.Count > 0)
					{
						int setAllPos = op == Operation.Node ? 5 : 6;
						bool setAllNodes = arguments.Length == setAllPos ? arguments[setAllPos - 1].GetIntValue(exm) != 0 : false;
						XmlNode child;
						if (op == Operation.Node)
						{
							var childNode = new XmlDocument();
							var xml = arguments[2].GetStrValue(exm);
							try
							{
								childNode.LoadXml(xml);
							}
							catch (XmlException e)
							{
								throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
							}
							var newNode = childNode.DocumentElement;
							child = doc.CreateNode(newNode.NodeType, newNode.Name, newNode.NamespaceURI);
							for (int i = 0; i < newNode.Attributes.Count; i++)
							{
								var xattr = newNode.Attributes[i];
								var attr = doc.CreateAttribute(xattr.Name);
								attr.Value = xattr.Value;
								child.Attributes.Append(attr);
							}
							child.InnerXml = newNode.InnerXml;
						}
						else
						{
							child = doc.CreateAttribute(arguments[2].GetStrValue(exm));
							if (arguments.Length >= 4) child.Value = arguments[3].GetStrValue(exm);
						}
						if (nodes.Count != 1)
						{
							if (setAllNodes)
								for (int i = 0; i < nodes.Count; i++) Insert(nodes[i], child, method);
						}
						else if (!Insert(nodes[0], child, method) && method > 0) return 0;
						if (saveToArg0)
						{
							(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
						}
					}
					return nodes.Count;
				}
			}

		private sealed class XmlRemoveNodeMethod : FunctionMethod
			{
				public enum Operation { Node, Attribute };
				public XmlRemoveNodeMethod(Operation op)
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int, ArgType.String, ArgType.Int  }, OmitStart = 2 },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefString, ArgType.String, ArgType.Int  }, OmitStart = 2 }
							};
					CanRestructure = false;
					this.op = op;
				}
				public XmlRemoveNodeMethod(Operation op, bool byname) : this(op)
				{
					byName = byname;
					argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.Int  }, OmitStart = 2 },
							};
				}
				private bool byName;
				Operation op;
				bool Remove(XmlNode node)
				{
					if (op == Operation.Attribute)
					{
						if (node is XmlAttribute attr)
						{
							attr.OwnerElement.Attributes.Remove(attr);
							return true;
						}
					}
					else
					{
						if (node.ParentNode != null)
						{
							var parent = node.ParentNode;
							node.ParentNode.RemoveChild(node);
							return true;
						}
					}
					return false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					XmlDocument doc;
					int method = arguments.Length >= 4 ? (int)arguments[3].GetIntValue(exm) : 0;
					if (method > 2 || method < 0) method = 0;
					bool saveToArg0 = true;
					if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
					{
						saveToArg0 = false;
						var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
						var dict = exm.VEvaluator.VariableData.DataXmlDocument;
						if (dict.ContainsKey(idx)) doc = dict[idx];
						else return -1;
					}
					else
					{
						string xml = arguments[0].GetStrValue(exm);
						doc = new XmlDocument();
						try
						{
							doc.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
						}
					}
		
					string path = arguments[1].GetStrValue(exm);
					XmlNodeList nodes;
					try
					{
						nodes = doc.SelectNodes(path);
					}
					catch (System.Xml.XPath.XPathException e)
					{
						throw new CodeEE(string.Format("{0}: XmlXPathParseError", Name, path, e.Message));
					}
					if (nodes.Count > 0)
					{
						bool setAllNodes = arguments.Length == 3 ? arguments[2].GetIntValue(exm) != 0 : false;
						if (nodes.Count != 1)
						{
							if (setAllNodes)
								for (int i = 0; i < nodes.Count; i++) Remove(nodes[i]);
						}
						else if (!Remove(nodes[0])) return 0;
						if (saveToArg0)
						{
							(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
						}
					}
					return nodes.Count;
				}
			}

		private sealed class XmlReplaceMethod : FunctionMethod
			{
				public XmlReplaceMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Any, ArgType.String  } },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int, ArgType.String, ArgType.String, ArgType.Int  }, OmitStart = 3 },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefString, ArgType.String, ArgType.String, ArgType.Int  }, OmitStart = 3 },
							};
					CanRestructure = false;
				}
				public XmlReplaceMethod(bool byname) : this()
				{
					byName = byname;
					argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.String, ArgType.Int  }, OmitStart = 3 },
							};
				}
				private bool byName;
		
				static bool Replace(XmlNode node, XmlNode newNode)
				{
					if (node.ParentNode != null)
					{
						node.ParentNode.ReplaceChild(newNode, node);
						return true;
					}
					return false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					XmlDocument newXml = new XmlDocument();
					{
						string xml = arguments.Length > 2 ? arguments[2].GetStrValue(exm) : arguments[1].GetStrValue(exm);
						try
						{
							newXml.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
						}
					}
					bool saveToArg0 = true;
					XmlDocument doc = null;
					if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)) || (arguments[0].GetOperandType() == typeof(string) && arguments.Length == 2))
					{
						saveToArg0 = false;
						var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
						var dict = exm.VEvaluator.VariableData.DataXmlDocument;
						if (!dict.ContainsKey(idx)) return -1;
						if (arguments.Length == 2)
						{
							dict[idx] = newXml;
							return 1;
						}
						doc = dict[idx];
					}
					else
					{
						string xml = arguments[0].GetStrValue(exm);
						doc = new XmlDocument();
						try
						{
							doc.LoadXml(xml);
						}
						catch (XmlException e)
						{
							throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
						}
					}
					string path = arguments[1].GetStrValue(exm);
					XmlNodeList nodes;
					try
					{
						nodes = doc.SelectNodes(path);
					}
					catch (System.Xml.XPath.XPathException e)
					{
						throw new CodeEE(string.Format("{0}: XmlXPathParseError", Name, path, e.Message));
					}
					if (nodes.Count > 0)
					{
						var newNode = newXml.DocumentElement;
						var child = doc.CreateNode(newNode.NodeType, newNode.Name, newNode.NamespaceURI);
						for (int i = 0; i < newNode.Attributes.Count; i++)
						{
							var xattr = newNode.Attributes[i];
							var attr = doc.CreateAttribute(xattr.Name);
							attr.Value = xattr.Value;
							child.Attributes.Append(attr);
						}
						child.InnerXml = newNode.InnerXml;
						bool setAllNodes = arguments.Length >= 4 ? arguments[3].GetIntValue(exm) != 0 : false;
						if (nodes.Count != 1)
						{
							if (setAllNodes)
								for (int i = 0; i < nodes.Count; i++) Replace(nodes[i], child);
						}
						else if (!Replace(nodes[0], child)) return 0;
						if (saveToArg0)
						{
							(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
						}
					}
					return nodes.Count;
				}
			}

		private sealed class ExistFileMethod : FunctionMethod
			{
				public ExistFileMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var filepath = Utils.GetValidPath(arguments[0].GetStrValue(exm));
					if (string.IsNullOrEmpty(filepath))
						return 0;
					//相対パスはゲームフォルダ基準。作業フォルダ基準では見つからない
					if (!Path.IsPathRooted(filepath))
						filepath = Program.ExeDir + uEmuera.Utils.NormalizeGamePath(filepath);
					if (File.Exists(filepath)) return 1;
					//綴りの大小が実体と違うだけかもしれない
					if (File.Exists(uEmuera.Utils.ResolvePath(filepath))) return 1;
					return 0;
				}
			}

		private sealed class DataTableManagementMethod : FunctionMethod
			{
				public enum Operation { Create, Check, Release, Clear, Case };
				public DataTableManagementMethod(Operation type)
				{
					ReturnType = typeof(long);
					if (type == Operation.Case)
						argumentTypeArray = new Type[] { typeof(string), typeof(long) };
					else
						argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					bool contains = dict.ContainsKey(key);
					switch (op)
					{
						case Operation.Clear:
							{
								if (contains)
								{
									dict[key].Clear();
									return 1;
								}
								return -1;
							}
						case Operation.Case:
							{
								if (contains)
								{
									dict[key].CaseSensitive = arguments[1].GetIntValue(exm) == 0;
									return 1;
								}
								return -1;
							}
						case Operation.Check: { return contains ? 1 : 0; }
						case Operation.Release: { if (contains) dict.Remove(key); return 1; }
					}
					if (contains) return 0;
					var dt = new DataTable(key)
					{
						CaseSensitive = true
					};
					var c = dt.Columns.Add("id", typeof(long));
					c.AllowDBNull = false;
					c.Unique = true;
					dict[key] = dt;
					dt.PrimaryKey = new System.Data.DataColumn[] { c };
					return 1;
				}
			}

		private sealed class DataTableColumnManagementMethod : FunctionMethod
			{
				public enum Operation { Create, Check, Remove, Names };
				public DataTableColumnManagementMethod(Operation type)
				{
					ReturnType = typeof(long);
					if (type == Operation.Create)
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.Any, ArgType.Int  }, OmitStart = 2 },
							};
					else if (type == Operation.Names)
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefString1D  }, OmitStart = 1 },
							};
					else
						argumentTypeArray = new Type[] { typeof(string), typeof(string) };
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return -1;
					var dt = dict[key];
					if (op == Operation.Names)
					{
						string[] output;
						if (arguments.Length > 1 && arguments[1] is VariableTerm v) output = v.Identifier.GetArray() as string[];
						else output = exm.VEvaluator.RESULTS_ARRAY;
						for (int i = 0; i < dt.Columns.Count; i++) output[i] = dt.Columns[i].ColumnName;
						return dt.Columns.Count;
					}
					string cName = arguments[1].GetStrValue(exm);
					bool contains = dt.Columns.Contains(cName);
					switch (op)
					{
						case Operation.Check: { return contains ? Utils.DataTable.TypeToInt(dt.Columns[cName].DataType) : 0; }
						case Operation.Remove:
							{
								if (contains && cName.ToLower() != "id")
								{
									dt.Columns.Remove(cName);
									return 1;
								}
								return 0;
							}
					}
					if (contains) return 0;
					Type t = null;
					if (arguments.Length >= 3)
					{
						if (arguments[2].GetOperandType() == typeof(string)) t = Utils.DataTable.NameToType(arguments[2].GetStrValue(exm));
						else t = Utils.DataTable.IntToType(arguments[2].GetIntValue(exm));
						if (t == null)
						{
							throw new CodeEE(string.Format("{0}: UnsupportedType", Name));
						}
					}
					bool nullable = arguments.Length == 4 ? arguments[3].GetIntValue(exm) != 0 : true;
					DataColumn dc;
					if (t != null) dc = dt.Columns.Add(cName, t);
					else dc = dt.Columns.Add(cName);
					dc.AllowDBNull = nullable;
					return 1;
				}
			}

		private sealed class DataTableRowSetMethod : FunctionMethod
			{
				public enum Operation { Add, Set };
				public DataTableRowSetMethod(Operation type)
				{
					ReturnType = typeof(long);
					if (type == Operation.Add)
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.VariadicString, ArgType.VariadicAny  }, MatchVariadicGroup = true, OmitStart = 1 },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefString1D, ArgType.RefAny1D, ArgType.Int  } },
							};
					else
						argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int, ArgType.VariadicString, ArgType.VariadicAny  }, MatchVariadicGroup = true },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int, ArgType.RefString1D, ArgType.RefAny1D, ArgType.Int  } },
							};
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				private static long lastGeneratedId = 0;
				void CheckName(DataTable dt, string name, string key)
				{
					if (name == "id")
						throw new CodeEE(string.Format("{0}: DTCanNotEditIdColumn", Name, key));
					if (!dt.Columns.Contains(name))
						throw new CodeEE(string.Format("{0}: DTLackOfNamedColumn", Name, key, name));
				}
				void SetValue(DataRow row, DataTable dt, string name, string key, ExpressionMediator exm, IOperandTerm v)
				{
					CheckName(dt, name, key);
					if (v == null)
					{
						row[name] = DBNull.Value;
						return;
					}
					bool isString = dt.Columns[name].DataType == typeof(string);
					if (v.GetOperandType() != (isString ? typeof(string) : typeof(long)))
						throw new CodeEE(string.Format("{0}: DTInvalidDataType", Name, key, name));
		
					if (isString)
						row[name] = v.GetStrValue(exm);
					else
						row[name] = Utils.DataTable.ConvertInt(v.GetIntValue(exm), dt.Columns[name].DataType);
				}
				void SetValue(DataRow row, DataTable dt, string name, string key, string str)
				{
					CheckName(dt, name, key);
					if (dt.Columns[name].DataType != typeof(string))
						throw new CodeEE(string.Format("{0}: DTInvalidDataType", Name, key, name));
					row[name] = str;
				}
				void SetValue(DataRow row, DataTable dt, string name, string key, long v)
				{
					CheckName(dt, name, key);
					if (dt.Columns[name].DataType == typeof(string))
						throw new CodeEE(string.Format("{0}: DTInvalidDataType", Name, key, name));
					row[name] = Utils.DataTable.ConvertInt(v, dt.Columns[name].DataType);
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var b = op == Operation.Add ? 0 : 1;
					string key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return -1;
					var dt = dict[key];
					var cCount = 0L;
					DataRow row = null;
					if (op == Operation.Set)
					{
						var idx = arguments[1].GetIntValue(exm);
						if (dt.Rows.Find(idx) is DataRow r)
							row = r;
						else return -2;
					}
					else
					{
						row = dt.NewRow();
						long nowTicks = System.DateTime.UtcNow.Ticks;
						if (nowTicks <= lastGeneratedId)
							nowTicks = lastGeneratedId + 1;
						while (dt.Rows.Find(nowTicks) != null)
							nowTicks++;
						lastGeneratedId = nowTicks;
						row[0] = nowTicks;
					}
					if (arguments.Length == b + 4)
					{
						var names = (arguments[b + 1] as VariableTerm).Identifier.GetArray() as string[];
						var count = Math.Min(names.Length, arguments[b + 3].GetIntValue(exm));
						if (arguments[b + 2].GetOperandType() == typeof(string))
						{
							var vals = (arguments[b + 2] as VariableTerm).Identifier.GetArray() as string[];
							count = Math.Min(vals.Length, count);
							for (int i = 0; i < count; i++)
								SetValue(row, dt, names[i], key, vals[i]);
							cCount += count;
						}
						else
						{
							var vals = (arguments[b + 2] as VariableTerm).Identifier.GetArray() as long[];
							count = Math.Min(vals.Length, count);
							for (int i = 0; i < count; i++)
								SetValue(row, dt, names[i], key, vals[i]);
							cCount += count;
						}
					}
					else
					{
						var pos = b + 1;
						while (pos < arguments.Length)
						{
							var name = arguments[pos].GetStrValue(exm);
							SetValue(row, dt, name, key, exm, arguments[pos + 1]);
							pos += 2;
							cCount++;
						}
					}
					if (op == Operation.Add)
					{
						try { dt.Rows.Add(row); return (long)row[0]; } catch (System.Exception e) { throw new MinorShift.Emuera.Sub.CodeEE(e.Message); }
					}
					return cCount;
				}
			}

		private sealed class DataTableLengthMethod : FunctionMethod
			{
				public enum Operation { Row, Column };
				public DataTableLengthMethod(Operation type)
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return -1;
					return op == Operation.Row ? dict[key].Rows.Count : dict[key].Columns.Count;
				}
			}

		private sealed class DataTableRowRemoveMethod : FunctionMethod
			{
				public DataTableRowRemoveMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int  } },
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefInt1D, ArgType.Int  } },
							};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return -1;
					var dt = dict[key];
					DataRow[] rows;
					if (arguments.Length == 3)
					{
						StringBuilder sb = new StringBuilder();
						var array = (arguments[1] as VariableTerm).Identifier.GetArray() as long[];
						var count = Math.Min((int)arguments[2].GetIntValue(exm), array.Length);
						if (count <= 0) return 0;
						sb.Append('(');
						for (int i = 0; i < count; i++)
							sb.Append(i == 0 ? array[i].ToString() : "," + array[i]);
						sb.Append(')');
						rows = dt.Select("id IN " + sb.ToString());
						if (rows == null) return 0;
					}
					else if (dt.Rows.Find(arguments[1].GetIntValue(exm)) is DataRow row)
						rows = new System.Data.DataRow[] { row };
					else return 0;
					foreach (var row in rows) dt.Rows.Remove(row);
					return rows.Length;
				}
			}

		private sealed class DataTableCellGetMethod : FunctionMethod
			{
				public enum Operation { Get, IsNull, Gets };
				public DataTableCellGetMethod(Operation type)
				{
					ReturnType = type == Operation.Gets ? typeof(string) : typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
								new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int, ArgType.String, ArgType.Int  }, OmitStart = 3 },
							};
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return op == Operation.IsNull ? -1 : 0;
					bool asId = arguments.Length == 4 ? arguments[3].GetIntValue(exm) != 0 : false;
					var dt = dict[key];
					var idx = arguments[1].GetIntValue(exm);
					var name = arguments[2].GetStrValue(exm);
					if (asId)
					{
						if (dt.Rows.Find(idx) is DataRow row && dt.Columns.Contains(name))
						{
							var v = row[name];
							return op == Operation.Get ? v == DBNull.Value ? 0 : Convert.ToInt64(v) : (v == DBNull.Value ? 1 : 0);
						}
					}
					else
					{
						if (0 <= idx && idx < dt.Rows.Count && dt.Columns.Contains(name))
						{
							var v = dt.Rows[(int)idx][name];
							return op == Operation.Get ? v == DBNull.Value ? 0 : Convert.ToInt64(v) : (v == DBNull.Value ? 1 : 0);
						}
					}
					return op == Operation.IsNull ? -2 : 0;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return string.Empty;
					bool asId = arguments.Length == 4 ? arguments[3].GetIntValue(exm) != 0 : false;
					var dt = dict[key];
					var idx = arguments[1].GetIntValue(exm);
					var name = arguments[2].GetStrValue(exm);
					if (asId)
					{
						if (dt.Rows.Find(idx) is DataRow row && dt.Columns.Contains(name))
						{
							var v = row[name];
							if (v != DBNull.Value) return (string)v;
						}
					}
					else
					{
						if (0 <= idx && idx < dt.Rows.Count && dt.Columns.Contains(name))
						{
							var v = dt.Rows[(int)idx][name];
							if (v != DBNull.Value) return v.ToString();
						}
					}
					return string.Empty;
				}
			}

		private sealed class DataTableCellSetMethod : FunctionMethod
			{
				public DataTableCellSetMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int, ArgType.String, ArgType.Any, ArgType.Int  }, OmitStart = 3 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return -1;
					bool asId = arguments.Length == 5 ? arguments[4].GetIntValue(exm) != 0 : false;
					var dt = dict[key];
					var idx = arguments[1].GetIntValue(exm);
					var name = arguments[2].GetStrValue(exm);
					if (name.ToLower() == "id") return 0;
					var v = arguments.Length > 3 ? arguments[3] : null;
					DataRow row = null;
					if (asId) row = dt.Rows.Find(idx);
					else if (idx >= 0 && idx < dt.Rows.Count) row = dt.Rows[(int)idx];
					if (row != null && dt.Columns.Contains(name))
					{
						if (v == null) row[name] = DBNull.Value;
						else
						{
							bool isString = dt.Columns[name].DataType == typeof(string);
							if (v.GetOperandType() != (isString ? typeof(string) : typeof(long))) return -2;
		
							if (isString)
								row[name] = v.GetStrValue(exm);
							else
								row[name] = Utils.DataTable.ConvertInt(v.GetIntValue(exm), dt.Columns[name].DataType);
						}
						return 1;
					}
					return -3;
				}
			}

		private sealed class DataTableSelectMethod : FunctionMethod
			{
				public DataTableSelectMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.String, ArgType.RefInt1D  }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return -1;
					var dt = dict[key];
					string filter = arguments.Length > 1 ? (arguments[1] != null ? arguments[1].GetStrValue(exm) : null) : null;
					string sort = arguments.Length > 2 ? (arguments[2] != null ? arguments[2].GetStrValue(exm) : null) : null;
					DataRow[] res;
					if (sort != null) res = dt.Select(filter, sort);
					else if (filter != null) res = dt.Select(filter);
					else res = dt.Select();
					bool toResult = arguments.Length != 4;
					long[] output = toResult ? GlobalStatic.VEvaluator.RESULT_ARRAY : (arguments[3] as VariableTerm).Identifier.GetArray() as long[];
					if (res != null)
					{
						int count = Math.Min(res.Length, toResult ? output.Length - 1 : output.Length);
						for (int i = 0; i < count; i++)
							output[toResult ? i + 1 : i] = (long)res[i][0];
						if (toResult) output[0] = res.Length;
						return res.Length;
					}
					if (toResult) output[0] = 0;
					return 0;
				}
			}

		private sealed class DataTableToXmlMethod : FunctionMethod
			{
				public DataTableToXmlMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefString  }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					if (!dict.ContainsKey(key)) return string.Empty;
					var dt = dict[key];
					var output = arguments.Length > 1 ? (arguments[1] as VariableTerm).Identifier.GetArray() as string[] : GlobalStatic.VEvaluator.RESULTS_ARRAY;
					var idx = arguments.Length > 1 ? 0 : 1;
		
					var sb = new StringBuilder();
					using (var sw = new StringWriter(sb))
					{
						dt.WriteXmlSchema(sw);
						output[idx] = sb.ToString();
						sb.Clear();
						dt.WriteXml(sw);
						return sb.ToString();
					}
				}
			}

		private sealed class DataTableFromXmlMethod : FunctionMethod
			{
				public DataTableFromXmlMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataDataTables;
					DataTable dt;
					try
					{
						dt = new DataTable(key);
						using (var reader = new StringReader(arguments[1].GetStrValue(exm)))
						{
							dt.ReadXmlSchema(reader);
						}
						using (var reader = new StringReader(arguments[2].GetStrValue(exm)))
						{
							dt.ReadXml(reader);
						}
					}
					catch
					{
						return 0;
					}
					if (dict.ContainsKey(key)) dict[key] = dt;
					else dict.Add(key, dt);
					return 1;
				}
			}

		private sealed class MapManagementMethod : FunctionMethod
			{
				public enum Operation { Create, Check, Release };
				public MapManagementMethod(Operation type)
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string key = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataStringMaps;
					bool contains = dict.ContainsKey(key);
					switch (op)
					{
						case Operation.Check: { return contains ? 1 : 0; }
						case Operation.Release: { if (contains) dict.Remove(key); return 1; }
					}
					if (contains) return 0;
					dict[key] = new Dictionary<string, string>();
					return 1;
				}
			}

		private sealed class MapDataOperationMethod : FunctionMethod
			{
				public enum Operation { Set, Has, Remove, Clear, Size };
				public MapDataOperationMethod(Operation type)
				{
					ReturnType = typeof(long);
					switch (type)
					{
						case Operation.Set:
							argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) }; break;
						case Operation.Has:
						case Operation.Remove:
							argumentTypeArray = new Type[] { typeof(string), typeof(string) }; break;
						default:
							argumentTypeArray = new Type[] { typeof(string) }; break;
					}
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var map = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataStringMaps;
					if (!dict.ContainsKey(map)) return -1;
					var sMap = dict[map];
					if (op == Operation.Clear) sMap.Clear();
					else if (op == Operation.Size) return (long)sMap.Count;
					else
					{
						var key = arguments[1].GetStrValue(exm);
						bool contains = sMap.ContainsKey(key);
						if (op == Operation.Has) return contains ? 1 : 0;
						if (op == Operation.Remove)
							sMap.Remove(key);
						else
							sMap[key] = arguments[2].GetStrValue(exm);
					}
					return 1;
				}
			}

		private sealed class MapGetStrMethod : FunctionMethod
			{
				public enum Operation { Get, ToXml, GetKeys };
				public MapGetStrMethod(Operation type)
				{
					ReturnType = typeof(string);
					switch (type)
					{
						case Operation.Get:
							argumentTypeArray = new Type[] { typeof(string), typeof(string) }; break;
						case Operation.ToXml:
							argumentTypeArray = new Type[] { typeof(string) }; break;
						case Operation.GetKeys:
							argumentTypeArrayEx = new ArgTypeList[] {
									new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int  }, OmitStart = 1 },
									new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.RefString1D, ArgType.Int  } },
								}; break;
					}
					CanRestructure = false;
					op = type;
				}
				private Operation op;
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var dict = exm.VEvaluator.VariableData.DataStringMaps;
					var map = arguments[0].GetStrValue(exm);
					if (!dict.ContainsKey(map)) return "";
					var sMap = dict[map];
					if (op == Operation.Get)
					{
						var key = arguments[1].GetStrValue(exm);
						if (sMap.ContainsKey(key)) return sMap[key];
						return "";
					}
					else if (op == Operation.GetKeys && arguments.Length > 1)
					{
						int count = 0;
						string[] array;
						if (arguments.Length == 3) // to array
						{
							var Term = arguments[1] as VariableTerm;
							if (arguments[2].GetIntValue(exm) == 0) return "";
							array = Term.Identifier.GetArray() as string[];
						}
						else if (arguments.Length == 2) // to RESULTS array
						{
							if (arguments[1].GetIntValue(exm) == 0) return "";
							array = exm.VEvaluator.RESULTS_ARRAY;
						}
						else return "";
						foreach (var k in sMap.Keys)
						{
							if (count >= array.Length) break;
							array[count] = k;
							count++;
						}
						exm.VEvaluator.RESULT = sMap.Keys.Count;
						return arguments.Length == 2 ? exm.VEvaluator.RESULTS : "";
					}
					StringBuilder sb = new StringBuilder();
					if (op == Operation.GetKeys)
					{
						bool isNotEmpty = false;
						foreach (var k in sMap.Keys)
						{
							if (isNotEmpty) sb.Append(",").Append(k);
							else
							{
								isNotEmpty = true;
								sb.Append(k);
							}
						}
					}
					else
					{
						sb.Append("<map>");
						foreach (var p in sMap)
							sb.Append(string.Format("<p><k>{0}</k><v>{1}</v></p>", p.Key, p.Value));
						sb.Append("</map>");
					}
					return sb.ToString();
				}
			}

		private sealed class MapFromXmlMethod : FunctionMethod
			{
				public MapFromXmlMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string), typeof(string) };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					var map = arguments[0].GetStrValue(exm);
					var dict = exm.VEvaluator.VariableData.DataStringMaps;
					if (!dict.ContainsKey(map)) return 0;
					var xml = arguments[1].GetStrValue(exm);
					var sMap = dict[map];
					XmlDocument doc = new XmlDocument();
					XmlNodeList nodes;
					try
					{
						doc.LoadXml(xml);
						nodes = doc.SelectNodes("/map/p");
					}
					catch (XmlException e)
					{
						throw new CodeEE(string.Format("{0}: XmlParseError", Name, xml, e.Message));
					}
					for (int i = 0; i < nodes.Count; i++)
					{
						XmlNodeList key, val;
						var node = nodes[i];
						key = node.SelectNodes("./k");
						val = node.SelectNodes("./v");
						if (key.Count != 1 || val.Count != 1) continue;
						sMap[key[0].InnerText] = val[0].InnerXml;
					}
					return 1;
				}
			}

		private sealed class MoveTextBoxMethod : FunctionMethod
			{
				public MoveTextBoxMethod(bool b = false)
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(long), typeof(long), typeof(long) };
					CanRestructure = false;
					resume = b;
				}
				bool resume;
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					if (resume) exm.Console.Window.ResetTextBoxPos();
					else exm.Console.Window.SetTextBoxPos(
						(int)arguments[0].GetIntValue(exm),
						(int)arguments[1].GetIntValue(exm),
						(int)arguments[2].GetIntValue(exm));
					return 1;
				}
			}

		private sealed class MouseButtonMethod : FunctionMethod
			{
				public MouseButtonMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArray = new Type[] {  };
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					//if (exm.Console.SelectingButton != null)
					//	return exm.Console.SelectingButton.ToString();
					bool b = exm.Console.AlwaysRefresh;
					//Point point = exm.Console.Window.MainPicBox.PointToClient(Control.MousePosition);
					//exm.Console.AlwaysRefresh = true;
					//if (exm.Console.Window.MainPicBox.ClientRectangle.Contains(point))
						//exm.Console.MoveMouse(point);
					//exm.Console.AlwaysRefresh = b;
					if (exm.Console.PointingSring != null)
					{
						if (!exm.Console.PointingSring.IsButton)
							return "";
						if (exm.Console.PointingSring.IsInteger)
							return exm.Console.PointingSring.Input.ToString();
						return exm.Console.PointingSring.Inputs;
					}
					return "";
				}
			}

		private sealed class ExistSoundMethod : FunctionMethod
			{
				public ExistSoundMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string str = arguments[0].GetStrValue(exm);
					string filepath = Path.GetFullPath(".\\sound\\" + str);
					if (File.Exists(filepath))
						return 1;
					return 0;
				}
			}

		private sealed class GetUsingMemoryMethod : FunctionMethod
			{
				public GetUsingMemoryMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] {  };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					using (System.Diagnostics.Process memory = System.Diagnostics.Process.GetCurrentProcess())
					{
						return memory.WorkingSet64;
					}
				}
			}

		private sealed class ClearMemoryMethod : FunctionMethod
			{
				public ClearMemoryMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] {  };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					using (System.Diagnostics.Process destmemory = System.Diagnostics.Process.GetCurrentProcess())
					{
						long destmemorysize = destmemory.WorkingSet64;
						GC.Collect();
						using (System.Diagnostics.Process memory = System.Diagnostics.Process.GetCurrentProcess())
						{
							return destmemorysize - memory.WorkingSet64;
						}
					}
				}
			}

		private sealed class GetTextBoxMethod : FunctionMethod
			{
				public GetTextBoxMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArray = new Type[] {  };
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					return exm.Console.Window.TextBox.Text;
				}
			}

		private sealed class ChangeTextBoxMethod : FunctionMethod
			{
				public ChangeTextBoxMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					exm.Console.Window.ChangeTextBox(arguments[0].GetStrValue(exm));
					return 1;
				}
			}

		private sealed class ErdNameMethod : FunctionMethod
			{
				public ErdNameMethod()
				{
					ReturnType = typeof(string);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.RefAny | ArgType.AllowConstRef, ArgType.Int, ArgType.Int  }, OmitStart = 2 },
						};
					CanRestructure = true;
					HasUniqueRestructure = true;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					VariableTerm vToken = (VariableTerm)arguments[0];
					string varname = "";
					if (arguments.Length > 2)
						varname = vToken.Identifier.Name + "@" + arguments[2].GetIntValue(exm);
					else
						varname = vToken.Identifier.Name;
					long value = arguments[1].GetIntValue(exm);
					string ret = ""; if (false)
						return ret;
					else
						return "";
				}
				public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					arguments[1] = arguments[1].Restructure(exm);
					return arguments[1] is SingleTerm;
				}
			}

		private sealed class GetDisplayLineMethod : FunctionMethod
			{
				public GetDisplayLineMethod()
				{
					ReturnType = typeof(string);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int  }},
						};
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					//修正に失敗したので差し戻す
					//long num = arguments[0].GetIntValue(exm)-exm.Console.DeletedLines;
					long num = arguments[0].GetIntValue(exm);
					if (num < 0 || num >= exm.Console.displayLineList.Count)
						return "";
					else
						return exm.Console.displayLineList[(int)num].ToString();
				}
			}

		private sealed class GetDoingFunctionMethod : FunctionMethod
			{
				public GetDoingFunctionMethod()
				{
					ReturnType = typeof(string);
					argumentTypeArray = new Type[] {  };
					CanRestructure = true;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					LogicalLine line = exm.Process.GetScaningLine();
					if ((line == null) || (line.ParentLabelLine == null))
						return "";//システム待機中のデバッグモードから呼び出し
					return line.ParentLabelLine.LabelName;
				}
			}

		private sealed class FlowInputMethod : FunctionMethod
			{
				public FlowInputMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> { ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
		
					exm.Process.flowinputDef = arguments[0].GetIntValue(exm);
					if (arguments.Length > 1)
						exm.Process.flowinput = arguments[1].GetIntValue(exm) != 0 ? true : false ;
					if (arguments.Length > 2)
						exm.Process.flowinputCanSkip = arguments[2].GetIntValue(exm) != 0 ? true : false ;
					if (arguments.Length > 3)
						exm.Process.flowinputForceSkip = arguments[3].GetIntValue(exm) != 0 ? true : false;
					return 0;
				}
			}

		private sealed class FlowInputsMethod : FunctionMethod
			{
				public FlowInputsMethod()
				{
					ReturnType = typeof(long);
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> { ArgType.Int, ArgType.String }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
		
					exm.Process.flowinputString = arguments[0].GetIntValue(exm) != 0 ? true : false ;
					if (arguments.Length > 1)
						exm.Process.flowinputDefString = arguments[1].GetStrValue(exm);
					return 0;
				}
			}

		private sealed class GetMethMethod : FunctionMethod
			{
				public GetMethMethod()
				{
					ReturnType = typeof(Int64);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int, ArgType.VariadicAny  }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
		
				public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					IOperandTerm[] methArgs = arguments.Skip(2).ToArray();
					var term = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, name, methArgs, true);
		
					if (term == null)
					{
						if (arguments.Length < 2 || arguments[1] == null)
							throw new CodeEE(string.Format("{0}: NotDefinedUserFunc", name));
						else
							return arguments[1].GetIntValue(exm);
					}
					else if (!term.IsInteger)
						throw new CodeEE(string.Format("{0}: IsNotInt", name));
					else
						return term.GetIntValue(exm);
				}
			}

		private sealed class GetMethsMethod : FunctionMethod
			{
				public GetMethsMethod()
				{
					ReturnType = typeof(string);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.String, ArgType.VariadicAny  }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					IOperandTerm[] methArgs = arguments.Skip(2).ToArray();
					var term = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, name, methArgs, true);
		
					if (term == null)
					{
						if (arguments.Length < 2 || arguments[1] == null)
							throw new CodeEE(string.Format("{0}: NotDefinedUserFunc", name));
						else
							return arguments[1].GetStrValue(exm);
					}
					else if (!term.IsString)
						throw new CodeEE(string.Format("{0}: IsNotStr", name));
					else
						return term.GetStrValue(exm);
				}
			}

		private sealed class ExistMethMethod : FunctionMethod
			{
				public ExistMethMethod()
				{
					ReturnType = typeof(Int64);
					argumentTypeArray = new Type[] { typeof(string) };
					CanRestructure = true;
				}
		
				public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string name = arguments[0].GetStrValue(exm);
					IOperandTerm term;
					try
					{
						term = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, name, new IOperandTerm[0], true);
					}
					catch (CodeEE)
					{
						return 0;
					}
		
					if (term == null)
					{
						return 0;
					}
					else
					{
						Int64 res = 0;
						if (term.IsInteger) res |= 1;
						if (term.IsString) res |= 2;
						return res;
					}
				}
			}

		private sealed class BitmapCacheEnableMethod : FunctionMethod
			{
				public BitmapCacheEnableMethod()
				{
					ReturnType = typeof(long);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int  }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					long argument0 = arguments[0].GetIntValue(exm);
					//GlobalStatic.Console.bitmapCacheEnabledForNextLine = argument0 != 0;
					return 0;
				}
			}

		private sealed class HotkeyStateMethod : FunctionMethod
			{
				public HotkeyStateMethod()
				{
					ReturnType = typeof(Int64);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int, ArgType.Int }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					Int64 argument0 = arguments[0].GetIntValue(exm);
					Int64 argument1 = arguments[1].GetIntValue(exm);
					// GlobalStatic.Console.Window.hotkeyState.HotkeyStateSet((IntPtr)argument0, (IntPtr)argument1);
					return 0;
				}
			}

		private sealed class HotkeyStateInitMethod : FunctionMethod
			{
				public HotkeyStateInitMethod()
				{
					ReturnType = typeof(Int64);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.Int }, OmitStart = 1 },
						};
					CanRestructure = false;
				}
				public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					Int64 argument0 = arguments[0].GetIntValue(exm);
					// GlobalStatic.Console.Window.hotkeyState.HotkeyStateInit((IntPtr)argument0);
					return 0;
				}
			}

		private sealed class OutputlogMethod : FunctionMethod
			{
				public OutputlogMethod()
				{
					ReturnType = typeof(Int64);
					// argumentTypeArray = null;
					argumentTypeArrayEx = new ArgTypeList[] {
							new ArgTypeList { ArgTypesEnum = new List<ArgType> {  ArgType.String, ArgType.Int }, OmitStart = 0 },
						};
					CanRestructure = false;
				}
				public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string filename = "";
					if (arguments.Length > 0)
						filename = arguments[0].GetStrValue(exm);
					bool hideInfo = false;
					if (arguments.Length > 1)
						hideInfo = arguments[1].GetIntValue(exm) == 1;
		
			
					exm.Console.OutputLog(filename);
					return 1;
				}
		
			}

	}
	internal sealed class ExistFunctionMethod : FunctionMethod
	{
		public ExistFunctionMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = new ArgTypeList[] {
				new ArgTypeList { ArgTypesEnum = new List<ArgType> { ArgType.String, ArgType.Int }, OmitStart = 1 }
			};
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			//本家(EE)と同じ:
			//  0:無し 1:通常の関数 2:#FUNCTION 3:#FUNCTIONS
			//  第2引数が非0なら大文字小文字を無視して探す
			//以前は第2引数でイベント関数を探しており、2/3も返していなかった
			string functionname = arguments[0].GetStrValue(exm);
			FunctionLabelLine func = null;
			if (arguments.Length == 1 || arguments[1].GetIntValue(exm) == 0)
			{
				if (Config.SCFunction == StringComparison.OrdinalIgnoreCase)
					func = GlobalStatic.LabelDictionary.GetNonEventLabel(functionname.ToUpper());
				else
					func = GlobalStatic.LabelDictionary.GetNonEventLabel(functionname);
			}
			else
			{
				foreach (string funcname in GlobalStatic.LabelDictionary.NoneventKeys)
				{
					if (funcname.ToUpper() == functionname.ToUpper())
					{
						func = GlobalStatic.LabelDictionary.GetNonEventLabel(funcname);
						break;
					}
				}
			}
			if (func == null)
				return 0;
			if (func.IsMethod)
			{
				if (func.MethodType == typeof(string))
					return 3;
				else if (func.MethodType == typeof(long))
					return 2;
			}
			return 1;
		}
	}
}