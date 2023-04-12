namespace Disassembler
{
    class Program
    {
        const byte BIT128_MASK = 0B_10000000;
        const byte BIT1_MASK = 0B_00000001;
        const byte MOV_REGTOREG = 0B_100010;
        const byte MOV_IMMTOREG = 0B_1011;
        const byte MOD_MEM = 0B_00;
        const byte MOD_MEM8BIT = 0B_01;
        const byte MOD_MEM16BIT = 0B_10;
        const byte MOD_REG = 0B_11;
        const byte REG_MASK = 0B_00111000;
        const byte RM_MASK = 0B_00000111;
        const byte REG_0 = 0B_000;
        const byte REG_1 = 0B_001;
        const byte REG_2 = 0B_010;
        const byte REG_3 = 0B_011;
        const byte REG_4 = 0B_100;
        const byte REG_5 = 0B_101;
        const byte REG_6 = 0B_110;
        const byte REG_7 = 0B_111;
        static readonly string[][] registerNames = new string[8][]
        {
            new string[2]{"al", "ax"},
            new string[2]{"cl", "cx"},
            new string[2]{"dl", "dx"},
            new string[2]{"bl", "bx"},
            new string[2]{"ah", "sp"},
            new string[2]{"ch", "bp"},
            new string[2]{"dh", "si"},
            new string[2]{"bh", "di"}
        };
        static readonly string[] addressCalculation = new string[8]
        {
            "[bx + si{0}]",
            "[bx + di{0}]",
            "[bp + si{0}]",
            "[bp + di{0}]",
            "[si{0}]",
            "[di{0}]",
            "[bp{0}]",
            "[bx{0}]"
        };

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Wrong number of parameters.");
                return;
            }

            BinaryReader binReader = new BinaryReader(File.Open(args[0], FileMode.Open));
            byte[] buffer = new byte[4];
            StreamWriter file = new StreamWriter("result" + args[0].Substring(args[0].IndexOf('_')) + ".asm");

            file.WriteLine("bits 16");
            file.WriteLine("");

            while (binReader.Read(buffer, 0, 1) > 0)
            {
                string instruction = "";
                if (CompareBits(buffer[0], MOV_REGTOREG, 6))
                {
                    instruction += "mov ";

                    var wBit = GetBit(buffer[0], 0) ? 1 : 0;
                    var dBit = GetBit(buffer[0], 1);
                    var op1 = string.Empty;
                    var op2 = string.Empty;

                    binReader.Read(buffer, 1, 1);

                    if (CompareBits(buffer[1], MOD_MEM, 2))
                    {
                        op1 = DecodeRegister(buffer[1], REG_MASK, wBit);
                        op2 = string.Format(DecodeMemoryAddress(buffer[1]), string.Empty);
                    }
                    else if (CompareBits(buffer[1], MOD_MEM8BIT, 2))
                    {
                        binReader.Read(buffer, 2, 1);
                        op1 = DecodeRegister(buffer[1], REG_MASK, wBit);
                        op2 = string.Format(DecodeMemoryAddress(buffer[1]), " + " + buffer[2]);
                    }
                    else if (CompareBits(buffer[1], MOD_MEM16BIT, 2))
                    {
                        binReader.Read(buffer, 2, 1);
                        binReader.Read(buffer, 3, 1);
                        op1 = DecodeRegister(buffer[1], REG_MASK, wBit);
                        op2 = string.Format(DecodeMemoryAddress(buffer[1]), " + " + WordToInt(buffer[3], buffer[2]));
                    }
                    else if (CompareBits(buffer[1], MOD_REG, 2))
                    {
                        op1 = DecodeRegister(buffer[1], REG_MASK, wBit);
                        op2 = DecodeRegister(buffer[1], RM_MASK, wBit);
                    }

                    instruction += dBit ? op1 + ", " + op2 : op2 + ", " + op1;
                    file.WriteLine(instruction);
                }
                else if (CompareBits(buffer[0], MOV_IMMTOREG, 4))
                {
                    instruction += "mov ";

                    var wBit = GetBit(buffer[0], 3) ? 1 : 0;

                    var reg = DecodeRegister(buffer[0], GenerateMaskL(3), wBit);

                    binReader.Read(buffer, 1, 1);
                    var immediate = 0;
                    if (wBit == 0)
                    {
                        immediate = buffer[1];
                    }
                    else
                    {
                        binReader.Read(buffer, 2, 1);
                        immediate = WordToInt(buffer[2], buffer[1]);
                    }

                    instruction += reg + ", " + immediate;

                    file.WriteLine(instruction);
                }
            }

            file.Close();
        }

        private static string DecodeRegister(byte data, byte mask, int wide)
        {
            byte shiftedByte = 0;

            if (mask == REG_MASK)
                shiftedByte = ApplyMaskAndShiftBy(data, mask, 3);
            else if (mask == RM_MASK)
                shiftedByte = ApplyMask(data, mask);

            return registerNames[shiftedByte][wide];
        }

        private static string DecodeMemoryAddress(byte data)
        {
            byte rm = ApplyMask(data, RM_MASK);

            return addressCalculation[rm];
        }

        private static bool CompareBits(byte b1, byte b2, int n)
        {
            byte mask = GenerateMaskH(n);
            byte shiftedByte = ApplyMaskAndShiftHigher(b1, mask, n);

            return b2.Equals(shiftedByte);
        }

        private static byte GenerateMaskH(int n)
        {
            byte mask = BIT128_MASK;

            if (n == 1) return mask;

            for (int i = 1; i<n; i++)
            {
                mask >>= 1;
                mask |= BIT128_MASK;
            }

            return mask;
        }
        
        private static byte GenerateMaskL(int n)
        {
            byte mask = BIT1_MASK;

            if (n == 1) return mask;

            for (int i = 1; i<n; i++)
            {
                mask <<= 1;
                mask |= BIT1_MASK;
            }

            return mask;
        }

        public static int WordToInt(byte higher, byte lower)
        {
            int word = higher << 8 | lower;
            return word;
        }

        private static byte ApplyMaskAndShiftBy(byte data, byte mask, int n)
        {
            return (byte)(ApplyMask(data, mask) >> n);
        }

        private static byte ApplyMaskAndShiftHigher(byte data, byte mask, int n)
        {
            return (byte)(ApplyMask(data, mask) >> 8 - n);
        }

        private static byte ApplyMask(byte data, byte mask)
        {
            return (byte)(data & mask);
        }

        private static bool GetBit(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
    }
}