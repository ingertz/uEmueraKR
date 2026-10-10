using System;

namespace MinorShift.Emuera.Sub
{
	/// <summary>
	/// xxHash(XXH32とXXH3の64bit版)。HASH_XXH32 / HASH_XXH3 用。
	///
	/// ShinEraTensei(メガテンP版)のEmueraはSystem.IO.Hashingを呼んでいるが、
	/// Unityには入っていないので同じ値が出るように書き起こした物。
	/// 種は常に0、秘密鍵は既定の物だけを扱う
	/// </summary>
	internal static class XxHash
	{
		const uint P32_1 = 0x9E3779B1U;
		const uint P32_2 = 0x85EBCA77U;
		const uint P32_3 = 0xC2B2AE3DU;
		const uint P32_4 = 0x27D4EB2FU;
		const uint P32_5 = 0x165667B1U;

		const ulong P64_1 = 0x9E3779B185EBCA87UL;
		const ulong P64_2 = 0xC2B2AE3D27D4EB4FUL;
		const ulong P64_3 = 0x165667B19E3779F9UL;
		const ulong P64_4 = 0x85EBCA77C2B2AE63UL;
		const ulong P64_5 = 0x27D4EB2F165667C5UL;
		const ulong PMX_1 = 0x165667919E3779F9UL;
		const ulong PMX_2 = 0x9FB21C651E98DF25UL;

		static readonly byte[] kSecret =
		{
			0xb8, 0xfe, 0x6c, 0x39, 0x23, 0xa4, 0x4b, 0xbe, 0x7c, 0x01, 0x81, 0x2c, 0xf7, 0x21, 0xad, 0x1c,
			0xde, 0xd4, 0x6d, 0xe9, 0x83, 0x90, 0x97, 0xdb, 0x72, 0x40, 0xa4, 0xa4, 0xb7, 0xb3, 0x67, 0x1f,
			0xcb, 0x79, 0xe6, 0x4e, 0xcc, 0xc0, 0xe5, 0x78, 0x82, 0x5a, 0xd0, 0x7d, 0xcc, 0xff, 0x72, 0x21,
			0xb8, 0x08, 0x46, 0x74, 0xf7, 0x43, 0x24, 0x8e, 0xe0, 0x35, 0x90, 0xe6, 0x81, 0x3a, 0x26, 0x4c,
			0x3c, 0x28, 0x52, 0xbb, 0x91, 0xc3, 0x00, 0xcb, 0x88, 0xd0, 0x65, 0x8b, 0x1b, 0x53, 0x2e, 0xa3,
			0x71, 0x64, 0x48, 0x97, 0xa2, 0x0d, 0xf9, 0x4e, 0x38, 0x19, 0xef, 0x46, 0xa9, 0xde, 0xac, 0xd8,
			0xa8, 0xfa, 0x76, 0x3f, 0xe3, 0x9c, 0x34, 0x3f, 0xf9, 0xdc, 0xbb, 0xc7, 0xc7, 0x0b, 0x4f, 0x1d,
			0x8a, 0x51, 0xe0, 0x4b, 0xcd, 0xb4, 0x59, 0x31, 0xc8, 0x9f, 0x7e, 0xc9, 0xd9, 0x78, 0x73, 0x64,
			0xea, 0xc5, 0xac, 0x83, 0x34, 0xd3, 0xeb, 0xc3, 0xc5, 0x81, 0xa0, 0xff, 0xfa, 0x13, 0x63, 0xeb,
			0x17, 0x0d, 0xdd, 0x51, 0xb7, 0xf0, 0xda, 0x49, 0xd3, 0x16, 0x55, 0x26, 0x29, 0xd4, 0x68, 0x9e,
			0x2b, 0x16, 0xbe, 0x58, 0x7d, 0x47, 0xa1, 0xfc, 0x8f, 0xf8, 0xb8, 0xd1, 0x7a, 0xd0, 0x31, 0xce,
			0x45, 0xcb, 0x3a, 0x8f, 0x95, 0x16, 0x04, 0x28, 0xaf, 0xd7, 0xfb, 0xca, 0xbb, 0x4b, 0x40, 0x7e,
		};

		static uint Rotl32(uint x, int r) { return (x << r) | (x >> (32 - r)); }
		static ulong Rotl64(ulong x, int r) { return (x << r) | (x >> (64 - r)); }

		static uint Read32(byte[] b, int i)
		{
			return (uint)b[i] | ((uint)b[i + 1] << 8) | ((uint)b[i + 2] << 16) | ((uint)b[i + 3] << 24);
		}
		static ulong Read64(byte[] b, int i)
		{
			return (ulong)Read32(b, i) | ((ulong)Read32(b, i + 4) << 32);
		}
		static ulong Swap64(ulong x)
		{
			x = ((x & 0x00FF00FF00FF00FFUL) << 8) | ((x >> 8) & 0x00FF00FF00FF00FFUL);
			x = ((x & 0x0000FFFF0000FFFFUL) << 16) | ((x >> 16) & 0x0000FFFF0000FFFFUL);
			return (x << 32) | (x >> 32);
		}

		#region XXH32
		public static uint Hash32(byte[] data)
		{
			unchecked
			{
				int len = data.Length;
				int p = 0;
				uint h;
				if (len >= 16)
				{
					uint v1 = P32_1 + P32_2;
					uint v2 = P32_2;
					uint v3 = 0;
					uint v4 = 0U - P32_1;
					int limit = len - 16;
					do
					{
						v1 = Rotl32(v1 + Read32(data, p) * P32_2, 13) * P32_1; p += 4;
						v2 = Rotl32(v2 + Read32(data, p) * P32_2, 13) * P32_1; p += 4;
						v3 = Rotl32(v3 + Read32(data, p) * P32_2, 13) * P32_1; p += 4;
						v4 = Rotl32(v4 + Read32(data, p) * P32_2, 13) * P32_1; p += 4;
					} while (p <= limit);
					h = Rotl32(v1, 1) + Rotl32(v2, 7) + Rotl32(v3, 12) + Rotl32(v4, 18);
				}
				else
					h = P32_5;
				h += (uint)len;
				while (p + 4 <= len)
				{
					h = Rotl32(h + Read32(data, p) * P32_3, 17) * P32_4;
					p += 4;
				}
				while (p < len)
				{
					h = Rotl32(h + data[p] * P32_5, 11) * P32_1;
					p += 1;
				}
				h ^= h >> 15;
				h *= P32_2;
				h ^= h >> 13;
				h *= P32_3;
				h ^= h >> 16;
				return h;
			}
		}
		#endregion

		#region XXH3 64bit
		/// <summary>64bit同士の積(128bit)の上位と下位を混ぜる</summary>
		static ulong MulFold(ulong a, ulong b)
		{
			unchecked
			{
				ulong al = a & 0xFFFFFFFFUL, ah = a >> 32;
				ulong bl = b & 0xFFFFFFFFUL, bh = b >> 32;
				ulong ll = al * bl;
				ulong hl = ah * bl;
				ulong lh = al * bh;
				ulong hh = ah * bh;
				ulong cross = (ll >> 32) + (hl & 0xFFFFFFFFUL) + lh;
				ulong high = (hl >> 32) + (cross >> 32) + hh;
				ulong low = (cross << 32) | (ll & 0xFFFFFFFFUL);
				return low ^ high;
			}
		}
		static ulong Avalanche64(ulong h)
		{
			unchecked
			{
				h ^= h >> 33;
				h *= P64_2;
				h ^= h >> 29;
				h *= P64_3;
				h ^= h >> 32;
				return h;
			}
		}
		static ulong Avalanche3(ulong h)
		{
			unchecked
			{
				h ^= h >> 37;
				h *= PMX_1;
				h ^= h >> 32;
				return h;
			}
		}
		static ulong Mix16(byte[] d, int p, int s)
		{
			return MulFold(Read64(d, p) ^ Read64(kSecret, s), Read64(d, p + 8) ^ Read64(kSecret, s + 8));
		}

		public static ulong Hash3(byte[] data)
		{
			unchecked
			{
				int len = data.Length;
				if (len == 0)
					return Avalanche64(Read64(kSecret, 56) ^ Read64(kSecret, 64));
				if (len <= 3)
				{
					uint combined = ((uint)data[0] << 16) | ((uint)data[len >> 1] << 24) | data[len - 1] | ((uint)len << 8);
					ulong flip = Read32(kSecret, 0) ^ Read32(kSecret, 4);
					return Avalanche64(combined ^ flip);
				}
				if (len <= 8)
				{
					ulong flip = Read64(kSecret, 8) ^ Read64(kSecret, 16);
					ulong in64 = Read32(data, len - 4) + ((ulong)Read32(data, 0) << 32);
					ulong h = in64 ^ flip;
					h ^= Rotl64(h, 49) ^ Rotl64(h, 24);
					h *= PMX_2;
					h ^= (h >> 35) + (ulong)len;
					h *= PMX_2;
					return h ^ (h >> 28);
				}
				if (len <= 16)
				{
					ulong lo = Read64(data, 0) ^ (Read64(kSecret, 24) ^ Read64(kSecret, 32));
					ulong hi = Read64(data, len - 8) ^ (Read64(kSecret, 40) ^ Read64(kSecret, 48));
					return Avalanche3((ulong)len + Swap64(lo) + hi + MulFold(lo, hi));
				}
				if (len <= 128)
				{
					ulong acc = (ulong)len * P64_1;
					if (len > 32)
					{
						if (len > 64)
						{
							if (len > 96)
							{
								acc += Mix16(data, 48, 96);
								acc += Mix16(data, len - 64, 112);
							}
							acc += Mix16(data, 32, 64);
							acc += Mix16(data, len - 48, 80);
						}
						acc += Mix16(data, 16, 32);
						acc += Mix16(data, len - 32, 48);
					}
					acc += Mix16(data, 0, 0);
					acc += Mix16(data, len - 16, 16);
					return Avalanche3(acc);
				}
				if (len <= 240)
				{
					ulong acc = (ulong)len * P64_1;
					int rounds = len / 16;
					for (int i = 0; i < 8; i++)
						acc += Mix16(data, 16 * i, 16 * i);
					acc = Avalanche3(acc);
					for (int i = 8; i < rounds; i++)
						acc += Mix16(data, 16 * i, 16 * (i - 8) + 3);
					acc += Mix16(data, len - 16, 136 - 17);
					return Avalanche3(acc);
				}
				return HashLong(data);
			}
		}

		static void Accumulate(ulong[] acc, byte[] d, int p, int s)
		{
			unchecked
			{
				for (int i = 0; i < 8; i++)
				{
					ulong v = Read64(d, p + 8 * i);
					ulong k = v ^ Read64(kSecret, s + 8 * i);
					acc[i ^ 1] += v;
					acc[i] += (k & 0xFFFFFFFFUL) * (k >> 32);
				}
			}
		}

		static ulong HashLong(byte[] data)
		{
			unchecked
			{
				const int kStripe = 64;
				const int kStripesPerBlock = (192 - kStripe) / 8;
				const int kBlock = kStripe * kStripesPerBlock;
				int len = data.Length;
				ulong[] acc = { P32_3, P64_1, P64_2, P64_3, P64_4, P32_2, P64_5, P32_1 };

				int blocks = (len - 1) / kBlock;
				for (int n = 0; n < blocks; n++)
				{
					for (int s = 0; s < kStripesPerBlock; s++)
						Accumulate(acc, data, n * kBlock + s * kStripe, s * 8);
					for (int i = 0; i < 8; i++)
					{
						ulong a = acc[i];
						a ^= a >> 47;
						a ^= Read64(kSecret, 192 - kStripe + 8 * i);
						a *= P32_1;
						acc[i] = a;
					}
				}
				int stripes = ((len - 1) - kBlock * blocks) / kStripe;
				for (int s = 0; s < stripes; s++)
					Accumulate(acc, data, blocks * kBlock + s * kStripe, s * 8);
				Accumulate(acc, data, len - kStripe, 192 - kStripe - 7);

				ulong result = (ulong)len * P64_1;
				for (int i = 0; i < 4; i++)
					result += MulFold(acc[2 * i] ^ Read64(kSecret, 11 + 16 * i), acc[2 * i + 1] ^ Read64(kSecret, 11 + 16 * i + 8));
				return Avalanche3(result);
			}
		}
		#endregion
	}
}
