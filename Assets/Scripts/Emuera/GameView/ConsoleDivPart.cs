using System;
using System.Text;
using uEmuera.Drawing;
using MinorShift.Emuera.GameView;

namespace MinorShift.Emuera.GameView
{
	/// <summary>
	/// divのdisplay属性。縦の基準線が変わる
	/// </summary>
	internal enum DivAnchor
	{
		/// <summary>現在行からの相対位置</summary>
		Relative = 0,
		/// <summary>画面上端が基準(absolute-lefttop)</summary>
		AbsoluteTop = 1,
		/// <summary>画面下端が基準(absolute-leftbottom)</summary>
		AbsoluteBottom = 2,
	}

	internal class ConsoleDivPart : AConsoleDisplayPart
	{
		public DivAnchor Anchor = DivAnchor.Relative;
		/// <summary>内容から求めた表示上の幅。Widthはテキストの流れ用で常に0</summary>
		public int ContentWidth;
		public ConsoleDivPart(MixedNum xPos, MixedNum yPos, MixedNum width, MixedNum height, int depth, int color, StyledBoxModel box, bool isRelative, ConsoleDisplayLine[] childs)
		{
			backgroundColor = color >= 0 ? Color.FromArgb((int)(color | 0xff000000)) : Color.Transparent;
			width.num = Math.Abs(width.num);
			height.num = Math.Abs(height.num);

			if (box != null)
			{
				HtmlUtils.MixedNum4ToInt4(box.margin, ref margin);
				HtmlUtils.MixedNum4ToInt4(box.padding, ref padding);
				HtmlUtils.MixedNum4ToInt4(box.border, ref border);
				HtmlUtils.MixedNum4ToInt4(box.radius, ref radius);
				if (box.color != null)
				{
					borderColors = new Color[4];
					for (int i = 0; i < 4; i++)
						borderColors[i] = box.color[i] >= 0 ? Color.FromArgb((int)(box.color[i] | 0xff000000)) : Color.Transparent;
				}
			}
			Str = string.Empty;
			xOffset = HtmlUtils.ToPixel(xPos);

			if (margin != null) divXOffset += margin[0]; // Left
			if (padding != null) divXOffset += padding[0]; // Left
			if (border != null) divXOffset += border[0]; // Left

			PointY = HtmlUtils.ToPixel(yPos);

			if (margin != null) yOffset += margin[1]; // Top
			if (padding != null) yOffset += padding[1]; // Top
			if (border != null) yOffset += border[1]; // Top

			this.width = HtmlUtils.ToPixel(width);
			Height = HtmlUtils.ToPixel(height);
			children = childs;
			Depth = depth;
			IsRelative = isRelative;

			ShiftChildrenX(PointX + divXOffset);
			if (xPos != null)
				PointXisLocked = true;
		}

		int pointX;
		public int xOffset;
		public int divXOffset;
		public int yOffset;
		public int width;

		public override int PointX
		{
			get { return pointX; }
			set
			{
				var diff = value - pointX;
				pointX = value;
				ShiftChildrenX(diff);
			}
		}

		public int PointY;
		public int Height;
		public int[] margin, padding, radius, border;
		public Color[] borderColors;
		public Color backgroundColor;
		string altHeadTag;
		readonly ConsoleDisplayLine[] children;
		public bool IsEscaped { get; set; }
		public override int Top { get { return PointY; } }
		public override int Bottom { get { return PointY + Height; } }
		public bool IsRelative { get; private set; }
		public bool PointXisLocked { get; private set; }
		public ConsoleDisplayLine[] Children { get { return children; } }

		public override bool CanDivide { get { return false; } }

		private void ShiftChildrenX(int diff)
		{
			if (children != null)
			{
				foreach (var child in children)
					child.ShiftPositionX(diff);
			}
		}

		public override void SetWidth(StringMeasure sm, float subPixel)
		{
			XsubPixel = 0;
			ContentWidth = this.width;

			if (children != null)
			{
				foreach (var line in children)
				{
					if (line == null || line.Buttons == null) continue;
					
					// we must include divXOffset in our starting coordinate so that the shift is preserved!
					int currentX = pointX + divXOffset;
					float currentSubPixel = 0.5f;
					foreach (var btn in line.Buttons)
					{
						if (btn == null)
						{
							currentX = pointX + divXOffset;
							continue;
						}
						btn.CalcWidth(sm, currentSubPixel);
						btn.CalcPointX(currentX);
						if (!btn.IsAbsolutePositioned)
						{
							currentX = btn.PointX + btn.Width;
							if (btn.PointXisLocked)
								currentSubPixel = 0;
							else
								currentSubPixel = btn.XsubPixel;
						}
						else
						{
							currentSubPixel = 0;
						}
					}
					if (this.width <= 0)
					{
						//幅未指定のdivは内容から求める。当たり判定や描画で使う
						int lineWidth = currentX - (pointX + divXOffset);
						if (lineWidth > ContentWidth)
							ContentWidth = lineWidth;
					}
				}
			}

			//depth指定のdivは別レイヤーへの重ね描画。表示領域はERB側が空白で確保するので、
			//テキストの流れに幅を占有させると後続が二重に押し出される。
			//depthなしのdivは前の要素に続けて並ぶため、幅を持たせないと同じ位置に重なる
			Width = Depth != 0 ? 0 : ContentWidth;
		}

		public override string ToString()
		{
			return "<div>...</div>";
		}

		public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
		{
			// Unity handles rendering via EmueraContent
		}

		public override void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog)
		{
			// Unity handles rendering via EmueraContent
		}
	}
}
