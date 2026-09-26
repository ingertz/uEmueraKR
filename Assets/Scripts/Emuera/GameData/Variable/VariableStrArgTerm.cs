using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameData.Expression;

namespace MinorShift.Emuera.GameData.Variable
{
	
	//変数の引数のうち文字列型のもの。
	internal sealed class VariableStrArgTerm : IOperandTerm
	{
		public VariableStrArgTerm(VariableCode code, IOperandTerm strTerm, int index)
			: this(code, strTerm, index, null)
		{
		}

		/// <summary>EE_ERD: varnameはERDの辞書を引くユーザー定義変数の名前</summary>
		public VariableStrArgTerm(VariableCode code, IOperandTerm strTerm, int index, string varname)
			: base(typeof(Int64))
		{
			this.varname = varname;
			this.strTerm = strTerm;
			parentCode = code;
			this.index = index;
		}
		IOperandTerm strTerm;
		readonly VariableCode parentCode;
		readonly int index;
		readonly string varname;
		Dictionary<string, int> dic = null;
		string errPos = null;
		
        public override Int64 GetIntValue(ExpressionMediator exm)
		{
			if (dic == null)
				dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index, varname);
			string key = strTerm.GetStrValue(exm);
			if (key == "")
				throw new CodeEE("キーワードを空には出来ません");
			if (dic == null)
				throw new CodeEE("変数" + parentCode.ToString() + "のERDに\"" + key + "\"の定義がありません");
            if (!dic.TryGetValue(key, out int i))
            {
                if (errPos == null)
                    throw new CodeEE("配列変数" + parentCode.ToString() + "の要素を文字列で指定することはできません");
                else
                    throw new CodeEE(errPos + "の中に\"" + key + "\"の定義がありません");
            }
            return i;
        }
		
        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
			if (dic == null)
				dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index, varname);
			strTerm = strTerm.Restructure(exm);
			if (!(strTerm is SingleTerm))
				return this;
			return new SingleTerm(this.GetIntValue(exm));
        }
	}

}