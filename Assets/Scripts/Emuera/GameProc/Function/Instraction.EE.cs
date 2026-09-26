using System;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc;

namespace MinorShift.Emuera.GameProc.Function
{
    internal sealed partial class FunctionIdentifier
    {
                private sealed class VARI_ArgBuilder : ArgumentBuilder
        {
            public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
            {
                string text = line.PopArgumentPrimitive().Substring();
                int commentIndex = text.IndexOf(';');
                if (commentIndex != -1) text = text.Substring(0, commentIndex);
                int equalsIndex = text.IndexOf('=');
                string left = equalsIndex == -1 ? text : text.Substring(0, equalsIndex);
                string right = equalsIndex == -1 ? "" : text.Substring(equalsIndex + 1);
                string[] leftSplit = left.Split(',');
                string varName = leftSplit[0].Trim();
                System.Collections.Generic.List<int> lengths = new System.Collections.Generic.List<int>();
                if (leftSplit.Length > 1)
                {
                    for (int i = 1; i < leftSplit.Length; i++) lengths.Add(int.Parse(leftSplit[i].Trim()));
                }
                else lengths.Add(1);
                
                MinorShift.Emuera.GameData.Expression.IOperandTerm exp = null;
                if (!string.IsNullOrEmpty(right.Trim()))
                {
                    MinorShift.Emuera.GlobalStatic.Process.scaningLine = line;
                    MinorShift.Emuera.Sub.WordCollection wc = MinorShift.Emuera.Sub.LexicalAnalyzer.Analyse(new MinorShift.Emuera.Sub.StringStream(right), MinorShift.Emuera.Sub.LexEndWith.EoL, MinorShift.Emuera.Sub.LexAnalyzeFlag.None);
                    exp = MinorShift.Emuera.GameData.Expression.ExpressionParser.ReduceIntegerTerm(wc, MinorShift.Emuera.GameData.Expression.TermEndWith.EoL);
                }
                
                MinorShift.Emuera.GameProc.UserDefinedVariableData varData = new MinorShift.Emuera.GameProc.UserDefinedVariableData();
                varData.Name = varName;
                varData.Static = false;
                varData.Lengths = lengths.ToArray();
                varData.Dimension = lengths.Count;
                varData.TypeIsStr = false;
                line.ParentLabelLine.AddPrivateVariable(varData);
                
                if (exp != null) return new IntAsignArgument(varName, lengths.ToArray(), exp);
                else return new IntAsignArgument(varName, lengths.ToArray(), new MinorShift.Emuera.GameData.Expression.SingleTerm(0));
            }
        }

        private sealed class VARS_ArgBuilder : ArgumentBuilder
        {
            public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
            {
                string text = line.PopArgumentPrimitive().Substring();
                int commentIndex = text.IndexOf(';');
                if (commentIndex != -1) text = text.Substring(0, commentIndex);
                int equalsIndex = text.IndexOf('=');
                string left = equalsIndex == -1 ? text : text.Substring(0, equalsIndex);
                string right = equalsIndex == -1 ? "" : text.Substring(equalsIndex + 1);
                string[] leftSplit = left.Split(',');
                string varName = leftSplit[0].Trim();
                System.Collections.Generic.List<int> lengths = new System.Collections.Generic.List<int>();
                if (leftSplit.Length > 1)
                {
                    for (int i = 1; i < leftSplit.Length; i++) lengths.Add(int.Parse(leftSplit[i].Trim()));
                }
                else lengths.Add(1);
                
                string valueStr = null;
                if (!string.IsNullOrEmpty(right.Trim()))
                {
                    MinorShift.Emuera.GlobalStatic.Process.scaningLine = line;
                    MinorShift.Emuera.Sub.WordCollection wc = MinorShift.Emuera.Sub.LexicalAnalyzer.Analyse(new MinorShift.Emuera.Sub.StringStream(right), MinorShift.Emuera.Sub.LexEndWith.EoL, MinorShift.Emuera.Sub.LexAnalyzeFlag.None);
                    MinorShift.Emuera.GameData.Expression.IOperandTerm exp = MinorShift.Emuera.GameData.Expression.ExpressionParser.ReduceExpressionTerm(wc, MinorShift.Emuera.GameData.Expression.TermEndWith.EoL);
                    if (exp is MinorShift.Emuera.GameData.Expression.SingleTerm) valueStr = ((MinorShift.Emuera.GameData.Expression.SingleTerm)exp).Str;
                }
                
                MinorShift.Emuera.GameProc.UserDefinedVariableData varData = new MinorShift.Emuera.GameProc.UserDefinedVariableData();
                varData.Name = varName;
                varData.Static = false;
                varData.Lengths = lengths.ToArray();
                varData.Dimension = lengths.Count;
                varData.TypeIsStr = true;
                line.ParentLabelLine.AddPrivateVariable(varData);
                
                return new StrAsignArgument(varName, lengths.ToArray(), valueStr ?? "");
            }
        }
private sealed class VARI_Instruction : AbstractInstruction
        {
            public VARI_Instruction()
            {
                ArgBuilder = new VARI_ArgBuilder();
                flag = 0x00004 | 0x00002 | 0x00010 | 0x00020; // METHOD_SAFE | EXTENDED | PARTIAL | FORCE_SETARG
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                var arg = (IntAsignArgument)func.Argument;
                var varName = arg.ConstStr;
                var privateVar = func.ParentLabelLine.GetPrivateVariable(varName);
                // privateVar.ScopeIn();
                if (privateVar.GetLength(0) == 1)
                {
                    privateVar.SetValue(arg.Exp.GetIntValue(exm), new long[] { 0 });
                }
            }
            public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
            {
                return null;
            }
        }
        private sealed class VARS_Instruction : AbstractInstruction
        {
            public VARS_Instruction()
            {
                ArgBuilder = new VARS_ArgBuilder();
                flag = 0x00004 | 0x00002 | 0x00010 | 0x00020; // METHOD_SAFE | EXTENDED | PARTIAL | FORCE_SETARG
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                var arg = (StrAsignArgument)func.Argument;
                var varName = arg.ConstStr;
                var privateVar = func.ParentLabelLine.GetPrivateVariable(varName);
                // privateVar.ScopeIn();
                if (privateVar.GetLength(0) == 1)
                {
                    privateVar.SetValue(arg.Value, new long[] { 0 });
                }
            }
            public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
            {
                return null;
            }
        }
        private sealed class TRYCALLF_Instruction : AbstractInstruction
        {
            public TRYCALLF_Instruction(bool form)
            {
                if (form)
                    ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORMF);
                else
                    ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLF);
                flag = 0x00002 | 0x00004 | 0x00020; // EXTENDED | METHOD_SAFE | FORCE_SETARG
            }

            public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
            {
                if (func.Argument == null)
                    return;
                if (!func.Argument.IsConst)
                {
                    useCallForm = true;
                    return;
                }
                SpCallFArgment callfArg = (SpCallFArgment)func.Argument;
                try
                {
                    callfArg.FuncTerm = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, callfArg.ConstStr, callfArg.RowArgs, true);
                }
                catch
                {
                    return;
                }
                if (callfArg.FuncTerm == null)
                {
                    return;
                }
            }

            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                MinorShift.Emuera.GameData.Expression.IOperandTerm mToken;
                string labelName;
                if ((!func.Argument.IsConst) || exm.Console.RunERBFromMemory)
                {
                    SpCallFArgment spCallformArg = (SpCallFArgment)func.Argument;
                    labelName = spCallformArg.FuncnameTerm.GetStrValue(exm);
                    mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, labelName, spCallformArg.RowArgs, true);
                }
                else
                {
                    labelName = func.Argument.ConstStr;
                    mToken = ((SpCallFArgment)func.Argument).FuncTerm;
                }
                if (mToken == null)
                    return;
                mToken.GetValue(exm);
            }
        }
        private sealed class PLAYBGM_Instruction : AbstractInstruction
        {
            public PLAYBGM_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                string filename = func.Argument.ConstStr;
                if (!func.Argument.IsConst)
                    filename = ((MinorShift.Emuera.GameProc.Function.ExpressionArgument)func.Argument).Term.GetStrValue(exm);
                uEmuera.SoundManager.Instance?.PlayBGM(filename);
            }
        }

        private sealed class STOPBGM_Instruction : AbstractInstruction
        {
            public STOPBGM_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                uEmuera.SoundManager.Instance?.StopBGM();
            }
        }

        private sealed class SETBGMVOLUME_Instruction : AbstractInstruction
        {
            public SETBGMVOLUME_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                long volume = func.Argument.ConstInt;
                if (!func.Argument.IsConst)
                    volume = ((MinorShift.Emuera.GameProc.Function.ExpressionArgument)func.Argument).Term.GetIntValue(exm);
                uEmuera.SoundManager.Instance?.SetBGMVolume((int)volume);
            }
        }

        /// <summary>
        /// CHECK_OVERFLOW 0/1 (Emuera1824+v10+v3)
        /// 1で、整数演算が溢れた時に符号が反転せずINT64の最大値/最小値に張り付く
        /// </summary>
        private sealed class CHECK_OVERFLOW_Instruction : AbstractInstruction
        {
            public CHECK_OVERFLOW_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                long mode = func.Argument.ConstInt;
                if (!func.Argument.IsConst)
                    mode = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);
                OverflowMode.Saturate = mode != 0;
            }
        }

        private sealed class PLAYSOUND_Instruction : AbstractInstruction
        {
            public PLAYSOUND_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                string filename = func.Argument.ConstStr;
                if (!func.Argument.IsConst)
                    filename = ((MinorShift.Emuera.GameProc.Function.ExpressionArgument)func.Argument).Term.GetStrValue(exm);
                uEmuera.SoundManager.Instance?.PlaySound(filename);
            }
        }

        private sealed class STOPSOUND_Instruction : AbstractInstruction
        {
            public STOPSOUND_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                uEmuera.SoundManager.Instance?.StopSound();
            }
        }

        private sealed class SETSOUNDVOLUME_Instruction : AbstractInstruction
        {
            public SETSOUNDVOLUME_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                long volume = func.Argument.ConstInt;
                if (!func.Argument.IsConst)
                    volume = ((MinorShift.Emuera.GameProc.Function.ExpressionArgument)func.Argument).Term.GetIntValue(exm);
                uEmuera.SoundManager.Instance?.SetSoundVolume((int)volume);
            }
        }
        private sealed class DummyGraphics_ArgumentBuilder : ArgumentBuilder
        {
            public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
            {
                MinorShift.Emuera.Sub.StringStream st = line.PopArgumentPrimitive();
                MinorShift.Emuera.Sub.WordCollection wc = MinorShift.Emuera.Sub.LexicalAnalyzer.Analyse(st, MinorShift.Emuera.Sub.LexEndWith.EoL, MinorShift.Emuera.Sub.LexAnalyzeFlag.None);
                IOperandTerm[] args = MinorShift.Emuera.GameData.Expression.ExpressionParser.ReduceArguments(wc, MinorShift.Emuera.GameData.Expression.ArgsEndWith.EoL, false);
                for(int i = 0; i< args.Length;i++)
                {
                    if(args[i] != null)
                        args[i] = args[i].Restructure(exm);
                }
                return new MinorShift.Emuera.GameProc.Function.SpPrintVArgument(args);
            }
        }
        private        class DummyGraphics_Instruction : AbstractInstruction
        {
            static System.Collections.Generic.Dictionary<string, MinorShift.Emuera.GameData.Function.FunctionMethod> methodCache = null;

            public DummyGraphics_Instruction()
            {
                ArgBuilder = new DummyGraphics_ArgumentBuilder();
                flag = 0x00002 | 0x00004; // EXTENDED | METHOD_SAFE
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                if (methodCache == null) {
                    methodCache = MinorShift.Emuera.GameData.Function.FunctionMethodCreator.GetMethodList();
                }
                var arg = func.Argument as MinorShift.Emuera.GameProc.Function.SpPrintVArgument;
                if (arg != null && arg.Terms != null)
                {
                    MinorShift.Emuera.GameData.Function.FunctionMethod method = null;
                    if (methodCache.TryGetValue(func.Function.Name, out method))
                    {
                        string err = method.CheckArgumentType(func.Function.Name, arg.Terms);
                        if (err != null)
                            throw new MinorShift.Emuera.Sub.CodeEE(err);
                        //命令形はRESULTに結果を返す。捨てるとERB側の判定(IF RESULT)が壊れる
                        exm.VEvaluator.RESULT = method.GetIntValue(exm, arg.Terms);
                    }
                }
            }
        }
		internal sealed class HTML_PRINT_ISLAND_Instruction : AbstractInstruction
		{
			public HTML_PRINT_ISLAND_Instruction()
			{
				// Takes a string expression, and optionally an integer (layer)
				// Since we just dummy it, we can accept ANY arguments or SP_ANY
				// Or we can just use ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION_NULLABLE) - wait, if there are multiple args, we need SP_something or custom.
				// Let's use SP_PRINT to allow parsing multiple args and do nothing.
				ArgBuilder = new HTML_PRINT_ArgumentBuilder();
				flag = METHOD_SAFE | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				//本家(Emuera.NET)と同じく、ログにも流れにも入らない独立したHTMLとして
				//画面の上端から重ねて表示する。描画はUnity側のIslandLayer
				if (GlobalStatic.Process.SkipPrint)
					return;
				var arg = (ExpressionsArgument)func.Argument;
				string str;
				if (arg.ArgumentArray[0] is SingleTerm st)
					str = st.Str;
				else
					str = arg.ArgumentArray[0].GetStrValue(exm);
				exm.Console.PrintHTMLIsland(str);
			}
		}

		internal sealed class HTML_PRINT_ISLAND_CLEAR_Instruction : AbstractInstruction
		{
			public HTML_PRINT_ISLAND_CLEAR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION_NULLABLE);
				flag = METHOD_SAFE | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.ClearHTMLIsland();
			}
		}

		internal sealed class MATCHALL_Instruction : AbstractInstruction
		{
			public MATCHALL_Instruction()
			{
				// MATCHALL usually takes a variable and an array/value
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_VAR_SET);
				flag = METHOD_SAFE | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				// Dummy implementation
			}
		}
    }
	//EE_BINPUT: 表示中のボタンからしか入力できないINPUT。
	//押せるボタンが1つも無ければ、既定値があればそれを返し、無ければエラー
	internal sealed class BINPUT_Instruction : AbstractInstruction
	{
		public BINPUT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
			flag = 0x02000 | 0x04000; // IS_PRINT | IS_INPUT
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!exm.Console.EmptyLine)
				exm.Console.NewLine();
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new InputRequest();
			req.InputType = InputType.IntButton;
			bool canSkip = arg.CanSkip != null && GlobalStatic.Console.MesSkip;
			if (!canSkip && !exm.Console.FindCurrentButton(b => b.IsInteger))
			{
				if (arg.Def == null)
					throw new MinorShift.Emuera.Sub.CodeEE("BINPUT:選択できるボタンがありません");
				GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
				return;
			}
			FunctionIdentifier.WaitIntInput(exm, arg, req);
		}
	}
	//EE_BINPUT: 表示中のボタンからしか入力できないINPUT。
	//押せるボタンが1つも無ければ、既定値があればそれを返し、無ければエラー
	internal sealed class BINPUTS_Instruction : AbstractInstruction
	{
		public BINPUTS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
			flag = 0x02000 | 0x04000; // IS_PRINT | IS_INPUT
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!exm.Console.EmptyLine)
				exm.Console.NewLine();
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new InputRequest();
			req.InputType = InputType.StrButton;
			bool canSkip = arg.CanSkip != null && GlobalStatic.Console.MesSkip;
			if (!canSkip && !exm.Console.FindCurrentButton(b => true))
			{
				if (arg.Def == null)
					throw new MinorShift.Emuera.Sub.CodeEE("BINPUTS:選択できるボタンがありません");
				GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
				return;
			}
			FunctionIdentifier.WaitStrInput(exm, arg, req);
		}
	}
	//EE_BINPUT: 表示中のボタンからしか入力できないINPUT。
	//押せるボタンが1つも無ければ、既定値があればそれを返し、無ければエラー
	internal sealed class ONEBINPUT_Instruction : AbstractInstruction
	{
		public ONEBINPUT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
			flag = 0x02000 | 0x04000; // IS_PRINT | IS_INPUT
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!exm.Console.EmptyLine)
				exm.Console.NewLine();
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new InputRequest();
			req.InputType = InputType.IntButton;
			req.OneInput = true;
			bool canSkip = arg.CanSkip != null && GlobalStatic.Console.MesSkip;
			if (!canSkip && !exm.Console.FindCurrentButton(b => b.IsInteger))
			{
				if (arg.Def == null)
					throw new MinorShift.Emuera.Sub.CodeEE("ONEBINPUT:選択できるボタンがありません");
				GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
				return;
			}
			FunctionIdentifier.WaitIntInput(exm, arg, req);
		}
	}
	//EE_BINPUT: 表示中のボタンからしか入力できないINPUT。
	//押せるボタンが1つも無ければ、既定値があればそれを返し、無ければエラー
	internal sealed class ONEBINPUTS_Instruction : AbstractInstruction
	{
		public ONEBINPUTS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
			flag = 0x02000 | 0x04000; // IS_PRINT | IS_INPUT
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!exm.Console.EmptyLine)
				exm.Console.NewLine();
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new InputRequest();
			req.InputType = InputType.StrButton;
			req.OneInput = true;
			bool canSkip = arg.CanSkip != null && GlobalStatic.Console.MesSkip;
			if (!canSkip && !exm.Console.FindCurrentButton(b => true))
			{
				if (arg.Def == null)
					throw new MinorShift.Emuera.Sub.CodeEE("ONEBINPUTS:選択できるボタンがありません");
				GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
				return;
			}
			FunctionIdentifier.WaitStrInput(exm, arg, req);
		}
	}

		internal sealed class SKIPLOG_Instruction : AbstractInstruction
	{
		public SKIPLOG_Instruction() { ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION); flag = 0x00020 | 0x00002; }
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			// Dummy: Ignore SKIPLOG in Android Emuera
		}
	}
	internal sealed class QUIT_AND_RESTART_Instruction : AbstractInstruction
	{
		public QUIT_AND_RESTART_Instruction() { ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID); }
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.PrintSingleLine("QUIT_AND_RESTART called, stopping execution.");
			state.SystemState = SystemStateCode.Title_Begin;
		}
	}
	internal sealed class FORCE_QUIT_AND_RESTART_Instruction : AbstractInstruction
	{
		public FORCE_QUIT_AND_RESTART_Instruction() { ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID); }
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.PrintSingleLine("FORCE_QUIT_AND_RESTART called, stopping execution.");
			state.SystemState = SystemStateCode.Title_Begin;
		}
	}
}