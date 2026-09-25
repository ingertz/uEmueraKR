using System;

namespace uEmuera
{
    /// <summary>
    /// UnityのTexture2D.LoadImageが読めないBMP/GIFを復号する。
    ///
    /// LoadImageはPNGとJPGしか扱えず、それ以外は失敗して画像が出ない。
    /// 古いeraゲームはBMPを多用しており、本家(GDI+)では表示されるので、ここで補う。
    /// GIFは本家と同じく最初のフレームだけを使う。
    ///
    /// 出力はRGBA32のバイト列で、行は下から上の順(Unityのテクスチャと同じ並び)。
    /// Unityの型に依存しないので単体でテストできる
    /// </summary>
    public static class LegacyImageDecoder
    {
        /// <summary>
        /// 1枚の上限画素数(8192x8192)。ヘッダが壊れていて巨大な値が入っていた時に、
        /// 何GBも確保しようとして端末ごと落ちるのを防ぐ
        /// </summary>
        const long MaxPixels = 8192L * 8192L;

        /// <summary>先頭のバイトからBMP/GIFか判定する。拡張子は信用しない</summary>
        public static bool CanDecode(byte[] data)
        {
            return IsBmp(data) || IsGif(data);
        }

        public static bool IsBmp(byte[] d)
        {
            return d != null && d.Length >= 26 && d[0] == 'B' && d[1] == 'M';
        }

        public static bool IsGif(byte[] d)
        {
            return d != null && d.Length >= 13 && d[0] == 'G' && d[1] == 'I' && d[2] == 'F' && d[3] == '8';
        }

        /// <summary>
        /// 復号できなければfalse。rgbaは width*height*4 バイト、行は下から上
        /// </summary>
        public static bool TryDecode(byte[] data, out int width, out int height, out byte[] rgba)
        {
            width = height = 0;
            rgba = null;
            try
            {
                if (IsBmp(data))
                    return DecodeBmp(data, out width, out height, out rgba);
                if (IsGif(data))
                    return DecodeGif(data, out width, out height, out rgba);
            }
            catch (Exception)
            {
                //壊れたファイルで例外が出てもゲームは止めない。画像が出ないだけにする
            }
            width = height = 0;
            rgba = null;
            return false;
        }

        #region BMP

        static int U16(byte[] d, int p) { return d[p] | (d[p + 1] << 8); }
        static int S32(byte[] d, int p) { return d[p] | (d[p + 1] << 8) | (d[p + 2] << 16) | (d[p + 3] << 24); }
        static uint U32(byte[] d, int p) { return (uint)S32(d, p); }

        const int BI_RGB = 0;
        const int BI_RLE8 = 1;
        const int BI_RLE4 = 2;
        const int BI_BITFIELDS = 3;
        const int BI_ALPHABITFIELDS = 6;

        static bool DecodeBmp(byte[] d, out int width, out int height, out byte[] rgba)
        {
            width = height = 0;
            rgba = null;

            int pixelOffset = S32(d, 10);
            int headerSize = S32(d, 14);
            int w, h, bpp, compression = BI_RGB, colorsUsed = 0;
            int paletteEntrySize;
            if (headerSize == 12)
            {
                //OS/2 BITMAPCOREHEADER
                w = U16(d, 18);
                h = (short)U16(d, 20);
                bpp = U16(d, 24);
                paletteEntrySize = 3;
            }
            else if (headerSize >= 40)
            {
                w = S32(d, 18);
                h = S32(d, 22);
                bpp = U16(d, 28);
                compression = S32(d, 30);
                colorsUsed = S32(d, 46);
                paletteEntrySize = 4;
            }
            else
                return false;

            bool topDown = h < 0;
            if (topDown)
                h = -h;
            if (w <= 0 || h <= 0 || w > 16384 || h > 16384 || (long)w * h > MaxPixels)
                return false;

            //ビットマスク。BITFIELDSはヘッダ直後(40バイト版)かV4/V5ヘッダ内にある
            uint rMask = 0, gMask = 0, bMask = 0, aMask = 0;
            if (compression == BI_BITFIELDS || compression == BI_ALPHABITFIELDS)
            {
                int mp = 14 + 40;
                rMask = U32(d, mp);
                gMask = U32(d, mp + 4);
                bMask = U32(d, mp + 8);
                if (compression == BI_ALPHABITFIELDS || headerSize >= 56)
                    aMask = U32(d, mp + 12);
            }
            else if (bpp == 16)
            {
                rMask = 0x7C00; gMask = 0x03E0; bMask = 0x001F;
            }

            //パレット
            byte[] palette = null;
            int paletteCount = 0;
            if (bpp <= 8)
            {
                paletteCount = colorsUsed > 0 ? colorsUsed : (1 << bpp);
                int paletteStart = 14 + headerSize;
                if (compression == BI_BITFIELDS && headerSize == 40)
                    paletteStart += 12;
                paletteCount = Math.Min(paletteCount, 256);
                palette = new byte[256 * 3];
                for (int i = 0; i < paletteCount; ++i)
                {
                    int p = paletteStart + i * paletteEntrySize;
                    if (p + 2 >= d.Length)
                        break;
                    palette[i * 3 + 0] = d[p + 2];
                    palette[i * 3 + 1] = d[p + 1];
                    palette[i * 3 + 2] = d[p + 0];
                }
            }

            //行の並びを揃えるため、ファイル上の行番号→出力の行番号(下から数える)
            //ファイルは通常下から上。top-downなら上から下
            byte[] outBuf = new byte[w * h * 4];

            if (compression == BI_RLE8 || compression == BI_RLE4)
            {
                if (palette == null)
                    return false;
                //RLEの未描画部分は透明扱い(GDI+と同じ)
                DecodeRle(d, pixelOffset, w, h, compression == BI_RLE4, palette, outBuf);
                if (topDown)
                    FlipRows(outBuf, w, h);
                width = w; height = h; rgba = outBuf;
                return true;
            }
            if (compression != BI_RGB && compression != BI_BITFIELDS && compression != BI_ALPHABITFIELDS)
                return false;

            int stride = ((w * bpp + 31) / 32) * 4;
            //途中で切れたファイル。全行分のデータが無ければ諦める
            if (pixelOffset < 0 || (long)pixelOffset + (long)stride * h > d.Length)
                return false;
            int rShift = Shift(rMask), gShift = Shift(gMask), bShift = Shift(bMask), aShift = Shift(aMask);
            int rBits = Bits(rMask), gBits = Bits(gMask), bBits = Bits(bMask), aBits = Bits(aMask);

            for (int row = 0; row < h; ++row)
            {
                int src = pixelOffset + row * stride;
                //Unityは下の行から。ファイルがbottom-upならそのまま、top-downなら逆
                int dstRow = topDown ? (h - 1 - row) : row;
                int dst = dstRow * w * 4;
                for (int x = 0; x < w; ++x, dst += 4)
                {
                    byte r, g, b, a = 255;
                    switch (bpp)
                    {
                        case 1:
                        case 2:
                        case 4:
                        case 8:
                        {
                            int bitPos = x * bpp;
                            int v = d[src + (bitPos >> 3)];
                            int shift = 8 - bpp - (bitPos & 7);
                            int idx = (v >> shift) & ((1 << bpp) - 1);
                            r = palette[idx * 3]; g = palette[idx * 3 + 1]; b = palette[idx * 3 + 2];
                            break;
                        }
                        case 16:
                        {
                            uint v = (uint)U16(d, src + x * 2);
                            r = Scale(v, rMask, rShift, rBits);
                            g = Scale(v, gMask, gShift, gBits);
                            b = Scale(v, bMask, bShift, bBits);
                            if (aMask != 0) a = Scale(v, aMask, aShift, aBits);
                            break;
                        }
                        case 24:
                        {
                            int p = src + x * 3;
                            b = d[p]; g = d[p + 1]; r = d[p + 2];
                            break;
                        }
                        case 32:
                        {
                            int p = src + x * 4;
                            if (compression == BI_RGB)
                            {
                                //GDI+はBI_RGBの32bitをアルファ無しとして読む。本家に合わせて不透明にする
                                b = d[p]; g = d[p + 1]; r = d[p + 2];
                            }
                            else
                            {
                                uint v = U32(d, p);
                                r = Scale(v, rMask, rShift, rBits);
                                g = Scale(v, gMask, gShift, gBits);
                                b = Scale(v, bMask, bShift, bBits);
                                if (aMask != 0) a = Scale(v, aMask, aShift, aBits);
                            }
                            break;
                        }
                        default:
                            return false;
                    }
                    outBuf[dst] = r; outBuf[dst + 1] = g; outBuf[dst + 2] = b; outBuf[dst + 3] = a;
                }
            }
            width = w; height = h; rgba = outBuf;
            return true;
        }

        static int Shift(uint mask)
        {
            if (mask == 0) return 0;
            int s = 0;
            while ((mask & 1) == 0) { mask >>= 1; ++s; }
            return s;
        }

        static int Bits(uint mask)
        {
            int n = 0;
            while (mask != 0) { n += (int)(mask & 1); mask >>= 1; }
            return n;
        }

        static byte Scale(uint v, uint mask, int shift, int bits)
        {
            if (mask == 0 || bits == 0) return 0;
            uint c = (v & mask) >> shift;
            uint max = (1u << bits) - 1;
            return (byte)((c * 255 + max / 2) / max);
        }

        /// <summary>RLE8/RLE4。出力はbottom-up(ファイルと同じ並び)</summary>
        static void DecodeRle(byte[] d, int p, int w, int h, bool rle4, byte[] pal, byte[] outBuf)
        {
            int x = 0, y = 0;
            Action<int> put = idx =>
            {
                if (x < w && y < h)
                {
                    int o = (y * w + x) * 4;
                    outBuf[o] = pal[idx * 3]; outBuf[o + 1] = pal[idx * 3 + 1]; outBuf[o + 2] = pal[idx * 3 + 2]; outBuf[o + 3] = 255;
                }
                ++x;
            };
            while (p + 1 < d.Length && y < h)
            {
                int count = d[p++];
                int val = d[p++];
                if (count > 0)
                {
                    for (int i = 0; i < count; ++i)
                        put(rle4 ? ((i & 1) == 0 ? (val >> 4) : (val & 15)) : val);
                    continue;
                }
                if (val == 0) { x = 0; ++y; }          //行末
                else if (val == 1) break;              //画像の終わり
                else if (val == 2)                     //移動
                {
                    if (p + 1 >= d.Length) break;
                    x += d[p++];
                    y += d[p++];
                }
                else                                   //非圧縮の並び
                {
                    int n = val;
                    if (rle4)
                    {
                        for (int i = 0; i < n; ++i)
                        {
                            if (p + (i >> 1) >= d.Length) break;
                            int b = d[p + (i >> 1)];
                            put((i & 1) == 0 ? (b >> 4) : (b & 15));
                        }
                        int bytes = (n + 1) / 2;
                        p += bytes + (bytes & 1);
                    }
                    else
                    {
                        for (int i = 0; i < n && p + i < d.Length; ++i)
                            put(d[p + i]);
                        p += n + (n & 1);
                    }
                }
            }
        }

        static void FlipRows(byte[] buf, int w, int h)
        {
            int stride = w * 4;
            byte[] tmp = new byte[stride];
            for (int y = 0; y < h / 2; ++y)
            {
                int a = y * stride, b = (h - 1 - y) * stride;
                Buffer.BlockCopy(buf, a, tmp, 0, stride);
                Buffer.BlockCopy(buf, b, buf, a, stride);
                Buffer.BlockCopy(tmp, 0, buf, b, stride);
            }
        }

        #endregion

        #region GIF

        static bool DecodeGif(byte[] d, out int width, out int height, out byte[] rgba)
        {
            width = height = 0;
            rgba = null;

            int sw = U16(d, 6);
            int sh = U16(d, 8);
            int flags = d[10];
            int p = 13;
            if (sw <= 0 || sh <= 0 || sw > 16384 || sh > 16384 || (long)sw * sh > MaxPixels)
                return false;

            byte[] globalTable = null;
            if ((flags & 0x80) != 0)
            {
                int n = 3 << ((flags & 7) + 1);
                globalTable = new byte[n];
                Buffer.BlockCopy(d, p, globalTable, 0, n);
                p += n;
            }

            int transparent = -1;
            while (p < d.Length)
            {
                int block = d[p++];
                if (block == 0x21)
                {
                    //拡張ブロック。透過色だけ拾う
                    int label = d[p++];
                    if (label == 0xF9 && d[p] >= 4)
                    {
                        int gceFlags = d[p + 1];
                        if ((gceFlags & 1) != 0)
                            transparent = d[p + 4];
                    }
                    p = SkipSubBlocks(d, p);
                }
                else if (block == 0x2C)
                {
                    int fx = U16(d, p), fy = U16(d, p + 2);
                    int fw = U16(d, p + 4), fh = U16(d, p + 6);
                    int fflags = d[p + 8];
                    p += 9;
                    byte[] table = globalTable;
                    if ((fflags & 0x80) != 0)
                    {
                        int n = 3 << ((fflags & 7) + 1);
                        table = new byte[n];
                        Buffer.BlockCopy(d, p, table, 0, n);
                        p += n;
                    }
                    if (table == null)
                        return false;
                    bool interlaced = (fflags & 0x40) != 0;

                    int minCodeSize = d[p++];
                    //サブブロックを連結してLZWの入力にする
                    int dataLen = 0;
                    for (int q = p; q < d.Length && d[q] != 0; q += d[q] + 1)
                        dataLen += d[q];
                    byte[] lzw = new byte[dataLen];
                    int o = 0;
                    while (p < d.Length && d[p] != 0)
                    {
                        int n = d[p++];
                        Buffer.BlockCopy(d, p, lzw, o, Math.Min(n, d.Length - p));
                        o += n;
                        p += n;
                    }

                    byte[] indices = LzwDecode(lzw, minCodeSize, fw * fh);

                    //論理画面の大きさの透明なキャンバスへ最初のフレームを置く(GDI+と同じ)
                    byte[] outBuf = new byte[sw * sh * 4];
                    int[] rowMap = interlaced ? InterlaceRows(fh) : null;
                    int colors = table.Length / 3;
                    for (int i = 0; i < fh; ++i)
                    {
                        int srcRow = i;
                        int destRowInFrame = interlaced ? rowMap[i] : i;
                        int y = fy + destRowInFrame;
                        if (y >= sh) continue;
                        //Unityは下の行から
                        int outRow = sh - 1 - y;
                        for (int j = 0; j < fw; ++j)
                        {
                            int x = fx + j;
                            if (x >= sw) break;
                            int k = srcRow * fw + j;
                            if (k >= indices.Length) break;
                            int idx = indices[k];
                            if (idx == transparent || idx >= colors) continue;
                            int od = (outRow * sw + x) * 4;
                            outBuf[od] = table[idx * 3];
                            outBuf[od + 1] = table[idx * 3 + 1];
                            outBuf[od + 2] = table[idx * 3 + 2];
                            outBuf[od + 3] = 255;
                        }
                    }
                    width = sw; height = sh; rgba = outBuf;
                    return true;
                }
                else
                    break;   //0x3B(終端)か不明なブロック
            }
            return false;
        }

        static int SkipSubBlocks(byte[] d, int p)
        {
            while (p < d.Length)
            {
                int n = d[p++];
                if (n == 0) break;
                p += n;
            }
            return p;
        }

        /// <summary>インターレースの格納順i番目が、画像の何行目か</summary>
        static int[] InterlaceRows(int h)
        {
            int[] map = new int[h];
            int i = 0;
            int[] start = { 0, 4, 2, 1 };
            int[] step = { 8, 8, 4, 2 };
            for (int pass = 0; pass < 4; ++pass)
                for (int y = start[pass]; y < h; y += step[pass])
                    map[i++] = y;
            return map;
        }

        static byte[] LzwDecode(byte[] data, int minCodeSize, int pixelCount)
        {
            byte[] output = new byte[pixelCount];
            if (minCodeSize < 1 || minCodeSize > 11)
                return output;
            int clear = 1 << minCodeSize;
            int eoi = clear + 1;
            const int MaxCodes = 4096;
            short[] prefix = new short[MaxCodes];
            byte[] suffix = new byte[MaxCodes];
            byte[] stack = new byte[MaxCodes + 1];
            for (int i = 0; i < clear; ++i) { prefix[i] = -1; suffix[i] = (byte)i; }

            int codeSize = minCodeSize + 1;
            int next = eoi + 1;
            int old = -1;
            byte first = 0;
            int bitBuf = 0, bitCount = 0, pos = 0, outPos = 0;

            while (outPos < pixelCount)
            {
                while (bitCount < codeSize)
                {
                    if (pos >= data.Length) return output;
                    bitBuf |= data[pos++] << bitCount;
                    bitCount += 8;
                }
                int code = bitBuf & ((1 << codeSize) - 1);
                bitBuf >>= codeSize;
                bitCount -= codeSize;

                if (code == clear)
                {
                    codeSize = minCodeSize + 1;
                    next = eoi + 1;
                    old = -1;
                    continue;
                }
                if (code == eoi)
                    break;

                if (old == -1)
                {
                    if (code >= clear) return output;
                    output[outPos++] = suffix[code];
                    old = code;
                    first = suffix[code];
                    continue;
                }

                int inCode = code;
                int sp = 0;
                if (code >= next)
                {
                    //KwKwK
                    stack[sp++] = first;
                    code = old;
                }
                while (code >= clear)
                {
                    if (code >= MaxCodes || sp >= MaxCodes) return output;
                    stack[sp++] = suffix[code];
                    code = prefix[code];
                }
                first = suffix[code];
                stack[sp++] = first;

                if (next < MaxCodes)
                {
                    prefix[next] = (short)old;
                    suffix[next] = first;
                    ++next;
                    if (next == (1 << codeSize) && codeSize < 12)
                        ++codeSize;
                }
                old = inCode;

                while (sp > 0 && outPos < pixelCount)
                    output[outPos++] = stack[--sp];
            }
            return output;
        }

        #endregion
    }
}
