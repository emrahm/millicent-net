﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Millicent.Net.Crypto.Bip39;

namespace Millicent.Net.Crypto.Implemetations
{
    public static class Bech32
    {
        private static readonly string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        private static readonly byte[] Byteset = Encoding.ASCII.GetBytes(Charset);
        private static readonly uint[] Generator = { 0x3b6a57b2U, 0x26508e6dU, 0x1ea119faU, 0x3d4233ddU, 0x2a1462b3U };

        private static uint Polymod(byte[] values)
        {
            
            uint chk = 1;
            foreach (var value in values)
            {
                var top = chk >> 25;
                chk = value ^ ((chk & 0x1ffffff) << 5);
                foreach (var i in Enumerable.Range(0, 5))
                {
                    chk ^= ((top >> i) & 1) == 1 ? Generator[i] : 0;
                }
            }
            return chk;
        }

        private static byte[] HrpExpand(byte[] hrp)
        {
            var len = hrp.Length;
            var ret = new byte[(2 * len) + 1];
            for (int i = 0; i < len; i++)
            {
                ret[i] = (byte)(hrp[i] >> 5);
                ret[i + len + 1] = (byte)(hrp[i] & 31);
            }
            return ret;
        }
        private static bool VerifyChecksum(byte[] hrp, byte[] data)
        {
            var values = ArrayHelpers.Concat(HrpExpand(hrp), data);
            return Polymod(values) == 1;
        }
        private static byte[] CreateChecksum(byte[] hrp, byte[] data)
        {
            var values = ArrayHelpers.Concat(HrpExpand(hrp), data, new byte[] { 0, 0, 0, 0, 0, 0 });
            var polymod = Polymod(values) ^ 1;
            var ret = new byte[6];
            foreach (var i in Enumerable.Range(0, 6))
            {
                ret[i] = (byte)((polymod >> 5 * (5 - i)) & 31);
            }
            return ret;
        }

        //public static string Bech32Encode(byte[] hrp, byte[] data)
        //{
        //    var combined = ArrayHelpers.Concat(data, CreateChecksum(hrp, data));
        //    var tmp = new byte[combined.Length];
        //    for (int i = 0; i < combined.Length; i++)
        //    {
        //        tmp[i] = Byteset[combined[i]];
        //    }
        //    return Encoding.ASCII.GetString(ArrayHelpers.Concat(hrp, new byte[] { 49 }, tmp));
        //}

        //public static byte[] Bech32Decode(string bech, out byte[] hrp)
        //{
        //	if (bech != bech.ToLowerInvariant() && bech != bech.ToUpperInvariant())
        //		throw new FormatException("bech cannot mix upper and lower case");

        //	var buffer = Encoding.ASCII.GetBytes(bech);
        //	if (buffer.Any(b => b < 33 || b > 126))
        //	{
        //		throw new FormatException("bech chars are out of range");
        //	}
        //	bech = bech.ToLowerInvariant();
        //	var pos = bech.LastIndexOf("1", StringComparison.InvariantCultureIgnoreCase);
        //	if (pos < 1 || pos + 7 > bech.Length || bech.Length > 90)
        //	{
        //		throw new FormatException("bech missing separator, separator misplaced or too long input");
        //	}
        //	if (bech.Substring(pos + 1).Any(x => !Charset.Contains(x)))
        //	{
        //		throw new FormatException("bech chars are out of range");
        //	}

        //	buffer = Encoding.ASCII.GetBytes(bech);
        //	hrp = Encoding.ASCII.GetBytes(bech.Substring(0, pos));
        //	var data = new byte[bech.Length - pos - 1];
        //	for (int j = 0, i = pos + 1; i < bech.Length; i++, j++)
        //	{
        //		data[j] = (byte)Array.IndexOf(Byteset, buffer[i]);
        //	}
        //	if (!VerifyChecksum(hrp, data))
        //	{
        //		throw new FormatException("Error while veriying checksum");
        //	}
        //	return data.Take(data.Length - 6).ToArray();
        //}

        private static byte[] ConvertBits(IEnumerable<byte> data, int fromBits, int toBits, bool pad = true)
        {
            var acc = 0;
            var bits = 0;
            var maxv = (1 << toBits) - 1;
            var ret = new List<byte>();
            foreach (var value in data)
            {
                if ((value >> fromBits) > 0)
                    throw new ArgumentOutOfRangeException();
                acc = (acc << fromBits) | value;
                bits += fromBits;
                while (bits >= toBits)
                {
                    bits -= toBits;
                    ret.Add((byte)((acc >> bits) & maxv));

                }
            }
            if (pad)
            {
                if (bits > 0)
                {
                    ret.Add((byte)((acc << (toBits - bits)) & maxv));
                }
            }
            else if (bits >= fromBits || (byte)(((acc << (toBits - bits)) & maxv)) != 0)
            {
                throw new Exception("I have no idea");
            }
            return ret.ToArray();
        }

        //public static byte[] Decode(string hrp, string addr, out byte witnessVerion)
        //{
        //	byte[] hrpgot;
        //	var data = Bech32Decode(addr, out hrpgot);
        //	if (hrp != Encoding.ASCII.GetString(hrpgot))
        //		throw new FormatException("Mismatching human readeable part");

        //	var decoded = ConvertBits(data.Skip(1), 5, 8, false);
        //	if (decoded.Length < 2 || decoded.Length > 40)
        //		throw new FormatException("Invalid decoded data length");

        //	witnessVerion = data[0];
        //	if (witnessVerion > 16)
        //		throw new FormatException("Invalid decoded witness version");

        //	if (witnessVerion == 0 && decoded.Length != 20 && decoded.Length != 32)
        //		throw new FormatException("Decoded witness program with unknown length");
        //	return decoded;
        //}

        //public static string Encode(byte[] hrp, byte witnessVerion, byte[] witnessProgramm)
        //{
        //	var data = ArrayHelpers.Concat(new[] { witnessVerion }, ConvertBits(witnessProgramm, 8, 5));
        //	var ret = Bech32Encode(hrp, data);
        //	byte witVer;
        //	Debug.Assert(Decode(Encoding.ASCII.GetString(hrp), ret, out witVer) == data);
        //	return ret;
        //}
        public static string Encode(string prefix, byte[] data)
        {
            var hrp = prefix.ToByteArrayFromString();
            var combined = ArrayHelpers.Concat(data, CreateChecksum(hrp, data));
            var tmp = new byte[combined.Length];
            for (int i = 0; i < combined.Length; i++)
            {
                tmp[i] = Byteset[combined[i]];
            }
            return Encoding.ASCII.GetString(ArrayHelpers.Concat(hrp, new byte[] { 49 }, tmp));
        }

        public static byte[] ToWords(byte[] data)
        {
            return ConvertBits(data, 8, 5, true);
        }

        public static (string prefix, byte[] words) Decode(string address)
        {

            if (address != address.ToLowerInvariant() && address != address.ToUpperInvariant())
                throw new FormatException("bech cannot mix upper and lower case");

            var buffer = Encoding.ASCII.GetBytes(address);
            if (buffer.Any(b => b < 33 || b > 126))
            {
                throw new FormatException("bech chars are out of range");
            }
            address = address.ToLowerInvariant();
            var pos = address.LastIndexOf("1", StringComparison.InvariantCultureIgnoreCase);
            if (pos < 1 || pos + 7 > address.Length || address.Length > 90)
            {
                throw new FormatException("bech missing separator, separator misplaced or too long input");
            }
            if (address.Substring(pos + 1).Any(x => !Charset.Contains(x)))
            {
                throw new FormatException("bech chars are out of range");
            }

            buffer = Encoding.ASCII.GetBytes(address);
            var hrp = Encoding.ASCII.GetBytes(address.Substring(0, pos));
            var data = new byte[address.Length - pos - 1];
            for (int j = 0, i = pos + 1; i < address.Length; i++, j++)
            {
                data[j] = (byte)Array.IndexOf(Byteset, buffer[i]);
            }
            if (!VerifyChecksum(hrp, data))
            {
                throw new FormatException("Error while veriying checksum");
            }

            return (hrp.ToStringFromByteArray(), data.Take(data.Length - 6).ToArray());
        }

        public static byte[] FromWords(byte[] words)
        {
            var res = ConvertBits(words, 5, 8, false);
            return res;
        }
    }
}

