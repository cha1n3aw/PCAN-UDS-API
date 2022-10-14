using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCAN_UDS_TEST
{
	public class SecurityAccess
	{
		const int AES128_ROUNDKEYARRAYSIZE_DU8 = 176;
		const int Aes128_ErrBitMaskRngChkInpSig_DU32 = 2;

		// Substitution Box (Lookup-table)
		private byte[] subBox_au8 = new byte[] {
		  0x63, 0x7C, 0x77, 0x7B, 0xF2, 0x6B, 0x6F, 0xC5, 0x30, 0x01, 0x67, 0x2B, 0xFE, 0xD7, 0xAB, 0x76,
		  0xCA, 0x82, 0xC9, 0x7D, 0xFA, 0x59, 0x47, 0xF0, 0xAD, 0xD4, 0xA2, 0xAF, 0x9C, 0xA4, 0x72, 0xC0,
		  0xB7, 0xFD, 0x93, 0x26, 0x36, 0x3F, 0xF7, 0xCC, 0x34, 0xA5, 0xE5, 0xF1, 0x71, 0xD8, 0x31, 0x15,
		  0x04, 0xC7, 0x23, 0xC3, 0x18, 0x96, 0x05, 0x9A, 0x07, 0x12, 0x80, 0xE2, 0xEB, 0x27, 0xB2, 0x75,
		  0x09, 0x83, 0x2C, 0x1A, 0x1B, 0x6E, 0x5A, 0xA0, 0x52, 0x3B, 0xD6, 0xB3, 0x29, 0xE3, 0x2F, 0x84,
		  0x53, 0xD1, 0x00, 0xED, 0x20, 0xFC, 0xB1, 0x5B, 0x6A, 0xCB, 0xBE, 0x39, 0x4A, 0x4C, 0x58, 0xCF,
		  0xD0, 0xEF, 0xAA, 0xFB, 0x43, 0x4D, 0x33, 0x85, 0x45, 0xF9, 0x02, 0x7F, 0x50, 0x3C, 0x9F, 0xA8,
		  0x51, 0xA3, 0x40, 0x8F, 0x92, 0x9D, 0x38, 0xF5, 0xBC, 0xB6, 0xDA, 0x21, 0x10, 0xFF, 0xF3, 0xD2,
		  0xCD, 0x0C, 0x13, 0xEC, 0x5F, 0x97, 0x44, 0x17, 0xC4, 0xA7, 0x7E, 0x3D, 0x64, 0x5D, 0x19, 0x73,
		  0x60, 0x81, 0x4F, 0xDC, 0x22, 0x2A, 0x90, 0x88, 0x46, 0xEE, 0xB8, 0x14, 0xDE, 0x5E, 0x0B, 0xDB,
		  0xE0, 0x32, 0x3A, 0x0A, 0x49, 0x06, 0x24, 0x5C, 0xC2, 0xD3, 0xAC, 0x62, 0x91, 0x95, 0xE4, 0x79,
		  0xE7, 0xC8, 0x37, 0x6D, 0x8D, 0xD5, 0x4E, 0xA9, 0x6C, 0x56, 0xF4, 0xEA, 0x65, 0x7A, 0xAE, 0x08,
		  0xBA, 0x78, 0x25, 0x2E, 0x1C, 0xA6, 0xB4, 0xC6, 0xE8, 0xDD, 0x74, 0x1F, 0x4B, 0xBD, 0x8B, 0x8A,
		  0x70, 0x3E, 0xB5, 0x66, 0x48, 0x03, 0xF6, 0x0E, 0x61, 0x35, 0x57, 0xB9, 0x86, 0xC1, 0x1D, 0x9E,
		  0xE1, 0xF8, 0x98, 0x11, 0x69, 0xD9, 0x8E, 0x94, 0x9B, 0x1E, 0x87, 0xE9, 0xCE, 0x55, 0x28, 0xDF,
		  0x8C, 0xA1, 0x89, 0x0D, 0xBF, 0xE6, 0x42, 0x68, 0x41, 0x99, 0x2D, 0x0F, 0xB0, 0x54, 0xBB, 0x16
		};

		// Inverse Substitution Box (Lookup-table)
		private byte[] ivsSubBox_au8 = new byte[] {
		  0x52, 0x09, 0x6A, 0xD5, 0x30, 0x36, 0xA5, 0x38, 0xBF, 0x40, 0xA3, 0x9E, 0x81, 0xF3, 0xD7, 0xFB,
		  0x7C, 0xE3, 0x39, 0x82, 0x9B, 0x2F, 0xFF, 0x87, 0x34, 0x8E, 0x43, 0x44, 0xC4, 0xDE, 0xE9, 0xCB,
		  0x54, 0x7B, 0x94, 0x32, 0xA6, 0xC2, 0x23, 0x3D, 0xEE, 0x4C, 0x95, 0x0B, 0x42, 0xFA, 0xC3, 0x4E,
		  0x08, 0x2E, 0xA1, 0x66, 0x28, 0xD9, 0x24, 0xB2, 0x76, 0x5B, 0xA2, 0x49, 0x6D, 0x8B, 0xD1, 0x25,
		  0x72, 0xF8, 0xF6, 0x64, 0x86, 0x68, 0x98, 0x16, 0xD4, 0xA4, 0x5C, 0xCC, 0x5D, 0x65, 0xB6, 0x92,
		  0x6C, 0x70, 0x48, 0x50, 0xFD, 0xED, 0xB9, 0xDA, 0x5E, 0x15, 0x46, 0x57, 0xA7, 0x8D, 0x9D, 0x84,
		  0x90, 0xD8, 0xAB, 0x00, 0x8C, 0xBC, 0xD3, 0x0A, 0xF7, 0xE4, 0x58, 0x05, 0xB8, 0xB3, 0x45, 0x06,
		  0xD0, 0x2C, 0x1E, 0x8F, 0xCA, 0x3F, 0x0F, 0x02, 0xC1, 0xAF, 0xBD, 0x03, 0x01, 0x13, 0x8A, 0x6B,
		  0x3A, 0x91, 0x11, 0x41, 0x4F, 0x67, 0xDC, 0xEA, 0x97, 0xF2, 0xCF, 0xCE, 0xF0, 0xB4, 0xE6, 0x73,
		  0x96, 0xAC, 0x74, 0x22, 0xE7, 0xAD, 0x35, 0x85, 0xE2, 0xF9, 0x37, 0xE8, 0x1C, 0x75, 0xDF, 0x6E,
		  0x47, 0xF1, 0x1A, 0x71, 0x1D, 0x29, 0xC5, 0x89, 0x6F, 0xB7, 0x62, 0x0E, 0xAA, 0x18, 0xBE, 0x1B,
		  0xFC, 0x56, 0x3E, 0x4B, 0xC6, 0xD2, 0x79, 0x20, 0x9A, 0xDB, 0xC0, 0xFE, 0x78, 0xCD, 0x5A, 0xF4,
		  0x1F, 0xDD, 0xA8, 0x33, 0x88, 0x07, 0xC7, 0x31, 0xB1, 0x12, 0x10, 0x59, 0x27, 0x80, 0xEC, 0x5F,
		  0x60, 0x51, 0x7F, 0xA9, 0x19, 0xB5, 0x4A, 0x0D, 0x2D, 0xE5, 0x7A, 0x9F, 0x93, 0xC9, 0x9C, 0xEF,
		  0xA0, 0xE0, 0x3B, 0x4D, 0xAE, 0x2A, 0xF5, 0xB0, 0xC8, 0xEB, 0xBB, 0x3C, 0x83, 0x53, 0x99, 0x61,
		  0x17, 0x2B, 0x04, 0x7E, 0xBA, 0x77, 0xD6, 0x26, 0xE1, 0x69, 0x14, 0x63, 0x55, 0x21, 0x0C, 0x7D
		};

		// Mix Colum Matrix (Lookup-table)
		byte[] mixColMtrx_au8 = new byte[] {
		  0x02, 0x03, 0x01, 0x01,
		  0x01, 0x02, 0x03, 0x01,
		  0x01, 0x01, 0x02, 0x03,
		  0x03, 0x01, 0x01, 0x02
		};

		// Inverse Mix Colum Matrix (Lookup-table)
		byte[] ivsMixColMtrx_au8 = new byte[] {
		  0x0e, 0x0b, 0x0d, 0x09,
		  0x09, 0x0e, 0x0b, 0x0d,
		  0x0d, 0x09, 0x0e, 0x0b,
		  0x0b, 0x0d, 0x09, 0x0e
		};

		private byte[] roundCon_au8 = new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36 };

		public void GetSeed()
		{
			// mk();
			byte[] mstKey = new byte[] { 0x37, 0x9d, 0x6c, 0x34, 0x9d, 0x27, 0x4f, 0x1d, 0xa7, 0x2c, 0xa8, 0xa6, 0x77, 0x93, 0xa4, 0x16 };

			// List<int> password = [0x6f, 0x4e, 0x6c, 0x59, 0x20, 0x64, 0x45, 0x6d, 0x4f, 0x20, 0x61, 0x43, 0x63, 0x45, 0x73, 0x53]; //oNlY dEmO aCcEsS
			// List<int> password = [0x73, 0x45, 0x72, 0x56, 0x69, 0x43, 0x65, 0x54, 0x65, 0x43, 0x68, 0x4e, 0x69, 0x4b, 0x65, 0x52]; //sErViCeTeChNiKeR 
			// List<int> password = [0x47, 0x77, 0x68, 0x6a, 0x69, 0x44, 0x39, 0x33, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; //"GwhjiD93";
			// List<int> password = [0x37, 0x54, 0x52, 0x50, 0x57, 0x6c, 0x4b, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; //"7TRPWlKU";
			// List<int> password = [0x48, 0x78, 0x43, 0x64, 0x70, 0x38, 0x51, 0x42, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; //"HxCdp8QB"; 
			// List<int> password = [0x44, 0x49, 0x4e, 0x42, 0x30, 0x6e, 0x30, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; //"DINB0n0s";
			// List<int> password = [0x67, 0x47, 0x36, 0x49, 0x66, 0x46, 0x47, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; //"gG6IfFGH";
			// List<int> password = [0x43, 0x4d, 0x33, 0x67, 0x41, 0x78, 0x30, 0x62, 0x68, 0x30, 0x48, 0x61, 0x6f, 0x42, 0x39, 0x43]; //"CM3gAx0bh0HaoB9C";


			byte[] password = new byte[] { 0x5A, 0x86, 0xEF, 0xEC, 0x47, 0x18, 0xA5, 0x33, 0xFB, 0x88, 0x6B, 0x21, 0x0A, 0x2D, 0x6F, 0x89 };
			// List<int> password =  [0x42, 0x98, 0x07, 0x75, 0x50, 0x89, 0x45, 0x98, 0xF0, 0x8C, 0x8C, 0x05, 0xC1, 0xF0, 0x22, 0xF1];
			// List<int> password =  [0x9F, 0xC8, 0x4F, 0x2C, 0x05, 0x6E, 0xE2, 0xD7, 0x4B, 0x9F, 0xEC, 0x89, 0x39, 0x13, 0xD5, 0x41];
			// List<int> password =  [0x01, 0x7A, 0x86, 0xB6, 0x3E, 0x6F, 0x28, 0xEF, 0x35, 0xEE, 0x2E, 0x0C, 0x5D, 0x7B, 0xA0, 0xFF];
			// List<int> password =  [0x89, 0xC7, 0xD6, 0x9A, 0xE0, 0x5A, 0x53, 0x32, 0x7D, 0x0A, 0xB2, 0x2B, 0x44, 0x8F, 0x75, 0x41];
			// List<int> password =  [0x75, 0xAF, 0x4A, 0x83, 0xB5, 0x61, 0xDB, 0x9D, 0x80, 0xA9, 0x4B, 0x5D, 0xCA, 0xFF, 0xF9, 0x65];
			// List<int> password =  [0x0B, 0xA6, 0xDC, 0x28, 0x45, 0xC8, 0x7B, 0xF9, 0xE8, 0x1B, 0xAC, 0x70, 0x53, 0x6A, 0x5F, 0xDA];

			byte[] seed = new byte[] { 0x45, 0x9F, 0xAA, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

			byte[] passKey = Decrypt(mstKey, password);
			//Console.WriteLine('${password.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt password');
			foreach (byte b in password) Console.Write($"{b:X2} ");
			Console.WriteLine(" password decrypt");
			//Console.WriteLine('${mstKey.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt mstKey');
			foreach (byte b in mstKey) Console.Write($"{b:X2} ");
			Console.WriteLine(" mstKey decrypt");
			//Console.WriteLine('${passKey.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt passKey\n');
			foreach (byte b in passKey) Console.Write($"{b:X2} ");
			Console.WriteLine(" passKey decrypt\n");

			byte[] aKey = Encrypt(passKey, seed);
			//Console.WriteLine('${seed.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} encrypt seed');
			foreach (byte b in seed) Console.Write($"{b:X2} ");
			Console.WriteLine(" seed encrypt");
			//Console.WriteLine('${passKey.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} encrypt passKey');
			foreach (byte b in passKey) Console.Write($"{b:X2} ");
			Console.WriteLine(" passKey encrypt");
			//Console.WriteLine('${aKey.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} encrypt aKey\n');
			foreach (byte b in aKey) Console.Write($"{b:X2} ");
			Console.WriteLine(" aKey encrypt\n");

			byte[] aKey2 = Encrypt(password, seed);
			//Console.WriteLine('${seed.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} encrypt seed');
			foreach (byte b in seed) Console.Write($"{b:X2} ");
			Console.WriteLine(" seed encrypt");
			//Console.WriteLine('${password.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} encrypt password');
			foreach (byte b in password) Console.Write($"{b:X2} ");
			Console.WriteLine(" password encrypt");
			//Console.WriteLine('${aKey2.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} encrypt aKey2\n');
			foreach (byte b in aKey2) Console.Write($"{b:X2} ");
			Console.WriteLine(" aKey2 encrypt\n");

			// List<int> accessKey =  [0xAA, 0xED, 0x8D, 0x54, 0xB6, 0xEF, 0x0A, 0xB2, 0x18, 0xA1, 0xCE, 0x53, 0x7A, 0x52, 0x92, 0xF5];


			// List<int> resTest = Aes128_Decr( seed, accessKey );
			// print('${accessKey.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt accessKey');
			// print('${seed.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt seed');
			// print('${resTest.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt resTest\n');

			// List<int> resultTest = Aes128_Encr( mstKey, seed );
			// print('${seed.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt seed');
			// print('${mstKey.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt mstKey');
			// print('${resultTest.map((e)=>e.toRadixString(16).padLeft(2, '0').toUpperCase()).toList().toString()} decrypt resultTest\n');
		}

		byte[] Encrypt(byte[] key, byte[] data)
		{

			byte indexRound;

			byte[] data_au8 = data.ToArray();
			byte[] keys_au8 = new byte[AES128_ROUNDKEYARRAYSIZE_DU8];

			// Generate round keys and store in private variable
			keys_au8 = GenerateRoundKeys(key, keys_au8);

			// Add Round Key step for round 1
			data_au8 = AddRoundKey(data_au8, keys_au8);

			// For loop for rounds 1-9, each executing 4 calculation steps
			for (indexRound = 1; indexRound < 10; indexRound++)
			{
				// 1. Substitute Bytes
				data_au8 = Substitute(data_au8);

				// 2. Shift Rows
				data_au8 = ShiftRows(data_au8);

				// 3. Mix Columns
				data_au8 = MixColumns(data_au8);

				// 4. Add Round Key
				byte[] tempRoundKey = new byte[keys_au8.Length - (indexRound * 16)];
				Array.Copy(keys_au8, indexRound * 16, tempRoundKey, 0, keys_au8.Length - (indexRound * 16));
				data_au8 = AddRoundKey(data_au8, tempRoundKey);
			}

			// Substitute Bytes last round
			data_au8 = Substitute(data_au8);

			// Shift Rows last round
			data_au8 = ShiftRows(data_au8);

			// Add Round Key last round
			byte[] tempLastRoundKey = new byte[keys_au8.Length - 160];
			Array.Copy(keys_au8, 160, tempLastRoundKey, 0, keys_au8.Length - 160);
			data_au8 = AddRoundKey(data_au8, tempLastRoundKey);
			return data_au8;
		}

		byte[] Decrypt(byte[] key, byte[] data)
		{
			byte indexRound;
			byte[] data_au8 = data.ToArray();
			byte[] keys_au8 = new byte[AES128_ROUNDKEYARRAYSIZE_DU8];
			// Generate round keys and store in private variable
			keys_au8 = GenerateRoundKeys(key, keys_au8);

			// Add Round Key step for round 1
			byte[] tempFirstRoundKey = new byte[keys_au8.Length - 160];
			Array.Copy(keys_au8, 160, tempFirstRoundKey, 0, keys_au8.Length - 160);
			data_au8 = AddRoundKey(data_au8, tempFirstRoundKey);

			// For loop for rounds 1-9, each executing 4 calculation steps
			for (indexRound = 1; indexRound < 10; indexRound++)
			{
				// 1. Inverse Shift Rows
				data_au8 = InverseShiftRows(data_au8);

				// 2. Inverse Substitute Bytes
				data_au8 = InverseSubstitute(data_au8);
				// 3. Add Round Key
				byte[] tempRoundKey = new byte[keys_au8.Length - ((10 - indexRound) * 16)];

				Array.Copy(keys_au8, (10 -indexRound) * 16, tempRoundKey, 0, keys_au8.Length - ((10 -indexRound) * 16));
				data_au8 = AddRoundKey(data_au8, tempRoundKey);

				// 4. Inverse Mix Columns
				data_au8 = InverseMixColumns(data_au8);
			}

			// Inverse Shift Rows last round
			data_au8 = InverseShiftRows(data_au8);

			// Inverse Substitute Bytes last round
			data_au8 = InverseSubstitute(data_au8);

			// Add Round Key last round
			data_au8 = AddRoundKey(data_au8, keys_au8);

			return data_au8;

		}

		byte[] GenerateRoundKeys(byte[] mstKey_au8, byte[] roundKeys_au8)
		{
			byte idxColU8, idxBytU8;

			// Store master key as first round key
			for (idxBytU8 = 0; idxBytU8 < 16; idxBytU8++)
			{
				// Copy key to round key array
				roundKeys_au8[idxBytU8] = mstKey_au8[idxBytU8];
			}

			// Generate all other round keys
			for (idxColU8 = 4; idxColU8 < 44; idxColU8++)
			{
				// Copy previous key
				roundKeys_au8[4 * idxColU8] = roundKeys_au8[4 * (idxColU8 - 1)];
				roundKeys_au8[4 * idxColU8 + 1] = roundKeys_au8[4 * (idxColU8 - 1) + 1];
				roundKeys_au8[4 * idxColU8 + 2] = roundKeys_au8[4 * (idxColU8 - 1) + 2];
				roundKeys_au8[4 * idxColU8 + 3] = roundKeys_au8[4 * (idxColU8 - 1) + 3];

				// Key transformation in case first column of state matrix
				if (0 == idxColU8 % 4)
				{
					byte tmpValU8;

					// Rotate word (column)
					tmpValU8 = roundKeys_au8[4 * idxColU8];
					roundKeys_au8[4 * idxColU8] = roundKeys_au8[4 * idxColU8 + 1];
					roundKeys_au8[4 * idxColU8 + 1] = roundKeys_au8[4 * idxColU8 + 2];
					roundKeys_au8[4 * idxColU8 + 2] = roundKeys_au8[4 * idxColU8 + 3];
					roundKeys_au8[4 * idxColU8 + 3] = tmpValU8;

					// Substitute Bytes
					roundKeys_au8[4 * idxColU8] = subBox_au8[roundKeys_au8[4 * idxColU8]];
					roundKeys_au8[4 * idxColU8 + 1] = subBox_au8[roundKeys_au8[4 * idxColU8 + 1]];
					roundKeys_au8[4 * idxColU8 + 2] = subBox_au8[roundKeys_au8[4 * idxColU8 + 2]];
					roundKeys_au8[4 * idxColU8 + 3] = subBox_au8[roundKeys_au8[4 * idxColU8 + 3]];

					// Add round constant
					// cppcheck-suppress negativeIndex : idxCol_u8 is greater or equal 4 so the index will not be negative at any time
					roundKeys_au8[4 * idxColU8] ^= roundCon_au8[idxColU8 / 4 - 1];
				}

				// XOR with previous state matrix
				roundKeys_au8[4 * idxColU8] ^= roundKeys_au8[4 * (idxColU8 - 4)];
				roundKeys_au8[4 * idxColU8 + 1] ^= roundKeys_au8[4 * (idxColU8 - 4) + 1];
				roundKeys_au8[4 * idxColU8 + 2] ^= roundKeys_au8[4 * (idxColU8 - 4) + 2];
				roundKeys_au8[4 * idxColU8 + 3] ^= roundKeys_au8[4 * (idxColU8 - 4) + 3];
			}
			return roundKeys_au8;
		}

		byte[] AddRoundKey(byte[] data_au8, byte[] roundKey_au8)
		{
			byte idxBytU8;
			for (idxBytU8 = 0; idxBytU8 < 16; idxBytU8++)
			{
				data_au8[idxBytU8] = (byte)(data_au8[idxBytU8] ^ roundKey_au8[idxBytU8]);
			}
			return data_au8;
		}

		byte[] MixColumns(byte[] data_au8)
		{
			byte[] tmpDataAu8 = new byte[4];
			byte indexColumn, indexByte;

			// Loop through data columns
			for (indexColumn = 0; indexColumn < 4; indexColumn++)
			{
				// Calculate each byte in new column with Galois-field F(2^8) multiplication of
				// the whole column with the mix-column-matrix row
				for (indexByte = 0; indexByte < 4; indexByte++)
				{
					tmpDataAu8[indexByte] = ByteMultiply(data_au8[4 * indexColumn], mixColMtrx_au8[4 * indexByte]);
					tmpDataAu8[indexByte] ^= ByteMultiply(data_au8[1 + 4 * indexColumn], mixColMtrx_au8[4 * indexByte + 1]);
					tmpDataAu8[indexByte] ^= ByteMultiply(data_au8[2 + 4 * indexColumn], mixColMtrx_au8[4 * indexByte + 2]);
					tmpDataAu8[indexByte] ^= ByteMultiply(data_au8[3 + 4 * indexColumn], mixColMtrx_au8[4 * indexByte + 3]);
				}

				// Replace column with new column
				data_au8[4 * indexColumn] = tmpDataAu8[0];
				data_au8[1 + 4 * indexColumn] = tmpDataAu8[1];
				data_au8[2 + 4 * indexColumn] = tmpDataAu8[2];
				data_au8[3 + 4 * indexColumn] = tmpDataAu8[3];
			}
			return data_au8;
		}

		byte[] InverseMixColumns(byte[] data_au8)
		{
			byte[] tmpDataAu8 = new byte[4];
			byte idxColU8, idxBytU8;

			// Loop through data columns
			for (idxColU8 = 0; idxColU8 < 4; idxColU8++)
			{
				// Calculate each byte in new column with Galois-field F(2^8) multiplication of
				// the whole column with the inverse-mix-column-matrix row
				for (idxBytU8 = 0; idxBytU8 < 4; idxBytU8++)
				{
					tmpDataAu8[idxBytU8] = ByteMultiply(data_au8[4 * idxColU8], ivsMixColMtrx_au8[4 * idxBytU8]);
					tmpDataAu8[idxBytU8] ^= ByteMultiply(data_au8[1 + 4 * idxColU8], ivsMixColMtrx_au8[4 * idxBytU8 + 1]);
					tmpDataAu8[idxBytU8] ^= ByteMultiply(data_au8[2 + 4 * idxColU8], ivsMixColMtrx_au8[4 * idxBytU8 + 2]);
					tmpDataAu8[idxBytU8] ^= ByteMultiply(data_au8[3 + 4 * idxColU8], ivsMixColMtrx_au8[4 * idxBytU8 + 3]);
				}

				// Replace column with new column
				data_au8[4 * idxColU8] = tmpDataAu8[0];
				data_au8[1 + 4 * idxColU8] = tmpDataAu8[1];
				data_au8[2 + 4 * idxColU8] = tmpDataAu8[2];
				data_au8[3 + 4 * idxColU8] = tmpDataAu8[3];
			}
			return data_au8;
		}

		byte ByteMultiply(byte fac1U8, byte fac2U8)
		{
			int tmpFacU16;
			int productU16 = 0x0000;
			int galoisU16 = 0x8d80;
			int countU16 = 0x8000;
			byte idxBitU8;

			tmpFacU16 = fac1U8;

			// Full galois-field multiplication would require tremendous resources.
			// We only simulate multiplacation for the known factors as used in AES.
			// Simulate binary multiplication with XOR and bit shift
			switch (fac2U8)
			{
				case 0x01:
					productU16 ^= tmpFacU16;
					break;

				case 0x02:
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					break;

				case 0x03:
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					break;

				case 0x09:
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 3;
					productU16 ^= tmpFacU16;
					break;

				case 0x0B:
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 2;
					productU16 ^= tmpFacU16;
					break;

				case 0x0D:
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 2;
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					break;

				case 0x0E:
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					tmpFacU16 <<= 1;
					productU16 ^= tmpFacU16;
					break;
			}

			// Combine multiplication value with galois-field polynomial
			for (idxBitU8 = 0; idxBitU8 < 8; ++idxBitU8)
			{
				if (0 != (productU16 & countU16))
				{
					productU16 ^= galoisU16;
				}
				galoisU16 >>= 1;
				countU16 >>= 1;
			}

			return (byte)(productU16 & 0xFF);
		}

		byte[] Substitute(byte[] data_au8)
		{
			byte indexByte;
			for (indexByte = 0; indexByte < 16; indexByte++)
			{
				data_au8[indexByte] = subBox_au8[data_au8[indexByte]];
			}
			return data_au8;
		}

		byte[] InverseSubstitute(byte[] data_au8)
		{
			byte indexByte;
			for (indexByte = 0; indexByte < 16; indexByte++)
			{
				data_au8[indexByte] = ivsSubBox_au8[data_au8[indexByte]];
			}
			return data_au8;
		}

		byte[] ShiftRows(byte[] data_au8)
		{
			byte[] tempData = new byte[16];
			byte indexByte;

			// Copy original data
			for (indexByte = 0; indexByte < 16; indexByte++)
			{
				tempData[indexByte] = data_au8[indexByte];
			}

			// Shift data
			data_au8[9] = tempData[13];
			data_au8[5] = tempData[9];
			data_au8[1] = tempData[5];
			data_au8[13] = tempData[1];
			data_au8[6] = tempData[14];
			data_au8[2] = tempData[10];
			data_au8[14] = tempData[6];
			data_au8[10] = tempData[2];
			data_au8[7] = tempData[3];
			data_au8[11] = tempData[7];
			data_au8[15] = tempData[11];
			data_au8[3] = tempData[15];

			return data_au8;
		}

		byte[] InverseShiftRows(byte[] data_au8)
		{
			byte[] tempData = new byte[16];
			byte indexByte;

			// Copy original data
			for (indexByte = 0; indexByte < 16; indexByte++)
			{
				tempData[indexByte] = data_au8[indexByte];
			}

			// Shift data
			data_au8[13] = tempData[9];
			data_au8[9] = tempData[5];
			data_au8[5] = tempData[1];
			data_au8[1] = tempData[13];
			data_au8[14] = tempData[6];
			data_au8[10] = tempData[2];
			data_au8[6] = tempData[14];
			data_au8[2] = tempData[10];
			data_au8[3] = tempData[7];
			data_au8[7] = tempData[11];
			data_au8[11] = tempData[15];
			data_au8[15] = tempData[3];

			return data_au8;
		}
	}
}
