using MinorShift.Emuera;
using MinorShift.Emuera.Sub;
using System;
using UnityEngine;

namespace MinorShift.Emuera.GameView
{
	internal static class HtmlUtils
	{
		public static T CreateIfNull<T>(ref T obj) where T : new()
		{
			if (obj == null) obj = new T();
			return obj;
		}

		public static int[] CreateIfNull(ref int[] obj, int len = 4)
		{
			if (obj == null) obj = new int[len];
			return obj;
		}

		public static bool TryParseStyledBoxModel(ref StyledBoxModel box, string tag, string word, string attrValue)
		{
			switch (word.ToLower())
			{
				case "border":
				case "border_width"://.NET版の名称
					if (CreateIfNull(ref box).border == null) box.border = new MixedNum[4];
					else throw new CodeEE(string.Format("<{0}>태그에 {1} 속성이 두 번 이상 지정되어 있습니다", tag, word));
					ParseParam4MixedNum(ref box.border, tag, word, attrValue);
					return true;
				case "radius":
					if (CreateIfNull(ref box).radius == null) box.radius = new MixedNum[4];
					else throw new CodeEE(string.Format("<{0}>태그에 {1} 속성이 두 번 이상 지정되어 있습니다", tag, word));
					ParseParam4MixedNum(ref box.radius, tag, word, attrValue);
					return true;
				case "margin":
					if (CreateIfNull(ref box).margin == null) box.margin = new MixedNum[4];
					else throw new CodeEE(string.Format("<{0}>태그에 {1} 속성이 두 번 이상 지정되어 있습니다", tag, word));
					ParseParam4MixedNum(ref box.margin, tag, word, attrValue);
					return true;
				case "padding":
					if (CreateIfNull(ref box).padding == null) box.padding = new MixedNum[4];
					else throw new CodeEE(string.Format("<{0}>태그에 {1} 속성이 두 번 이상 지정되어 있습니다", tag, word));
					ParseParam4MixedNum(ref box.padding, tag, word, attrValue);
					return true;
			}
			return false;
		}

		public static void ParseParam4MixedNum(ref MixedNum[] nums, string tag, string word, string attrValue)
		{
			string[] tokens = attrValue.Split(',');
			switch (tokens.Length)
			{
				case 1: // all
					ParseMixedNum(ref nums[0], tag, word, tokens[0].Trim());
					nums[1] = nums[0];
					nums[2] = nums[0];
					nums[3] = nums[0];
					break;
				case 2: // top and bottom | left and right
					ParseMixedNum(ref nums[0], tag, word, tokens[0].Trim());
					ParseMixedNum(ref nums[1], tag, word, tokens[1].Trim());
					nums[2] = nums[0];
					nums[3] = nums[1];
					break;
				case 3: //top | left and right | bottom
					ParseMixedNum(ref nums[0], tag, word, tokens[0].Trim());
					ParseMixedNum(ref nums[1], tag, word, tokens[1].Trim());
					ParseMixedNum(ref nums[2], tag, word, tokens[2].Trim());
					nums[3] = nums[1];
					break;
				case 4: // top | right | bottom | left
					for (int i = 0; i < 4; i++)
						ParseMixedNum(ref nums[i], tag, word, tokens[i].Trim());
					break;
				default:
					throw new CodeEE(string.Format("{0} 속성값을 해석할 수 없습니다", attrValue));
			}
		}

		public static void ParseMixedNum(ref MixedNum num, string tag, string word, string attrValue)
		{
			if (num == null) num = new MixedNum();
			else
				throw new CodeEE(string.Format("<{0}>태그에 {1} 속성이 두 번 이상 지정되어 있습니다", tag, word));
			
			attrValue = attrValue.Trim();
			
			if (attrValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
			{
				if (!int.TryParse(attrValue.Substring(0, attrValue.Length - 2).Trim(), out num.num))
					throw new CodeEE(string.Format("<{0}>태그의 {1} 속성값이 숫자로 해석되지 않습니다", tag, word));
				num.isPx = true;
			}
			else if (!int.TryParse(attrValue, out num.num))
				throw new CodeEE(string.Format("<{0}>태그의 {1} 속성값이 숫자로 해석되지 않습니다", tag, word));
		}

		public static void MixedNum4ToInt4(MixedNum[] mnums, ref int[] nums)
		{
			if (mnums != null)
			{
				CreateIfNull(ref nums);
				for (int i = 0; i < 4; i++)
				{
					if (mnums[i].isPx)
						nums[i] = mnums[i].num; // Unity uses px as internal unit
					else
						nums[i] = (int)((float)mnums[i].num * Config.FontSize / 100f);
				}
			}
		}

		public static int ToPixel(MixedNum num)
		{
			if (num == null) return 0;
			if (num.isPx) return num.num;
			return (int)((float)num.num * Config.FontSize / 100f);
		}

		public static void ParseParam4IntNum(ref int[] nums, string tag, string word, string attrValue)
		{
			if (nums == null) nums = new int[4];
			else throw new CodeEE(string.Format("<{0}>태그에 {1} 속성이 두 번 이상 지정되어 있습니다", tag, word));
			string[] tokens = attrValue.Split(',');
			switch (tokens.Length)
			{
				case 1: // all
					nums[0] = HtmlManager.stringToColorInt32(tokens[0].Trim());
					nums[1] = nums[0];
					nums[2] = nums[0];
					nums[3] = nums[0];
					break;
				case 2: // top and bottom | left and right
					nums[0] = HtmlManager.stringToColorInt32(tokens[0].Trim());
					nums[1] = HtmlManager.stringToColorInt32(tokens[1].Trim());
					nums[2] = nums[0];
					nums[3] = nums[1];
					break;
				case 3: //top | left and right | bottom
					nums[0] = HtmlManager.stringToColorInt32(tokens[0].Trim());
					nums[1] = HtmlManager.stringToColorInt32(tokens[1].Trim());
					nums[2] = HtmlManager.stringToColorInt32(tokens[2].Trim());
					nums[3] = nums[1];
					break;
				case 4:  // top | right | bottom | left
					for (int i = 0; i < 4; i++)
						nums[i] = HtmlManager.stringToColorInt32(tokens[i].Trim());
					break;
				default:
					throw new CodeEE(string.Format("{0} 속성값을 해석할 수 없습니다", attrValue));
			}
		}
	}
}
