using MinorShift._Library;
using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using uEmuera.Drawing;
//using System.Threading.Tasks;

namespace MinorShift.Emuera.Content
{
	internal sealed class GraphicsImage : AbstractImage
	{
		//public Bitmap Bitmap;
		//public IntPtr GDIhDC { get; protected set; }
		//protected Graphics g;
		//protected IntPtr hBitmap;
		//protected IntPtr hDefaultImg;

		public GraphicsImage(int id)
		{
			ID = id;
			//g = null;
			//Bitmap = null;
			//created = false;
			//locked = false;
		}
		public readonly int ID;
        //Size size;
        
        //Bitmap b;
        //Graphics g;


        ////bool created;
        ////bool locked;
        //public void LockGraphics()
        //{
        //	//if (locked)
        //	//	return;
        //	//g = Graphics.FromImage(b);
        //	//locked = true;
        //}
        //public void UnlockGraphics()
        //{
        //	//if (!locked)
        //	//	return;
        //	//g.Dispose();
        //	//g = null;
        //	//locked = false;
        //}

        #region Bitmap書き込み・作成

        /// <summary>
        /// GCREATE(int ID, int width, int height)
        /// Graphicsの基礎となるBitmapを作成する。エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GCreate(int x, int y, bool useGDI)
        {
            this.GDispose();
            is_created = true;
            width = x;
            height = y;
            string uniqueName = "GCREATE_" + ID + "_" + System.Guid.NewGuid().ToString("N");
            this.Bitmap = new uEmuera.Drawing.Bitmap(x, y, uniqueName);
        }

        internal void GCreateFromF(Bitmap bmp, bool useGDI)
        {
            this.GDispose();
            is_created = true;
            width = bmp.Width;
            height = bmp.Height;
            //ファイルから読んだテクスチャはファイル名単位でキャッシュを共有している。
            //GDRAWG等でそこへ直接書き込むとキャッシュ自体が書き換わってしまい、
            //GDISPOSEして読み直しても前回の描画結果が残り、呼ぶ度に劣化が累積する。
            //GDI番号ごとの専用テクスチャへ複製してから保持する
            this.Bitmap = CreatePrivateCopy(bmp);
        }

        /// <summary>
        /// 共有キャッシュを壊さないよう、このGDI番号専用のテクスチャへ内容を複製する。
        /// 大きさが同じなら既存のテクスチャを使い回すので番号あたり1枚に収まる
        /// </summary>
        Bitmap CreatePrivateCopy(Bitmap src)
        {
            string ownName = "GFROMFILE_" + ID;
            var copy = new Bitmap(ownName);
            copy.name = ownName;
            copy.size = new uEmuera.Drawing.Size(src.Width, src.Height);

            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    //セーブデータからの読み込み等、name未設定のBitmapが渡る事がある
                    string srcName = string.IsNullOrEmpty(src.name) ? src.path : src.name;
                    if (string.IsNullOrEmpty(srcName)) return;
                    var srcTi = SpriteManager.GetTextureInfo(srcName, src.path);
                    if (srcTi == null || srcTi.texture == null) return;
                    var srcTex = srcTi.texture;
                    var destTex = SpriteManager.GetOrCreateDynamicTexture(ownName, srcTex.width, srcTex.height);
                    if (destTex == null) return;
                    destTex.SetPixels32(srcTex.GetPixels32());
                    destTex.Apply(false, false);
                    copy.size = new uEmuera.Drawing.Size(srcTex.width, srcTex.height);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError("GCreateFromF copy error: " + e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();

            width = copy.Width;
            height = copy.Height;
            return copy;
        }

        /// <summary>
        /// GCLEAR(int ID, int cARGB)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GClear(uEmuera.Drawing.Color c)
        {
            if (this.Bitmap == null) return;
            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    if (destTi == null) return;
                    var destTex = destTi.texture;
                    int dW = destTex.width;
                    int dH = destTex.height;
                    UnityEngine.Color[] destPixels = new UnityEngine.Color[dW * dH];
                    UnityEngine.Color uc = new UnityEngine.Color(c.r, c.g, c.b, c.a);
                    for (int i = 0; i < destPixels.Length; i++) destPixels[i] = uc;
                    destTex.SetPixels(destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError(e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }

        public void GDrawString(string text, int x, int y)
        {
            if (this.Bitmap == null || string.IsNullOrEmpty(text)) return;
            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    if (destTi == null) return;
                    var destTex = destTi.texture;

                    // Setup RenderTexture
                    int rtWidth = destTex.width;
                    int rtHeight = destTex.height;
                    UnityEngine.RenderTexture rt = UnityEngine.RenderTexture.GetTemporary(rtWidth, rtHeight, 0, UnityEngine.RenderTextureFormat.ARGB32);
                    int layer = 31; // Isolate rendering
                    
                    // Setup Camera
                    UnityEngine.GameObject camObj = new UnityEngine.GameObject("TempCam");
                    camObj.layer = layer;
                    UnityEngine.Camera cam = camObj.AddComponent<UnityEngine.Camera>();
                    cam.orthographic = true;
                    cam.orthographicSize = rtHeight / 2f;
                    cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                    cam.backgroundColor = new UnityEngine.Color(0,0,0,0);
                    cam.targetTexture = rt;
                    cam.transform.position = new UnityEngine.Vector3(rtWidth / 2f, rtHeight / 2f, -10f);
                    cam.cullingMask = 1 << layer;

                    // Setup Canvas
                    UnityEngine.GameObject canvasObj = new UnityEngine.GameObject("TempCanvas");
                    canvasObj.layer = layer;
                    UnityEngine.Canvas canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
                    canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
                    var rtCanvas = canvas.GetComponent<UnityEngine.RectTransform>();
                    rtCanvas.sizeDelta = new UnityEngine.Vector2(rtWidth, rtHeight);
                    rtCanvas.position = new UnityEngine.Vector3(rtWidth / 2f, rtHeight / 2f, 0);

                    // Setup Text
                    UnityEngine.GameObject textObj = new UnityEngine.GameObject("TempText");
                    textObj.layer = layer;
                    textObj.transform.SetParent(canvasObj.transform, false);
                    UnityEngine.UI.Text uiText = textObj.AddComponent<UnityEngine.UI.Text>();
                    uiText.text = text;
                    
                    if (this.font != null && this.font.FontFamily != null) {
                        uiText.font = FontUtils.GetFont(this.font.FontFamily.Name);
                    }
                    if (uiText.font == null) uiText.font = FontUtils.default_font;
                    if (uiText.font == null) uiText.font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");

                    uiText.fontSize = this.font != null ? (int)this.font.Size : 18;
                    
                    if (this.brush != null && this.brush is uEmuera.Drawing.SolidBrush sb) {
                        uiText.color = new UnityEngine.Color(sb.Color.r, sb.Color.g, sb.Color.b, sb.Color.a);
                    } else {
                        uiText.color = new UnityEngine.Color(Config.ForeColor.r, Config.ForeColor.g, Config.ForeColor.b, Config.ForeColor.a);
                    }

                    uiText.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
                    uiText.verticalOverflow = UnityEngine.VerticalWrapMode.Overflow;
                    uiText.alignment = UnityEngine.TextAnchor.UpperLeft;
                    
                    var rtText = uiText.GetComponent<UnityEngine.RectTransform>();
                    rtText.sizeDelta = new UnityEngine.Vector2(rtWidth, rtHeight);
                    rtText.pivot = new UnityEngine.Vector2(0, 1);
                    rtText.anchorMin = new UnityEngine.Vector2(0, 1);
                    rtText.anchorMax = new UnityEngine.Vector2(0, 1);
                    rtText.anchoredPosition = new UnityEngine.Vector2(x, -y);

                    UnityEngine.Canvas.ForceUpdateCanvases();
                    uiText.SetAllDirty();
                    uiText.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
                    uiText.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);

                    // Render
                    UnityEngine.RenderTexture activeObj = UnityEngine.RenderTexture.active;
                    UnityEngine.RenderTexture.active = rt;
                    cam.Render();

                    // Read Pixels
                    UnityEngine.Texture2D tempTex = new UnityEngine.Texture2D(rtWidth, rtHeight, UnityEngine.TextureFormat.RGBA32, false);
                    tempTex.ReadPixels(new UnityEngine.Rect(0, 0, rtWidth, rtHeight), 0, 0);
                    tempTex.Apply(false, false);

                    UnityEngine.RenderTexture.active = activeObj;

                    // Blend tempTex onto destTex
                    UnityEngine.Color[] srcPixels = tempTex.GetPixels();
                    UnityEngine.Color[] destPixels = destTex.GetPixels();
                    for(int i = 0; i < srcPixels.Length; i++) {
                        UnityEngine.Color sC = srcPixels[i];
                        if (sC.a > 0) {
                            UnityEngine.Color dC = destPixels[i];
                            float outA = sC.a + dC.a * (1f - sC.a);
                            if (outA > 0f) {
                                destPixels[i] = new UnityEngine.Color(
                                    (sC.r * sC.a + dC.r * dC.a * (1f - sC.a)) / outA,
                                    (sC.g * sC.a + dC.g * dC.a * (1f - sC.a)) / outA,
                                    (sC.b * sC.a + dC.b * dC.a * (1f - sC.a)) / outA,
                                    outA
                                );
                            }
                        }
                    }
                    destTex.SetPixels(destPixels);
                    destTex.Apply(false, false);

                    // Cleanup
                    UnityEngine.Object.Destroy(tempTex);
                    UnityEngine.Object.Destroy(textObj);
                    UnityEngine.Object.Destroy(canvasObj);
                    UnityEngine.Object.Destroy(camObj);
                    UnityEngine.RenderTexture.ReleaseTemporary(rt);

                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError("GDrawString error: " + e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }

        public void GDrawString(string text, int x, int y, int width, int height)
        {
            // Map the bounding box version to the basic version for now.
            // Text clipping could be implemented via Mask, but simple overflow is usually sufficient.
            GDrawString(text, x, y);
        }

        public void GDrawRectangle(Rectangle rect)
        {
            if (this.Bitmap == null) return;
            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    if (destTi == null) return;
                    var destTex = destTi.texture;
                    int dX = rect.X;
                    int dW = rect.Width;
                    int dH = rect.Height;
                    int dY = rect.Y;
                    int unityDy = destTex.height - dY - dH;
                    
                    if (dX < 0) { dW += dX; dX = 0; }
                    if (unityDy < 0) { dH += unityDy; unityDy = 0; }
                    if (dX + dW > destTex.width) dW = destTex.width - dX;
                    if (unityDy + dH > destTex.height) dH = destTex.height - unityDy;
                    if (dW <= 0 || dH <= 0) return;

                    UnityEngine.Color[] destPixels = destTex.GetPixels(dX, unityDy, dW, dH);
                    UnityEngine.Color uc;
                    if (pen != null)
                        uc = new UnityEngine.Color(pen.Color.r, pen.Color.g, pen.Color.b, pen.Color.a);
                    else
                        uc = new UnityEngine.Color(Config.ForeColor.r, Config.ForeColor.g, Config.ForeColor.b, Config.ForeColor.a);
                    
                    int penWidth = pen != null ? (int)pen.Width : 1;
                    
                    for (int y = 0; y < dH; y++) {
                        for (int x = 0; x < dW; x++) {
                            if (x < penWidth || x >= dW - penWidth || y < penWidth || y >= dH - penWidth) {
                                UnityEngine.Color dC = destPixels[y * dW + x];
                                float outA = uc.a + dC.a * (1f - uc.a);
                                if (outA > 0f) {
                                    destPixels[y * dW + x] = new UnityEngine.Color(
                                        (uc.r * uc.a + dC.r * dC.a * (1f - uc.a)) / outA,
                                        (uc.g * uc.a + dC.g * dC.a * (1f - uc.a)) / outA,
                                        (uc.b * uc.a + dC.b * dC.a * (1f - uc.a)) / outA,
                                        outA
                                    );
                                }
                            }
                        }
                    }
                    destTex.SetPixels(dX, unityDy, dW, dH, destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError(e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }

        public void GFillRectangle(Rectangle rect)
        {
            if (this.Bitmap == null) return;
            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    if (destTi == null) return;
                    var destTex = destTi.texture;
                    int dX = rect.X;
                    int dW = rect.Width;
                    int dH = rect.Height;
                    int dY = rect.Y;
                    int unityDy = destTex.height - dY - dH;
                    
                    if (dX < 0) { dW += dX; dX = 0; }
                    if (unityDy < 0) { dH += unityDy; unityDy = 0; }
                    if (dX + dW > destTex.width) dW = destTex.width - dX;
                    if (unityDy + dH > destTex.height) dH = destTex.height - unityDy;
                    if (dW <= 0 || dH <= 0) return;

                    UnityEngine.Color[] destPixels = destTex.GetPixels(dX, unityDy, dW, dH);
                    UnityEngine.Color uc;
                    if (brush != null && brush is SolidBrush sb)
                        uc = new UnityEngine.Color(sb.Color.r, sb.Color.g, sb.Color.b, sb.Color.a);
                    else
                        uc = new UnityEngine.Color(Config.BackColor.r, Config.BackColor.g, Config.BackColor.b, Config.BackColor.a);
                    
                    for (int i = 0; i < destPixels.Length; i++) {
                        UnityEngine.Color dC = destPixels[i];
                        float outA = uc.a + dC.a * (1f - uc.a);
                        if (outA > 0f) {
                            destPixels[i] = new UnityEngine.Color(
                                (uc.r * uc.a + dC.r * dC.a * (1f - uc.a)) / outA,
                                (uc.g * uc.a + dC.g * dC.a * (1f - uc.a)) / outA,
                                (uc.b * uc.a + dC.b * dC.a * (1f - uc.a)) / outA,
                                outA
                            );
                        }
                    }

                    destTex.SetPixels(dX, unityDy, dW, dH, destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError(e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }

		/// <summary>
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight)
		/// エラーチェックは呼び出し元でのみ行う
		/// </summary>
		public void GDrawCImg(ASprite img, Rectangle destRect)
		{
			GDrawCImg(img, destRect, null);
		}

		/// <summary>
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight, float[][] cm)
		/// エラーチェックは呼び出し元でのみ行う
		/// </summary>
		public void GDrawCImg(ASprite img, Rectangle destRect, float[][] cm)
		{
            if (this.Bitmap == null || img == null || img.Bitmap == null) return;
            MinorShift.Emuera.Content.ASpriteSingle singleSprite = img as MinorShift.Emuera.Content.ASpriteSingle;
            if (singleSprite == null) return;

            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    var srcTi = SpriteManager.GetTextureInfo(singleSprite.Bitmap.name, singleSprite.Bitmap.path);
                    if (destTi == null || srcTi == null) return;

                    var destTex = destTi.texture;
                    var srcTex = srcTi.texture;
                    
                    int sX = singleSprite.SrcRectangle.X;
                    int sY = singleSprite.SrcRectangle.Y;
                    int sW = singleSprite.SrcRectangle.Width;
                    int sH = singleSprite.SrcRectangle.Height;
                    if (sW <= 0 || sH <= 0) return;
                    
                    int dX = destRect.X;
                    int dY = destRect.Y;
                    int dW = destRect.Width;
                    int dH = destRect.Height;
                    //ずらす位置が明示されている時だけ動かす。
                    //6列しかないcsvの行では、この値に切り出し元の座標が入っており、
                    //それを足すと台紙の外へ出て何も描かれない
                    if (singleSprite.HasExplicitPosition && !singleSprite.DestBasePosition.IsEmpty)
                    {
                        int baseW = singleSprite.DestBaseSize.Width > 0 ? singleSprite.DestBaseSize.Width : sW;
                        int baseH = singleSprite.DestBaseSize.Height > 0 ? singleSprite.DestBaseSize.Height : sH;
                        if (baseW > 0 && baseH > 0)
                        {
                            dX += singleSprite.DestBasePosition.X * dW / baseW;
                            dY += singleSprite.DestBasePosition.Y * dH / baseH;
                        }
                    }
                    if (dW <= 0 || dH <= 0) return;

                    // Clamp source rect to source texture bounds
                    if (sX < 0) { sW += sX; sX = 0; }
                    if (sY < 0) { sH += sY; sY = 0; }
                    if (sX + sW > srcTex.width) sW = srcTex.width - sX;
                    if (sY + sH > srcTex.height) sH = srcTex.height - sY;
                    if (sW <= 0 || sH <= 0) return;

                    // Clamp dest rect to dest texture bounds
                    if (dX < 0) { dW += dX; dX = 0; }
                    if (dY < 0) { dH += dY; dY = 0; }
                    if (dX + dW > destTex.width) dW = destTex.width - dX;
                    if (dY + dH > destTex.height) dH = destTex.height - dY;
                    if (dW <= 0 || dH <= 0) return;

                    int unitySy = srcTex.height - sY - sH;
                    if (unitySy < 0) { sH += unitySy; unitySy = 0; }
                    if (sH <= 0) return;
                    UnityEngine.Color[] srcPixels = srcTex.GetPixels(sX, unitySy, sW, sH);
                    
                    int unityDy = destTex.height - dY - dH;
                    if (unityDy < 0) { dH += unityDy; unityDy = 0; }
                    if (dH <= 0) return;
                    UnityEngine.Color[] destPixels = destTex.GetPixels(dX, unityDy, dW, dH);
                    
                    for(int y = 0; y < dH; y++)
                    {
                        for(int x = 0; x < dW; x++)
                        {
                            int sx = x * sW / dW;
                            int sy = y * sH / dH;
                            int srcIdx = sy * sW + sx;
                            int destIdx = y * dW + x;
                            if (srcIdx < 0 || srcIdx >= srcPixels.Length || destIdx < 0 || destIdx >= destPixels.Length) continue;
                            
                            UnityEngine.Color sC = srcPixels[srcIdx];
                            if (cm != null)
                            {
                                float r = sC.r * cm[0][0] + sC.g * cm[1][0] + sC.b * cm[2][0] + sC.a * cm[3][0] + cm[4][0];
                                float g = sC.r * cm[0][1] + sC.g * cm[1][1] + sC.b * cm[2][1] + sC.a * cm[3][1] + cm[4][1];
                                float b = sC.r * cm[0][2] + sC.g * cm[1][2] + sC.b * cm[2][2] + sC.a * cm[3][2] + cm[4][2];
                                float a = sC.r * cm[0][3] + sC.g * cm[1][3] + sC.b * cm[2][3] + sC.a * cm[3][3] + cm[4][3];
                                sC = new UnityEngine.Color(r, g, b, UnityEngine.Mathf.Clamp01(a));
                            }
                            
                            UnityEngine.Color dC = destPixels[destIdx];
                            float outA = sC.a + dC.a * (1f - sC.a);
                            if (outA > 0f)
                            {
                                destPixels[destIdx] = new UnityEngine.Color(
                                    (sC.r * sC.a + dC.r * dC.a * (1f - sC.a)) / outA,
                                    (sC.g * sC.a + dC.g * dC.a * (1f - sC.a)) / outA,
                                    (sC.b * sC.a + dC.b * dC.a * (1f - sC.a)) / outA,
                                    outA
                                );
                            }
                        }
                    }
                    destTex.SetPixels(dX, unityDy, dW, dH, destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError(e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
		}

        /// <summary>
        /// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect)
        {
            GDrawG(srcGra, destRect, srcRect, null);
        }


        /// <summary>
        /// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight, float[][] cm)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect, float[][] cm)
        {
            if (this.Bitmap == null || srcGra == null || srcGra.Bitmap == null) return;

            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    var srcTi = SpriteManager.GetTextureInfo(srcGra.Bitmap.name, srcGra.Bitmap.path);
                    if (destTi == null || srcTi == null) return;

                    var destTex = destTi.texture;
                    var srcTex = srcTi.texture;

                    int sX = srcRect.X;
                    int sY = srcRect.Y;
                    int sW = srcRect.Width;
                    int sH = srcRect.Height;

                    // Clamp source rect to source texture bounds
                    if (sX < 0) { sW += sX; sX = 0; }
                    if (sY < 0) { sH += sY; sY = 0; }
                    if (sX + sW > srcTex.width) sW = srcTex.width - sX;
                    if (sY + sH > srcTex.height) sH = srcTex.height - sY;
                    if (sW <= 0 || sH <= 0) return;

                    int dX = destRect.X;
                    int dY = destRect.Y;
                    int dW = destRect.Width;
                    int dH = destRect.Height;

                    // Clamp dest rect to dest texture bounds
                    if (dX < 0) { dW += dX; dX = 0; }
                    if (dY < 0) { dH += dY; dY = 0; }
                    if (dX + dW > destTex.width) dW = destTex.width - dX;
                    if (dY + dH > destTex.height) dH = destTex.height - dY;
                    if (dW <= 0 || dH <= 0) return;

                    // Unity Y is flipped (bottom-up)
                    int unitySy = srcTex.height - sY - sH;
                    if (unitySy < 0) { sH += unitySy; unitySy = 0; }
                    if (sH <= 0) return;
                    UnityEngine.Color[] srcPixels = srcTex.GetPixels(sX, unitySy, sW, sH);

                    int unityDy = destTex.height - dY - dH;
                    if (unityDy < 0) { dH += unityDy; unityDy = 0; }
                    if (dH <= 0) return;
                    UnityEngine.Color[] destPixels = destTex.GetPixels(dX, unityDy, dW, dH);

                    for (int y = 0; y < dH; y++)
                    {
                        for (int x = 0; x < dW; x++)
                        {
                            int sx = x * sW / dW;
                            int sy = y * sH / dH;
                            int srcIdx = sy * sW + sx;
                            int destIdx = y * dW + x;

                            if (srcIdx < 0 || srcIdx >= srcPixels.Length) continue;

                            UnityEngine.Color sC = srcPixels[srcIdx];

                            // Apply ColorMatrix if provided
                            if (cm != null)
                            {
                                float r = sC.r * cm[0][0] + sC.g * cm[1][0] + sC.b * cm[2][0] + sC.a * cm[3][0] + cm[4][0];
                                float g = sC.r * cm[0][1] + sC.g * cm[1][1] + sC.b * cm[2][1] + sC.a * cm[3][1] + cm[4][1];
                                float b = sC.r * cm[0][2] + sC.g * cm[1][2] + sC.b * cm[2][2] + sC.a * cm[3][2] + cm[4][2];
                                float a = sC.r * cm[0][3] + sC.g * cm[1][3] + sC.b * cm[2][3] + sC.a * cm[3][3] + cm[4][3];
                                sC = new UnityEngine.Color(
                                    UnityEngine.Mathf.Clamp01(r),
                                    UnityEngine.Mathf.Clamp01(g),
                                    UnityEngine.Mathf.Clamp01(b),
                                    UnityEngine.Mathf.Clamp01(a)
                                );
                            }

                            // Alpha composite (Porter-Duff "over")
                            UnityEngine.Color dC = destPixels[destIdx];
                            float outA = sC.a + dC.a * (1f - sC.a);
                            if (outA > 0f)
                            {
                                destPixels[destIdx] = new UnityEngine.Color(
                                    (sC.r * sC.a + dC.r * dC.a * (1f - sC.a)) / outA,
                                    (sC.g * sC.a + dC.g * dC.a * (1f - sC.a)) / outA,
                                    (sC.b * sC.a + dC.b * dC.a * (1f - sC.a)) / outA,
                                    outA
                                );
                            }
                        }
                    }
                    destTex.SetPixels(dX, unityDy, dW, dH, destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError("GDrawG error: " + e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }


        /// <summary>
        /// GDRAWGWITHMASK(int ID, int srcID, int maskID, int destX, int destY)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
                public void GDrawGWithRotate(GraphicsImage srcGra, float angle, int destX, int destY)
        {
            if (this.Bitmap == null || srcGra == null || srcGra.Bitmap == null) return;

            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    var srcTi = SpriteManager.GetTextureInfo(srcGra.Bitmap.name, srcGra.Bitmap.path);
                    if (destTi == null || srcTi == null) return;

                    var destTex = destTi.texture;
                    var srcTex = srcTi.texture;

                    int sW = srcTex.width;
                    int sH = srcTex.height;
                    UnityEngine.Color[] srcPixels = srcTex.GetPixels(); // Whole image

                    float cos = UnityEngine.Mathf.Cos(angle);
                    float sin = UnityEngine.Mathf.Sin(angle);

                    // Bounding box of the rotated image
                    float hw = sW / 2f;
                    float hh = sH / 2f;

                    float p1x = -hw * cos - -hh * sin; float p1y = -hw * sin + -hh * cos;
                    float p2x =  hw * cos - -hh * sin; float p2y =  hw * sin + -hh * cos;
                    float p3x = -hw * cos -  hh * sin; float p3y = -hw * sin +  hh * cos;
                    float p4x =  hw * cos -  hh * sin; float p4y =  hw * sin +  hh * cos;

                    int minX = UnityEngine.Mathf.FloorToInt(UnityEngine.Mathf.Min(p1x, p2x, p3x, p4x));
                    int maxX = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(p1x, p2x, p3x, p4x));
                    int minY = UnityEngine.Mathf.FloorToInt(UnityEngine.Mathf.Min(p1y, p2y, p3y, p4y));
                    int maxY = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(p1y, p2y, p3y, p4y));

                    int dW = maxX - minX;
                    int dH = maxY - minY;

                    // destX and destY are the center point on the destination image
                    int destStartX = destX + minX;
                    int destStartY = destY + minY;

                    // Unity Y is flipped (bottom-up)
                    int unityDy = destTex.height - destStartY - dH;
                    
                    // Clamp to dest bounds
                    int dX = destStartX;
                    if (dX < 0) { dW += dX; minX -= dX; dX = 0; }
                    if (unityDy < 0) { dH += unityDy; minY += unityDy; unityDy = 0; } // Note: Since Unity Y is inverted, minY logic is flipped, but we calculate per pixel anyway
                    
                    if (dX + dW > destTex.width) dW = destTex.width - dX;
                    if (unityDy + dH > destTex.height) dH = destTex.height - unityDy;
                    
                    if (dW <= 0 || dH <= 0) return;

                    UnityEngine.Color[] destPixels = destTex.GetPixels(dX, unityDy, dW, dH);

                    // To map correctly, we iterate over the destination bounding box
                    for (int y = 0; y < dH; y++)
                    {
                        // Current y in destination coordinates relative to the center of rotation
                        // Wait, unityDy corresponds to bottom-up. So the actual Y in top-down is destTex.height - (unityDy + y) - 1
                        int actualDestY = destTex.height - (unityDy + y) - 1;
                        float relY = actualDestY - destY; // relative to center point

                        for (int x = 0; x < dW; x++)
                        {
                            int actualDestX = dX + x;
                            float relX = actualDestX - destX;

                            // Inverse rotation to find source pixel
                            float srcRelX = relX * cos + relY * sin;
                            float srcRelY = -relX * sin + relY * cos;

                            int sX = UnityEngine.Mathf.FloorToInt(srcRelX + hw);
                            int sY = UnityEngine.Mathf.FloorToInt(srcRelY + hh);

                            if (sX >= 0 && sX < sW && sY >= 0 && sY < sH)
                            {
                                // Unity source Y is bottom-up
                                int unitySy = srcTex.height - sY - 1;
                                int srcIdx = unitySy * sW + sX;
                                int destIdx = y * dW + x;

                                UnityEngine.Color sC = srcPixels[srcIdx];
                                UnityEngine.Color dC = destPixels[destIdx];

                                float outA = sC.a + dC.a * (1f - sC.a);
                                if (outA > 0f)
                                {
                                    destPixels[destIdx] = new UnityEngine.Color(
                                        (sC.r * sC.a + dC.r * dC.a * (1f - sC.a)) / outA,
                                        (sC.g * sC.a + dC.g * dC.a * (1f - sC.a)) / outA,
                                        (sC.b * sC.a + dC.b * dC.a * (1f - sC.a)) / outA,
                                        outA
                                    );
                                }
                            }
                        }
                    }
                    destTex.SetPixels(dX, unityDy, dW, dH, destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError("GDrawGWithRotate error: " + e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }

		public void GDrawGWithMask(GraphicsImage srcGra, GraphicsImage maskGra, Point destPoint)
        {
            if (this.Bitmap == null || srcGra == null || srcGra.Bitmap == null ||
                maskGra == null || maskGra.Bitmap == null) return;

            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() => {
                try {
                    var destTi = SpriteManager.GetTextureInfo(this.Bitmap.name, this.Bitmap.path);
                    var srcTi = SpriteManager.GetTextureInfo(srcGra.Bitmap.name, srcGra.Bitmap.path);
                    var maskTi = SpriteManager.GetTextureInfo(maskGra.Bitmap.name, maskGra.Bitmap.path);
                    if (destTi == null || srcTi == null || maskTi == null) return;

                    var destTex = destTi.texture;
                    var srcTex = srcTi.texture;
                    var maskTex = maskTi.texture;

                    //srcの全体をdestPointへ転送する。srcとmaskは同じ大きさである事を呼び出し元が保証している
                    int w = System.Math.Min(srcTex.width, maskTex.width);
                    int h = System.Math.Min(srcTex.height, maskTex.height);

                    //転送元の切り出し開始位置(左上基準)
                    int sxOff = 0;
                    int syOff = 0;
                    int dX = destPoint.X;
                    int dY = destPoint.Y;
                    if (dX < 0) { sxOff = -dX; w += dX; dX = 0; }
                    if (dY < 0) { syOff = -dY; h += dY; dY = 0; }
                    if (dX + w > destTex.width) w = destTex.width - dX;
                    if (dY + h > destTex.height) h = destTex.height - dY;
                    if (w <= 0 || h <= 0) return;

                    // UnityのYは下から上なので、上端基準の座標を変換する
                    int unitySy = srcTex.height - syOff - h;
                    int unityMy = maskTex.height - syOff - h;
                    int unityDy = destTex.height - dY - h;
                    if (unitySy < 0 || unityMy < 0 || unityDy < 0) return;

                    UnityEngine.Color[] srcPixels = srcTex.GetPixels(sxOff, unitySy, w, h);
                    UnityEngine.Color[] maskPixels = maskTex.GetPixels(sxOff, unityMy, w, h);
                    UnityEngine.Color[] destPixels = destTex.GetPixels(dX, unityDy, w, h);

                    for (int i = 0; i < destPixels.Length; i++)
                    {
                        //原作はマスク画像のB成分をそのまま不透明度として使う
                        float m = maskPixels[i].b;
                        if (m <= 0f)        //完全透明
                            continue;
                        UnityEngine.Color sC = srcPixels[i];
                        if (m >= 1f)        //完全不透明
                        {
                            destPixels[i] = sC;
                            continue;
                        }
                        //半透明。アルファ合成ではなく各成分の線形補間
                        UnityEngine.Color dC = destPixels[i];
                        float inv = 1f - m;
                        destPixels[i] = new UnityEngine.Color(
                            sC.r * m + dC.r * inv,
                            sC.g * m + dC.g * inv,
                            sC.b * m + dC.b * inv,
                            sC.a * m + dC.a * inv
                        );
                    }
                    destTex.SetPixels(dX, unityDy, w, h, destPixels);
                    destTex.Apply(false, false);
                } catch(System.Exception e) {
                    UnityEngine.Debug.LogError("GDrawGWithMask error: " + e);
                } finally {
                    waitHandle.Set();
                }
            });
            waitHandle.WaitOne();
        }

        
        public Brush brush = null;
        public Pen pen = null;
        public uEmuera.Drawing.Font font = null;

        public void GSetFont(uEmuera.Drawing.Font r)
        {
            if (font != null) font.Dispose();
            font = r;
        }
        public void GSetBrush(Brush r)
        {
            brush = r;
        }
        public void GSetPen(Pen r)
        {
            // Pen does not implement IDisposable in our Drawing shim
            pen = r;
        }

        //private static byte[] BytesFromBitmap(Bitmap bmp)
        //{
        //	BitmapData bmpData = bmp.LockBits(
        //	  new Rectangle(0, 0, bmp.Width, bmp.Height),
        //	  ImageLockMode.ReadOnly,  // 書き込むときはReadAndWriteで
        //	  PixelFormat.Format32bppArgb
        //	);
        //	if (bmpData.Stride < 0)
        //		throw new Exception();//変な形式のが送られてくることはありえないはずだが一応
        //	byte[] pixels = new byte[bmpData.Stride * bmp.Height];
        //	try
        //	{ 
        //		IntPtr ptr = bmpData.Scan0;
        //		System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
        //	}
        //	finally
        //	{
        //		bmp.UnlockBits(bmpData);

        //	}
        //	return pixels;
        //}

        /// <summary>
        /// GTOARRAY int ID, var array
        /// エラーチェックは呼び出し元でのみ行う
        /// <returns></returns>
        //public bool GBitmapToInt64Array(Int64[,] array, int xstart, int ystart)
        //{
        //	if (g == null || Bitmap == null)
        //		throw new NullReferenceException();
        //	int w = Bitmap.Width;
        //	int h = Bitmap.Height;
        //	if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
        //		return false;
        //	Rectangle rect = new Rectangle(0, 0, w, h);
        //	System.Drawing.Imaging.BitmapData bmpData =
        //		Bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
        //		PixelFormat.Format32bppArgb);
        //	IntPtr ptr = bmpData.Scan0;
        //	byte[] rgbValues = new byte[w * h * 4];
        //	Marshal.Copy(ptr, rgbValues, 0, rgbValues.Length);
        //	Bitmap.UnlockBits(bmpData);
        //	int i = 0;
        //	for (int y = 0; y < h; y++)
        //	{
        //		for (int x = 0; x < w; x++)
        //		{
        //			array[x + xstart, y + ystart] =
        //			rgbValues[i++] + //B
        //			(((Int64)rgbValues[i++]) << 8) + //G
        //			(((Int64)rgbValues[i++]) << 16) + //R
        //			(((Int64)rgbValues[i++]) << 24);  //A
        //		}
        //	}
        //	return true;
        //}


        /// <summary>
        /// GFROMARRAY int ID, var array
        /// エラーチェックは呼び出し元でのみ行う
        /// <returns></returns>
        //public bool GByteArrayToBitmap(Int64[,] array, int xstart, int ystart)
        //{
        //	if (g == null || Bitmap == null)
        //		throw new NullReferenceException();
        //	int w = Bitmap.Width;
        //	int h = Bitmap.Height;
        //	if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
        //		return false;

        //	byte[] rgbValues = new byte[w * h * 4];
        //	int i = 0;
        //	for (int y = 0; y < h; y++)
        //	{
        //		for (int x = 0; x < w; x++)
        //		{
        //			Int64 c = array[x + xstart, y + ystart];
        //			rgbValues[i++] = (byte)(c & 0xFF);//B
        //			rgbValues[i++] = (byte)((c >> 8) & 0xFF);//G
        //			rgbValues[i++] = (byte)((c >> 16) & 0xFF);//R
        //			rgbValues[i++] = (byte)((c >> 24) & 0xFF);//A
        //		}
        //	}
        //	Rectangle rect = new Rectangle(0, 0, w, h);
        //	System.Drawing.Imaging.BitmapData bmpData =
        //		Bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
        //		PixelFormat.Format32bppArgb);
        //	IntPtr ptr = bmpData.Scan0;
        //	Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
        //	Bitmap.UnlockBits(bmpData);
        //	return true;
        //}
        #endregion
        #region Bitmap読み込み・削除
        /// <summary>
        /// 未作成ならエラー
        /// </summary>
        //public Bitmap GetBitmap()
        //{
        //	if (Bitmap == null)
        //		throw new NullReferenceException();
        //	//UnlockGraphics();
        //	return Bitmap;
        //}
        /// <summary>
        /// GSETCOLOR(int ID, int cARGB, int x, int y)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GSetColor(uEmuera.Drawing.Color c, int x, int y)
        {
        	if (Bitmap == null)
        		throw new NullReferenceException();
            //	//UnlockGraphics();
            //	Bitmap.SetPixel(x, y, c);
        }

        /// <summary>
        /// GGETCOLOR(int ID, int x, int y)
        /// エラーチェックは呼び出し元でのみ行う。特に画像範囲内であるかどうかチェックすること
        /// </summary>
        public uEmuera.Drawing.Color GGetColor(int x, int y)
        {
        	if (Bitmap == null)
        		throw new NullReferenceException();
        	//UnlockGraphics();
        	return Bitmap.GetPixel(x, y);
        }


        /// <summary>
        /// GDISPOSE(int ID)
        /// </summary>
        public void GDispose()
        {
            is_created = false;
            width = 0;
            height = 0;
            this.Bitmap = null;
        }

        public override void Dispose()
        {
            //	this.GDispose();
            //if(render_texture != null)
            //{
            //    UnityEngine.Object.Destroy(render_texture);
            //}
            this.GDispose();
        }

        ~GraphicsImage()
        {
            Dispose();
        }
        #endregion

//#region 状態判定（Bitmap読み書きを伴わない）
        //public override bool IsCreated { get { return g != null; } }
        public override bool IsCreated { get { return is_created; } }
        bool is_created = false;

        /// <summary>
        /// int GWIDTH(int ID)
        /// </summary>
        public int Width { get { return width; } }
        int width = 0;
		/// <summary>
		/// int GHEIGHT(int ID)
		/// </summary>
		public int Height { get { return height; } }
        int height = 0;
        //#endregion
	}
}
