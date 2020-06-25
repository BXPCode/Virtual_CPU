using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;//正则表达式
namespace CPU
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //数据定义模块
        byte[] R0 = new byte[8];//R0寄存器中的数据
        byte[] R1 = new byte[8];//R1寄存器中的数据
        byte[] R2 = new byte[8];//R2寄存器中的数据
        byte[] R3 = new byte[8];//R3寄存器中的数据
        byte[] R4 = new byte[8];//R4寄存器中的数据
        byte[] R5 = new byte[8];//R5寄存器中的数据
        byte[] R6 = new byte[8];//R6寄存器中的数据
        byte[] R7 = new byte[8];//R7寄存器中的数据
        byte[] TEMP = new byte[16];//TEMP寄存器中的数据
        byte[] PC = new byte[16];//PC寄存器中的数据
        byte[] IR = new byte[16];//IR寄存器中的数据
        byte[] MAR = new byte[16];//MAR寄存器中的数据
        byte[] MDR = new byte[16];//MDR寄存器中的数据
        byte[] BUS = new byte[16];//BUS中的数据
        byte[] SR = new byte[8];//SR寄存器中的数据
        byte[] DR = new byte[8];//DR寄存器中的数据
        byte[] LA = new byte[16];//LA寄存器中的数据
        byte[] LT = new byte[16];//LT寄存器中的数据
        byte[] C0 = new byte[16];//C0
        byte[] PSW = new byte[8];//PSW寄存器中的数据
        byte H = 0;//半进位标志
        byte S = 0;//符号标志位
        byte V = 0;//有符号数溢出
        byte N = 0;//负数标志
        byte Z = 0;//0标志
        byte C = 0;//无符号数溢出标志
        byte[,] M = new byte[65536, 8];//内存
        bool FT = false;//取指周期
        bool ST = false;//取源操作数周期
        bool DT = false;//取目的操作数周期
        bool ET = false;//执行周期

        //编译模块
        //1、打开文件，逐行进行：先输入到“汇编指令”代码框中，编译，再输入到“机器指令”代码框中
        //2、将汇编指令的操作码与操作数分离，对操作码进行编译
        //3、根据操作码将操作数的源操作数（若有）、目的操作数分离
        //4、分离目的(源）操作数的寻址模式与寄存器编号，然后分别进行编译
        //5、将机器指令装载进存储器
        string asm;//汇编指令
        string[] ins = new string[100];//机器指令
        int index = 0;//指令装载序号
        private void 打开文件ToolStripMenuItem_Click(object sender, EventArgs e)//打开文件
        {
            string SFormat = null;
            string[] sformat = new string[4];
            string machla = null;
            int i;
            OpenFileDialog dlg1 = new OpenFileDialog();//创建OpenFileDialog类的对象
            dlg1.Filter = "所有文件(*.*)|*.*";//文件名筛选器
            dlg1.Multiselect = false;
            dlg1.InitialDirectory = Application.StartupPath;//设置对话框初始显示目录，为程序当前目录
            if (dlg1.ShowDialog() == DialogResult.OK)//若文件对话框显示正常
            {
                OpenFile();//控制点击“打开文件”后界面显示
                StreamReader sr = new StreamReader(dlg1.FileName);//创建StreamReader类的对象，以文本方式对流进行读操作
                index = 0;
                while (sr.Peek() > -1)//在读到文件尾前
                {
                    asm = sr.ReadLine();//每次读入一行
                    asm = asm.ToUpper();//转换为大写
                    listBox_ASM.Items.Add(asm + "\n");//添加到“汇编指令”代码框中
                    try
                    {
                        ASMtoMach(asm, ins);//转换为机器指令
                        SFormat = ins[index++];
                        machla = null;
                        for (i = 0; i < 4; i++)//控制机器指令输出格式
                        {
                            sformat[i] = SFormat.Substring(i * 4, 4);//将机器指令4位一组
                            machla += sformat[i];
                            machla += "  ";//插入空格
                        }
                        listBox_Machla.Items.Add(machla);//添加到“机器指令”代码框中                       
                    }
                    catch
                    {
                        MessageBox.Show("文件代码导入错误,请重试！", "提示");
                        打开文件ToolStripMenuItem_Click(sender, e);
                        index = 0;
                        break;
                    }
                }
                sr.Close();//关闭读写器
            }
            toolStripButton_Run.Enabled = true;//打开文件后，“启动”按钮使能有效
        }
        private void ASMtoMach(string asm, string[] ins)//将汇编指令的操作码与操作数分离，对操作码进行编译，根据操作码确定操作数编译方式
        {
            asm = new Regex("[\\s]+").Replace(asm, " ");//利用正则表达式将代码中空格都转为一个空格
            string opcode = null;//操作码字段
            string operand = null;//操作数码字段
            string operand1 = null;//目的操作数码字段
            string operand2 = null;//源操作数码字段
            string rs = null;//源操作数寄存器
            string rd = null;//目的操作数寄存器
            if (!asm.Equals("NOP"))
            {
                string[] ASM = asm.Split(' ');//汇编指令解析（第一层）：将操作码字段与操作数字段分离
                opcode = ASM[0];//操作码字段
                operand = ASM[1];//操作数字段
                if (!opcode.Equals("INC") && !opcode.Equals("DEC") && !opcode.Equals("NEG") && !opcode.Equals("JMP") && !opcode.Equals("JC"))//单操作数不用分离
                {
                    string[] OPERAND = operand.Split(',');//汇编指令解析（第二层）：操作数分离
                    operand1 = OPERAND[0];//目的操作数
                    operand2 = OPERAND[1];//源操作数
                }
            }
            else
            {
                opcode = asm;//NOP指令，只有操作码字段
            }
            switch (opcode)//操作码编码并根据不同的操作码完成地址码编码
            {
                case "ADD":
                    ins[index] += "0001";//双操作数加法                   
                    codeM(operand2, ref rs, ins);//源操作数寻址模式
                    codeR(rs, ins);//源操作数寄存器
                    codeM(operand1, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "SUB":
                    ins[index] += "0010";//双操作数减法
                    codeM(operand2, ref rs, ins);//源操作数寻址模式
                    codeR(rs, ins);//源操作数寄存器
                    codeM(operand1, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "AND":
                    ins[index] += "0011";//逻辑乘
                    codeM(operand2, ref rs, ins);//源操作数寻址模式
                    codeR(rs, ins);//源操作数寄存器
                    codeM(operand1, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "INC":
                    ins[index] += "0100";//单操作数加1
                    ins[index] += "000000";
                    codeM(operand, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "DEC":
                    ins[index] += "0101";//单操作数减1
                    ins[index] += "000000";
                    codeM(operand, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "NEG":
                    ins[index] += "0110";//求补码
                    ins[index] += "000000";
                    codeM(operand, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "JMP":
                    ins[index] += "0111";//无条件相对跳转
                    ins[index] += "000000000";
                    codeR(operand, ins);//目的操作数寄存器
                    break;
                case "JC":
                    ins[index] += "1000";//有进位跳转
                    ins[index] += "000000000";
                    codeR(operand, ins);//目的操作数寄存器
                    break;
                case "MOV":
                    ins[index] += "1010";//数据传送
                    codeM(operand2, ref rs, ins);//源操作数寻址模式
                    codeR(rs, ins);//源操作数寄存器
                    codeM(operand1, ref rd, ins);//目的操作数寻址模式
                    codeR(rd, ins);//目的操作数寄存器
                    break;
                case "LDI":
                    ins[index] += "1110";//载入立即数
                    ins[index] += HtoB(operand2);
                    ins[index] += "0";
                    codeR(operand1, ins);//目的操作数寄存器
                    break;
                case "LD":
                    ins[index] += "1001";//装载指令
                    ins[index] += HtoB(operand2);
                    ins[index] += "1";
                    codeR(operand1, ins);//目的操作数寄存器
                    break;
                case "NOP":
                    ins[index] += "0000000000000000";//空操作
                    break;
                default:
                    break;
            }
        }
        private void codeM(string operand, ref string r, string[] ins)//寻址方式解析
        {
            if (new Regex("^[A-Za-z0-9]+$").IsMatch(operand))//寄存器寻址
            {
                ins[index] += "000";
                r = operand;
            }
            else if (new Regex("^[(]+[A-Za-z0-9]+[)]+$").IsMatch(operand))//寄存器间址
            {
                ins[index] += "001";
                r = operand.Substring(1, 2);
            }
            else if (new Regex("^[(]+[A-Za-z0-9]+[)]+[+]+$").IsMatch(operand))//自增型寄存器间址
            {
                ins[index] += "010";
                r = operand.Substring(1, 2);
            }
            else if (new Regex("^[A-Za-z0-9]+[(]+[A-Za-z0-9]+[)]+$").IsMatch(operand))//自增型双间址
            {
                ins[index] += "100";
                r = operand.Substring(2, 2);
            }
            else if (new Regex("^[@]+[(]+[A-Za-z0-9]+[)]+[+]+$").IsMatch(operand))//变址寻址
            {
                ins[index] += "011";
                r = operand.Substring(2, 2);
            }
        }
        private void codeR(string r, string[] ins)//寄存器编号解析
        {
            switch (r)
            {
                case "R0":
                    ins[index] += "000";
                    break;
                case "R1":
                    ins[index] += "001";
                    break;
                case "R2":
                    ins[index] += "010";
                    break;
                case "R3":
                    ins[index] += "011";
                    break;
                case "R4":
                    ins[index] += "100";
                    break;
                case "R5":
                    ins[index] += "101";
                    break;
                case "R6":
                    ins[index] += "110";
                    break;
                case "R7":
                    ins[index] += "111";
                    break;
                default:
                    break;
            }
        }
        private void InstoM()//指令装载进指令存储器
        {
            int i, j, k;
            int indexM = ARRAYtoNUM(PC);
            line = lineStart = indexM;
            for (i = 0; i < index; i++)
            {
                for (k = 0; k < 2; k++)
                {
                    for (j = 0; j < 8; j++)
                    {
                        M[indexM, 7 - j] = byte.Parse(ins[i][j + k * 8].ToString());
                    }
                    indexM++;
                }
            }
        }
        //辅助函数
        private string HtoB(string H)//十六进制转为二进制
        {
            int index = 1;
            string B = null;
            string[] h = new string[2];
            if (H.Length == 1)
            {
                h[1] = "0";
                h[0] = H;
            }
            else
            {
                h[1] = H.Substring(0, 1);
                h[0] = H.Substring(1, 1);
            }
            while (index >= 0)
            {
                switch (h[index--])
                {
                    case "0":
                        B += "0000";
                        break;
                    case "1":
                        B += "0001";
                        break;
                    case "2":
                        B += "0010";
                        break;
                    case "3":
                        B += "0011";
                        break;
                    case "4":
                        B += "0100";
                        break;
                    case "5":
                        B += "0101";
                        break;
                    case "6":
                        B += "0110";
                        break;
                    case "7":
                        B += "0111";
                        break;
                    case "8":
                        B += "1000";
                        break;
                    case "9":
                        B += "1001";
                        break;
                    case "A":
                        B += "1010";
                        break;
                    case "B":
                        B += "1011";
                        break;
                    case "C":
                        B += "1100";
                        break;
                    case "D":
                        B += "1101";
                        break;
                    case "E":
                        B += "1110";
                        break;
                    case "F":
                        B += "1111";
                        break;
                    default:
                        break;
                }
            }
            return B;
        }
        private string BtoH(byte[] B)//二进制转为十六进制
        {
            string H = null;
            int hex = 0;
            int i, j;
            int t = 0;
            for (i = (B.Length) - 4; i >= 0; i -= 4)
            {
                t = 1;
                hex = 0;
                for (j = 0; j < 4; j++)
                {
                    hex += t * B[j + i];
                    t *= 2;
                }
                switch (hex)
                {
                    case 10: H += "A"; break;
                    case 11: H += "B"; break;
                    case 12: H += "C"; break;
                    case 13: H += "D"; break;
                    case 14: H += "E"; break;
                    case 15: H += "F"; break;
                    default: H += hex.ToString(); break;
                }
            }
            return H;
        }

        //微程序控制模块
        //1、按操作数数目将指令分为双操作数、单操作数、零操作数和NOP指令，分别设置不同的指令控制逻辑（主要是ST、DT周期的有无）
        //2、按寻址方式和操作码字段分别设置各机器周期内的微操作序列
        //3、微操作的具体实现
        //单微指令执行控制逻辑
        int p = 0;
        int t = 0;
        private void RunBYMicOrder()
        {
            CloseLabelVisable();
            CloselabelTime();
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insOP = instruct.Substring(0, 4);//分割出操作码字段
            switch (insOP)//根据操作码字段，执行对应的指令控制逻辑
            {
                case "0001":
                    ControlDouble();
                    break;
                case "0010":
                    ControlDouble();
                    break;
                case "0011":
                    ControlDouble();
                    break;
                case "0100":
                    ControlSingle();
                    break;
                case "0101":
                    ControlSingle();
                    break;
                case "0110":
                    ControlSingle();
                    break;
                case "0111":
                    ControlNone();
                    break;
                case "1000":
                    ControlNone();
                    break;
                case "1010":
                    ControlMOV();
                    break;
                case "1110":
                    ControlNone();
                    break;
                case "1001":
                    ControlNone();
                    break;
                case "0000":
                    ControlNOP();
                    break;
            }
            if (((line - lineStart) / 2) < listBox_ASM.Items.Count)//选中对应的代码
            {
                listBox_ASM.SelectedIndex = (line - lineStart) / 2;
                listBox_Machla.SelectedIndex = listBox_ASM.SelectedIndex;
            }
            if (p == 3)
            {
                WriteLog();
            }
            p = (++p) % 4;
        }
        private void ControlDouble() //双操作数指令控制逻辑
        {
            if (FT == true)//取指周期
            {
                FTime();
                label_FT.Visible = true;
            }
            else if (ST == true)//取源操作数周期
            {
                STanalyze();
                label_ST.Visible = true;
            }
            else if (DT == true)//取目的操作数周期
            {
                DTanalyze();
                label_DT.Visible = true;
            }
            else if (ET == true)//执行周期
            {
                ETanalyze();
                label_ET.Visible = true;
            }
        }
        private void ControlSingle()//单操作数指令控制逻辑
        {
            if (FT == true)//取指周期
            {
                FTime();
                if (ST == true)
                {
                    OnetoDT();
                    label_OnetoST.Visible = false;
                }
                label_FT.Visible = true;
            }
            else if (DT == true)//取目的操作数周期
            {
                DTDRanalyze();
                label_DT.Visible = true;
            }
            else if (ET == true)//执行周期
            {
                ETanalyze();
                label_ET.Visible = true;
            }
        }
        private void ControlNone()//零操作数指令控制逻辑（JMP\JC\MOV\LDI\LD)
        {
            if (FT == true)//取指周期
            {
                FTime();
                if (ST == true)
                {
                    OnetoET();
                    label_OnetoST.Visible = false;
                }
                label_FT.Visible = true;
            }
            else if (ET == true)//执行周期
            {
                ETanalyze();
                label_ET.Visible = true;
            }
        }
        private void ControlMOV()//MOV指令控制逻辑
        {
            if (FT == true)//取指周期
            {
                FTime();
                label_FT.Visible = true;
            }
            else if (ST == true)//取源操作数周期
            {
                STanalyze();
                if (DT == true)
                {
                    OnetoET();
                    label_OnetoDT.Visible = false;
                }
                label_ST.Visible = true;
            }
            else if (ET == true)//执行周期
            {
                ETanalyze();
                label_ET.Visible = true;
            }
        }
        private void ControlNOP()//NOP指令控制逻辑
        {
            if (FT == true)//取指周期
            {
                FTime();
                if (ST == true)
                {
                    OnetoFT();
                    label_OnetoST.Visible = false;
                }
                label_FT.Visible = true;
            }
        }
        //将微指令按周期封装（主要按寻址方式）
        private void FTime()//FT周期微操作序列
        {
            switch (p)
            {
                case 0:
                    PCtoBUS();
                    BUStoMAR();
                    READ();
                    CLEARLA();
                    OnetoC0(1, 0);
                    ADD(BUS, C0);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    BUStoPC();
                    WAIT();
                    break;
                case 2:
                    MDRtoBUS();
                    BUStoIR();
                    break;
                case 3:
                    OnetoST();
                    label_FT.Visible = true;
                    break;
                default:
                    break;
            }
        }
        private void ModeRtoBUS(int x)//根据寄存器编号判定寄存器传入BUS
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insRS = instruct.Substring(x, 3);
            switch (insRS)
            {
                case "000":
                    R0toBUS();
                    break;
                case "001":
                    R1toBUS();
                    break;
                case "010":
                    R2toBUS();
                    break;
                case "011":
                    R3toBUS();
                    break;
                case "100":
                    R4toBUS();
                    break;
                case "101":
                    R5toBUS();
                    break;
                case "110":
                    R6toBUS();
                    break;
                case "111":
                    R7toBUS();
                    break;
                default:
                    break;
            }
        }
        private void ModeBUStoR(int x)//根据寄存器编号判定BUS传入寄存器
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insRS = instruct.Substring(x, 3);
            switch (insRS)
            {
                case "000":
                    BUStoR0();
                    break;
                case "001":
                    BUStoR1();
                    break;
                case "010":
                    BUStoR2();
                    break;
                case "011":
                    BUStoR3();
                    break;
                case "100":
                    BUStoR4();
                    break;
                case "101":
                    BUStoR5();
                    break;
                case "110":
                    BUStoR6();
                    break;
                case "111":
                    BUStoR7();
                    break;
                default:
                    break;
            }
        }
        private void RaSTime()//源操作数寄存器寻址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(7);
                    BUStoSR();
                    break;
                case 1:
                    label_vans.Visible = true;
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoDT();
                    break;
                default:
                    break;
            }
        }
        private void RbSTime()//源操作数寄存器间址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(7);
                    BUStoMAR();
                    READ();
                    WAIT();
                    break;
                case 1:
                    MDRtoBUS();
                    BUStoSR();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoDT();
                    break;
                default:
                    break;
            }
        }
        private void RcSTime()//源操作数自增型寄存器间址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(7);
                    BUStoMAR();
                    READ();
                    CLEARLA();
                    OnetoC0(0, 1);
                    ADD(BUS, C0);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    ModeBUStoR(7);
                    WAIT();
                    break;
                case 2:
                    MDRtoBUS();
                    BUStoSR();
                    break;
                case 3:
                    OnetoDT();
                    break;
                default:
                    break;
            }
        }
        private void RdSTime()//源操作数自增型双间址
        {
            if (t == 0)
            {
                switch (p)
                {
                    case 0:
                        ModeRtoBUS(7);
                        BUStoMAR();
                        READ();
                        CLEARLA();
                        OnetoC0(0, 1);
                        ADD(BUS, C0);
                        ALUtoLT();
                        break;
                    case 1:
                        LTtoBUS();
                        ModeBUStoR(7);
                        WAIT();
                        break;
                    case 2:
                        MDRtoBUS();
                        BUStoTEMP();
                        break;
                    case 3:
                        OnetoST();
                        t = 1;
                        break;
                    default:
                        break;
                }
            }
            else if (t == 1)
            {
                switch (p)
                {
                    case 0:
                        TEMPtoBUS();
                        BUStoMAR();
                        READ();
                        WAIT();
                        break;
                    case 1:
                        MDRtoBUS();
                        CLEARLA();
                        OnetoC0(0, 1);
                        ADD(BUS, C0);
                        ALUtoLT();
                        break;
                    case 2:
                        LTtoBUS();
                        BUStoSR();
                        break;
                    case 3:
                        OnetoDT();
                        t = 0;
                        break;
                    default:
                        break;
                }
            }
        }
        private void ReSTime()//源操作数变址寻址
        {
            switch (p)
            {
                case 0:
                    PCtoBUS();
                    BUStoLA();
                    OnetoC0(1, 0);
                    ADD(LA, C0);
                    ALUtoLT();
                    LTtoBUS();
                    break;
                case 1:
                    BUStoPC();
                    ModeRtoBUS(13);
                    ADD(LA, BUS);
                    ALUtoLT();
                    LTtoBUS();
                    BUStoMAR();
                    READ();
                    break;
                case 2:
                    WAIT();
                    MDRtoBUS();
                    BUStoSR();
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void STanalyze()//ST周期双操作数微操作序列
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insMS = instruct.Substring(4, 3);
            switch (insMS)
            {
                case "000":
                    RaSTime();//寄存器寻址
                    break;
                case "001":
                    RbSTime();//寄存器间址
                    break;
                case "010":
                    RcSTime();//自增型寄存器间址
                    break;
                case "011":
                    RdSTime();//自增型双间址
                    break;
                case "100":
                    ReSTime();//变址寻址
                    break;
                default:
                    break;
            }
        }
        private void RaDTime()//目的操作数寄存器寻址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(13);
                    BUStoLA();
                    break;
                case 1:
                    label_vans.Visible = true;
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void RbDTime()//目的操作数寄存器间址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(13);
                    BUStoMAR();
                    READ();
                    WAIT();
                    break;
                case 1:
                    MDRtoBUS();
                    BUStoLA();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void RcDTime()//目的操作数自增型寄存器间址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(13);
                    BUStoMAR();
                    READ();
                    CLEARLA();
                    OnetoC0(0, 1);
                    ADD(BUS, C0);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    ModeBUStoR(13);
                    WAIT();
                    break;
                case 2:
                    MDRtoBUS();
                    BUStoLA();
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void RdDTime()//目的操作数自增型双间址
        {
            if (t == 0)
            {
                switch (p)
                {
                    case 0:
                        ModeRtoBUS(13);
                        BUStoMAR();
                        READ();
                        CLEARLA();
                        OnetoC0(0, 1);
                        ADD(BUS, C0);
                        ALUtoLT();
                        break;
                    case 1:
                        LTtoBUS();
                        ModeBUStoR(13);
                        WAIT();
                        break;
                    case 2:
                        MDRtoBUS();
                        BUStoTEMP();
                        break;
                    case 3:
                        OnetoDT();
                        t = 1;
                        break;
                    default:
                        break;
                }
            }
            else if (t == 1)
            {
                switch (p)
                {
                    case 0:
                        TEMPtoBUS();
                        BUStoMAR();
                        READ();
                        WAIT();
                        break;
                    case 1:
                        MDRtoBUS();
                        CLEARLA();
                        OnetoC0(0, 1);
                        ADD(BUS, C0);
                        ALUtoLT();
                        break;
                    case 2:
                        LTtoBUS();
                        BUStoLA();
                        break;
                    case 3:
                        OnetoDT();
                        t = 0;
                        break;
                    default:
                        break;
                }
            }
        }
        private void ReDTime()//目的操作数变址寻址
        {
            switch (p)
            {
                case 0:
                    PCtoBUS();
                    BUStoLA();
                    OnetoC0(1, 0);
                    ADD(LA, C0);
                    ALUtoLT();
                    LTtoBUS();
                    break;
                case 1:
                    BUStoPC();
                    ModeRtoBUS(13);
                    ADD(LA, BUS);
                    ALUtoLT();
                    LTtoBUS();
                    BUStoMAR();
                    READ();
                    break;
                case 2:
                    WAIT();
                    MDRtoBUS();
                    BUStoLA();
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void DTanalyze()//DT周期双操作数微操作序列
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insMD = instruct.Substring(10, 3);
            switch (insMD)
            {
                case "000":
                    RaDTime();//寄存器寻址
                    break;
                case "001":
                    RbDTime();//寄存器间址
                    break;
                case "010":
                    RcDTime();//自增型寄存器间址
                    break;
                case "011":
                    RdDTime();//自增型双间址
                    break;
                case "100":
                    ReDTime();//变址寻址
                    break;
                default:
                    break;
            }
        }
        private void DRaDTime()//单操作数寄存器寻址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(13);
                    BUStoDR();
                    break;
                case 1:
                    label_vans.Visible = true;
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void DRbDTime()//单操作数寄存器间址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(13);
                    BUStoMAR();
                    READ();
                    WAIT();
                    break;
                case 1:
                    MDRtoBUS();
                    BUStoDR();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void DRcDTime()//单操作数自增型寄存器间址
        {
            switch (p)
            {
                case 0:
                    ModeRtoBUS(13);
                    BUStoMAR();
                    READ();
                    CLEARLA();
                    OnetoC0(0, 1);
                    ADD(BUS, C0);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    ModeBUStoR(13);
                    WAIT();
                    break;
                case 2:
                    MDRtoBUS();
                    BUStoDR();
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void DRdDTime()//单操作数自增型双间址
        {
            if (t == 0)
            {
                switch (p)
                {
                    case 0:
                        ModeRtoBUS(13);
                        BUStoMAR();
                        READ();
                        CLEARLA();
                        OnetoC0(0, 1);
                        ADD(BUS, C0);
                        ALUtoLT();
                        break;
                    case 1:
                        LTtoBUS();
                        ModeBUStoR(13);
                        WAIT();
                        break;
                    case 2:
                        MDRtoBUS();
                        BUStoTEMP();
                        break;
                    case 3:
                        OnetoDT();
                        t = 1;
                        break;
                    default:
                        break;
                }
            }
            else if (t == 1)
            {
                switch (p)
                {
                    case 0:
                        TEMPtoBUS();
                        BUStoMAR();
                        READ();
                        WAIT();
                        break;
                    case 1:
                        MDRtoBUS();
                        BUStoDR();
                        break;
                    case 2:
                        label_vans.Visible = true;
                        break;
                    case 3:
                        OnetoET();
                        t = 0;
                        break;
                    default:
                        break;
                }
            }
        }
        private void DReDTime()//单操作数变址寻址
        {
            switch (p)
            {
                case 0:
                    PCtoBUS();
                    BUStoLA();
                    OnetoC0(1, 0);
                    ADD(LA, C0);
                    ALUtoLT();
                    LTtoBUS();
                    BUStoPC();
                    break;
                case 1:
                    ModeRtoBUS(13);
                    ADD(LA, BUS);
                    ALUtoLT();
                    LTtoBUS();
                    BUStoMAR();
                    READ();
                    break;
                case 2:
                    WAIT();
                    MDRtoBUS();
                    BUStoDR();
                    break;
                case 3:
                    OnetoET();
                    break;
                default:
                    break;
            }
        }
        private void DTDRanalyze()//DT周期单操作数微操作序列
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insMD = instruct.Substring(10, 3);//寻址方式字段
            switch (insMD)
            {
                case "000":
                    DRaDTime();//寄存器寻址
                    break;
                case "001":
                    DRbDTime();//寄存器间址
                    break;
                case "010":
                    DRcDTime();//自增型寄存器间址
                    break;
                case "011":
                    DRdDTime();//自增型双间址
                    break;
                case "100":
                    DReDTime();//变址寻址
                    break;
                default:
                    break;
            }
        }
        private void ADDETime()//双操作数加法ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_ADDC.Visible = true;
                    SRtoBUS();
                    ADD(LA, BUS);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    judgeRorM();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void SUBETime()//双操作数减法ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_SUBC.Visible = true;
                    SRtoBUS();
                    SUB(LA, BUS);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    judgeRorM();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void ANDETime()//逻辑乘法ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_ANDC.Visible = true;
                    SRtoBUS();
                    AND(LA, BUS);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    judgeRorM();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void INCETime()//单操作数加1指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_INCC.Visible = true;
                    DRtoBUS();
                    CLEARLA();
                    OnetoC0(0, 1);
                    INC(BUS);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    judgeRorM();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void DECETime()//单操作数减1指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_DECC.Visible = true;
                    DRtoBUS();
                    CLEARLA();
                    OnetoC0(0, 1);
                    DEC(BUS);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    judgeRorM();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void NECETime()//求补码指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_NECC.Visible = true;
                    DRtoBUS();
                    CLEARLA();
                    OnetoC0(0, 1);
                    NEC(BUS);
                    ALUtoLT();
                    break;
                case 1:
                    LTtoBUS();
                    judgeRorM();
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void JMPETime()//无条件相对跳转指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_JMPC.Visible = true;
                    ModeRtoBUS(13);
                    BUStoPC();
                    break;
                case 1:
                    label_vans.Visible = true;
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void JCETime()//有进位跳转指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_JCC.Visible = true;
                    if (H == 1)
                    {
                        ModeRtoBUS(13);
                        BUStoPC();
                    }
                    break;
                case 1:
                    label_vans.Visible = true;
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void MOVETime()//数据传送指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_MOVC.Visible = true;
                    SRtoBUS();
                    break;
                case 1:
                    ModeBUStoR(13);
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void LDIETime()//载入立即数指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_LDIC.Visible = true;
                    byte[] insimdi = new byte[8];
                    KtoNUM(insimdi);
                    toBUS(insimdi);
                    break;
                case 1:
                    ModeBUStoR(13);
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void LDETime()//装载指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_LDC.Visible = true;
                    byte[] insimdi = new byte[8];
                    KtoNUM(insimdi);
                    toBUS(insimdi);
                    BUStoMAR();
                    READ();
                    break;
                case 1:
                    WAIT();
                    break;
                case 2:
                    MDRtoBUS();
                    ModeBUStoR(13);
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void NOPETime()//空操作指令ET周期微操作序列
        {
            switch (p)
            {
                case 0:
                    label_NOPC.Visible = true;
                    break;
                case 1:
                    label_vans.Visible = true;
                    break;
                case 2:
                    label_vans.Visible = true;
                    break;
                case 3:
                    OnetoFT();
                    break;
                default:
                    break;
            }
        }
        private void ETanalyze()//ET周期微操作序列
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insOP = instruct.Substring(0, 4);//操作码字段
            switch (insOP)
            {
                case "0001":
                    ADDETime();//双操作数加法
                    break;
                case "0010":
                    SUBETime();//双操作数减法
                    break;
                case "0011":
                    ANDETime();//逻辑乘法
                    break;
                case "0100":
                    INCETime();//单操作数加1
                    break;
                case "0101":
                    DECETime();//单操作数减1
                    break;
                case "0110":
                    NECETime();//求补码
                    break;
                case "0111":
                    JMPETime();//无条件相对跳转
                    break;
                case "1000":
                    JCETime();//有进位跳转
                    break;
                case "1010":
                    MOVETime();//数据传送
                    break;
                case "1110":
                    LDIETime();//载入立即数
                    break;
                case "1001":
                    LDETime();//装载指令
                    break;
                case "0000":
                    NOPETime();//空操作
                    break;
                default:
                    break;
            }
        }
        //微操作阵列
        private void LTtoBUS()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                BUS[i] = LT[i];
            }
            label_LTtoBUS.Visible = true;
        }
        private void ALUtoLT()
        {
            label_ALUtoLT.Visible = true;
        }
        private void BUStoLA()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                LA[i] = BUS[i];
            }
            label_BUStoLA.Visible = true;
        }
        private void OnetoC0(byte x, byte y)//1送C0，根据PC+1还是INC运算+1设置不同的C0值
        {
            C0[1] = x;
            C0[0] = y;
            label_1toC0.Visible = true;
        }
        private void CLEARLA()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                LA[i] = 0;
            }
            label_CLEARLA.Visible = true;
        }
        private void ADD(byte[] A1, byte[] A2)//双操作数加法指令
        {
            int i;
            byte cf = 0;//低位向高位的进位
            for (i = 0; i < 16; i++)
            {
                if (i >= 8)
                {
                    cf = 0;
                }
                LT[i] = (byte)(A1[i] ^ A2[i] ^ cf);
                if ((A1[i] & A2[i]) == 1 || (A1[i] & cf) == 1 || (A2[i] & cf) == 1)
                {
                    cf = 1;
                }
                else
                {
                    cf = 0;
                }
                if (i == 3)
                {
                    PSW[5] = H = cf;//半进位标志
                }
            }
            if ((A1[7] == 0 && A2[7] == 0 && LT[7] == 1) || (A1[7] == 1 && A2[7] == 1 && LT[7] == 0))
            {
                PSW[3] = V = 1;//有符号数溢出标志位
            }
            if (LT[7] == 1)
            {
                PSW[2] = N = 1;
            }
            JudgeZero(LT);//0标志位
            PSW[4] = S = (byte)(N ^ V);//S符号标志位
            RtoM(PSW, 8);
            label_ADD.Visible = true;
        }
        private void SUB(byte[] A1, byte[] A2)//双操作数减法指令
        {
            byte[] compl = new byte[16];
            compl = Complement(A2);
            ADD(A1, compl);
            label_ADD.Visible = false;
            label_SUB.Visible = true;
        }
        private void AND(byte[] A1, byte[] A2)//逻辑乘指令
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                LT[i] = (byte)(A1[i] ^ A2[i]);
            }
            JudgeZero(LT);
            label_AND.Visible = true;
        }
        private void INC(byte[] A1)//单操作数加1
        {
            byte[] plus1 = new byte[16] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            ADD(A1, plus1);
            label_ADD.Visible = false;
            label_INC.Visible = true;
        }
        private void DEC(byte[] A1)//单操作数减1
        {
            byte[] plus1 = new byte[16] { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            ADD(A1, plus1);
            label_ADD.Visible = false;
            label_DEC.Visible = true;
        }
        private void NEC(byte[] A1)//求补码指令
        {
            byte[] InverseCode = new byte[8];
            byte qf = 00000001;//求反码时异或用
            byte[] plus1 = new byte[16] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };//求补码时加1使用
            int i;
            if (A1[7] == 1)//负数，取反加1
            {
                InverseCode[7] = 1;
                for (i = 0; i < 7; i++)
                {
                    InverseCode[i] = (byte)(A1[i] ^ qf);
                }
                ADD(InverseCode, plus1);
                label_ADD.Visible = false;
            }
            else//正数，不变
            {
                for (i = 0; i < 8; i++)
                {
                    LT[i] = A1[i];
                }
            }
            label_NEC.Visible = false;
        }
        private void BUStoMAR()
        {
            int i = 0;
            for (i = 0; i < 16; i++)
            {
                MAR[i] = BUS[i];
            }
            label_BUStoMAR.Visible = true;
        }
        private void BUStoMDR()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                MDR[i] = BUS[i];
            }
            label_BUStoMDR.Visible = true;
        }
        private void MDRtoBUS()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                BUS[i] = MDR[i];
            }
            label_MDRtoBUS.Visible = true;
        }
        private void READ()
        {
            int i, j;
            int indexM = ARRAYtoNUM(MAR);
            if (FT == true)
            {
                line = indexM;//控制对应代码的显示
                for (i = 0; i < 2; i++)
                {
                    for (j = 0; j < 8; j++)
                    {
                        MDR[j + (1 - i) * 8] = M[indexM, j];
                    }
                    indexM++;
                }
            }
            else
            {
                for (i = 0; i < 8; i++)
                {
                    MDR[i] = M[indexM, i];
                }
                for (i = 8; i < 16; i++)
                {
                    MDR[i] = 0;
                }
            }
            label_READ.Visible = true;
        }
        private void WRITE()
        {
            int i;
            int indexM = ARRAYtoNUM(MAR);
            for (i = 0; i < 8; i++)
            {
                M[indexM, i] = MDR[i];
            }
            if (indexM <= 8)//同步内存与对应寄存器的值，实现统一编址
            {
                MtoR(indexM);
            }
            label_WRITE.Visible = true;
        }
        private void WAIT()
        {
            label_WAITE.Visible = true;
        }
        private void PCtoBUS()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                BUS[i] = PC[i];
            }
            label_PCtoBUS.Visible = true;
        }
        private void BUStoPC()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                PC[i] = BUS[i];
            }
            label_BUStoPC.Visible = true;
        }
        private void BUStoIR()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                IR[i] = BUS[i];
            }
            label_BUStoIR.Visible = true;
        }
        private void R0toBUS()
        {
            toBUS(R0);
            label_R0toBUS.Visible = true;
        }
        private void R1toBUS()
        {
            toBUS(R1);
            label_R1toBUS.Visible = true;
        }
        private void R2toBUS()
        {
            toBUS(R2);
            label_R2toBUS.Visible = true;
        }
        private void R3toBUS()
        {
            toBUS(R3);
            label_R3toBUS.Visible = true;
        }
        private void R4toBUS()
        {
            toBUS(R4);
            label_R4toBUS.Visible = true;
        }
        private void R5toBUS()
        {
            toBUS(R5);
            label_R5toBUS.Visible = true;
        }
        private void R6toBUS()
        {
            toBUS(R6);
            label_R6toBUS.Visible = true;
        }
        private void R7toBUS()
        {
            toBUS(R7);
            label_R7toBUS.Visible = true;
        }
        private void SRtoBUS()
        {
            toBUS(SR);
            label_SRtoBUS.Visible = true;
        }
        private void DRtoBUS()
        {
            toBUS(DR);
            label_DRtoBUS.Visible = true;
        }
        private void TEMPtoBUS()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                BUS[i] = TEMP[i];
            }
            label_TEMPtoBUS.Visible = true;
        }
        private void BUStoR0()
        {
            BUSto(R0);
            RtoM(R0, 0);
            label_BUStoR0.Visible = true;
        }
        private void BUStoR1()
        {
            BUSto(R1);
            RtoM(R1, 1);
            label_BUStoR1.Visible = true;
        }
        private void BUStoR2()
        {
            BUSto(R2);
            RtoM(R2, 2);
            label_BUStoR2.Visible = true;
        }
        private void BUStoR3()
        {
            BUSto(R3);
            RtoM(R3, 3);
            label_BUStoR3.Visible = true;
        }
        private void BUStoR4()
        {
            BUSto(R4);
            RtoM(R4, 4);
            label_BUStoR4.Visible = true;
        }
        private void BUStoR5()
        {
            BUSto(R5);
            RtoM(R5, 5);
            label_BUStoR5.Visible = true;
        }
        private void BUStoR6()
        {
            BUSto(R6);
            RtoM(R6, 6);
            label_BUStoR6.Visible = true;
        }
        private void BUStoR7()
        {
            BUSto(R7);
            RtoM(R7, 7);
            label_BUStoR7.Visible = true;
        }
        private void BUStoSR()
        {
            BUSto(SR);
            label_BUStoSR.Visible = true;
        }
        private void BUStoDR()
        {
            BUSto(DR);
            label_BUStoDR.Visible = true;
        }
        private void BUStoTEMP()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                TEMP[i] = BUS[i];
            }
            label_BUStoTEMP.Visible = true;
        }
        private void OnetoST()
        {
            FT = false;
            ST = true;
            DT = false;
            ET = false;
            label_OnetoST.Visible = true;
        }
        private void OnetoDT()
        {
            FT = false;
            ST = false;
            DT = true;
            ET = false;
            label_OnetoDT.Visible = true;
        }
        private void OnetoET()
        {
            FT = false;
            ST = false;
            DT = false;
            ET = true;
            label_OnetoET.Visible = true;
        }
        private void OnetoFT()
        {
            FT = true;
            ST = false;
            DT = false;
            ET = false;
            label_OnetoFT.Visible = true;
        }
        //辅助函数
        private void toBUS(byte[] R)//寄存器值送总线
        {
            int i = 0;
            for (i = 0; i < 8; i++)
            {
                BUS[i] = R[i];
            }
            for (i = 8; i < 16; i++)
            {
                BUS[i] = 0;
            }
        }
        private void BUSto(byte[] R)//总线上的值送寄存器
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                R[i] = BUS[i];
            }
        }
        private int ARRAYtoNUM(byte[] A)//地址换算逻辑
        {
            int length = A.Length;
            int i, t, s = 0;
            t = 1;
            for (i = 0; i < A.Length; i++)//将二进制地址码转为十进制内存数组行下标
            {
                s += A[i] * t;
                t *= 2;
            }
            return s;
        }
        private void KtoNUM(byte[] insimdi)//将kkkkkkkk转为立即数
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            int i = 0;
            int j = 7;
            for (i = 4; i < 12; i++)
            {
                insimdi[j--] = byte.Parse(instruct.Substring(i, 1));
            }
        }
        private void JudgeZero(byte[] LT)//判断运算结果是否为0
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                if (LT[i] != 0)
                {
                    PSW[1] = Z = 0;
                    return;
                }
            }
            PSW[1] = Z = 1;
        }
        private byte[] Complement(byte[] T)//减数取负转为补码运算
        {
            byte[] Compl = new byte[16];
            byte[] InverseCode = new byte[16];
            byte qf = 00000001;//求反码时异或用
            byte[] plus1 = new byte[8] { 1, 0, 0, 0, 0, 0, 0, 0 };//求补码时加1使用
            byte cf = 0;
            int i;
            InverseCode[7] = 1;
            for (i = 0; i < 7; i++)//求反码
            {
                InverseCode[i] = (byte)(T[i] ^ qf);
            }
            Compl[7] = 1;
            for (i = 0; i < 7; i++)//加1
            {
                Compl[i] = (byte)(InverseCode[i] ^ plus1[i] ^ cf);
                if ((InverseCode[i] & plus1[i]) == 1 || (InverseCode[i] & cf) == 1 || (plus1[i] & cf) == 1)
                {
                    cf = 1;
                }
                else
                {
                    cf = 0;
                }
            }
            return Compl;
        }
        private void ArraytoString(byte[] a, ref string s)//将数组里面的数提取为字符串，便于分割出子串进行操作
        {
            int i;
            for (i = 15; i >= 0; i--)
            {
                s += (string)(a[i].ToString());
            }
        }
        private void judgeRorM()//判断ET周期写寄存器还是写存储器
        {
            string instruct = null;
            ArraytoString(IR, ref instruct);
            string insMD = instruct.Substring(10, 3);
            switch (insMD)
            {
                case "000":
                    ModeBUStoR(13);
                    break;
                case "001":
                case "010":
                case "011":
                case "100":
                    BUStoMDR();
                    WRITE();
                    WAIT();
                    break;
                default:
                    break;
            }
        }
        //内存统一编址实现
        private void RtoM(byte[] R, int x)//寄存器值更新时，对应内存空间的值同步更新
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                M[x, i] = R[i];
            }
        }
        private void MtoR(int x)//内存空间的值更新时，对应寄存器的值同步更新
        {

            switch (x)
            {
                case 0:
                    mtor(0, R0);
                    break;
                case 1:
                    mtor(1, R1);
                    break;
                case 2:
                    mtor(2, R2);
                    break;
                case 3:
                    mtor(3, R3);
                    break;
                case 4:
                    mtor(4, R4);
                    break;
                case 5:
                    mtor(5, R5);
                    break;
                case 6:
                    mtor(6, R6);
                    break;
                case 7:
                    mtor(7, R7);
                    break;
                case 8:
                    mtor(8, PSW);
                    break;
                default:
                    break;
            }
        }
        private void mtor(int x, byte[] R)
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                R[i] = M[x, i];
            }
        }

        //交互控制模块
        //1、菜单和工具条按钮功能定义
        //2、用户在点击"新建”后，可以在汇编指令代码框中输入汇编指令代码，实现用户即时编程功能
        //3、界面刷新
        bool Saved = false;//程序日志文件是否保存
        private void 保存文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg2 = new SaveFileDialog();
            dlg2.Filter = "文本文件(*.txt)|*.txt|全部文件(*.*)|*.*";//文件名筛选器
            dlg2.InitialDirectory = Application.StartupPath;//设置对话框初始显示目录，为程序当前目录
            if (dlg2.ShowDialog() == DialogResult.OK)
            {
                sw.Close();//关闭sw读写器
                fs.Close();//关闭文件流
                StreamReader sr2 = new StreamReader("Temp.txt");//将temp文件中的内容读出
                StreamWriter sw2 = new StreamWriter(dlg2.FileName);//创建StreamWriter类的对象，以文本方式对流进行写操作
                sw2.Write(sr2.ReadToEnd());//写入用户要保存的文件
                sw2.Close();//关闭sw2读写器
                sr2.Close();//关闭sr2读写器
                Saved = true;//代表日志文件已保存
            }
        }
        private void 关闭ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Saved == false)
            {
                DialogResult dr = MessageBox.Show("是否保存指令运行过程的日志文件", "提示", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Yes)
                {
                    保存文件ToolStripMenuItem_Click(sender, e);
                    File.Delete("temp.txt");
                    this.Close();
                }
                else if (dr == DialogResult.No)
                {
                    sw.Close();
                    fs.Close();
                    File.Delete("temp.txt");
                    this.Close();
                }
            }
        }
        private void 开始执行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (coding == true)//判断是否是用户输入模式
            {
                try
                {
                    Code();//对用户输入代码进行编译
                }
                catch
                {
                    MessageBox.Show("代码输入错误", "提示");
                    新建ToolStripMenuItem_Click(sender, e);
                }
                finally
                {
                    richTextBox_ASM.Clear();//清屏，以方便再次输入
                    richTextBox_ASM.Enabled = false;//关闭richTextBox，显示listBox
                    richTextBox_ASM.Visible = false;
                }
            }
            int i, j;
            for (i = 0; i < 16; i++)//给PC赋初值1000H
            {
                PC[i] = 0;
            }
            PC[12] = 1;
            for (i = 0; i < 65536; i++)
            {
                for (j = 0; j < 4; j++)
                {
                    M[i, j] = 1;
                }
                for (j = 4; j < 8; j++)
                {
                    M[i, j] = 0;
                }
            }
            for (i = 0; i <= 8; i++)
            {
                MtoR(i);
            }
            InstoM();//指令加载进内存
            FT = true;//进入取值周期
            timer1.Enabled = true;
        }
        private void 单微指令执行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunBYMicOrder();
            timer1.Enabled = true;
        }
        private void 单指令执行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i = 0;
            for (i = 0; i < 4; i++)
            {
                RunBYMicOrder();
            }
            CloseLabelVisable();
            timer1.Enabled = true;
        }
        private void 复位ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RESET();
            InstoM();//指令加载进内存
            FT = true;//进入取值周期
            sw.Write("\n[RESET!!!]\n\r\n");//在程序日志中记录
            timer1.Enabled = true;
        }
        private void ToolStripButton_Open_Click(object sender, EventArgs e)
        {
            打开文件ToolStripMenuItem_Click(sender, e);
        }
        private void ToolStripButton_Save_Click(object sender, EventArgs e)
        {
            保存文件ToolStripMenuItem_Click(sender, e);
        }
        private void ToolStripButton_Run_Click(object sender, EventArgs e)
        {
            开始执行ToolStripMenuItem_Click(sender, e);
        }
        private void ToolStripButton_RunByOrder_Click(object sender, EventArgs e)
        {
            单指令执行ToolStripMenuItem_Click(sender, e);
        }
        private void ToolStripButton_RunBYMicOrder_Click(object sender, EventArgs e)
        {
            单微指令执行ToolStripMenuItem_Click(sender, e);
        }
        private void ToolStripButton_Stop_Click(object sender, EventArgs e)
        {
            复位ToolStripMenuItem_Click(sender, e);
        }
        private void ToolStripMenuItem_OneStep_Click(object sender, EventArgs e)
        {
            while ((line - lineStart) < 2 * index - 1)
            {
                单指令执行ToolStripMenuItem_Click(sender, e);
            }
        }
        private void ToolStripButton_OneStep_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_OneStep_Click(sender, e);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Saved == false)
            {
                DialogResult dr = MessageBox.Show("是否保存指令运行过程的日志文件", "提示", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Yes)
                {
                    保存文件ToolStripMenuItem_Click(sender, e);
                    File.Delete("temp.txt");
                }
                else if (dr == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (dr == DialogResult.No)
                {
                    sw.Close();
                    fs.Close();
                    File.Delete("temp.txt");
                }
            }
        }
        private void 使用帮助ToolStripMenuItem_Click(object sender, EventArgs e)//使用帮助
        {
            Form2 fm = new Form2();
            fm.StartPosition = FormStartPosition.CenterScreen;
            fm.Show();
        }
        //用户即时编程
        bool coding = false;
        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RESET();
            NewFile();
            sw.Write("\n[CODING!!!]\n\r\n");//在程序日志中记录
            timer1.Enabled = true;
        }
        private void ToolStripButton_New_Click(object sender, EventArgs e)
        {
            新建ToolStripMenuItem_Click(sender, e);
        }
        private void Code()//编写和编译代码
        {
            index = 0;
            string SFormat = null;
            string[] sformat = new string[4];
            string machla = null;
            int i;
            for (i = 0; i < 100; i++)
            {
                ins[i] = null;
            }
            FileStream fs1 = File.Create("code.txt");
            StreamWriter sw1 = new StreamWriter(fs1);
            sw1.Write(richTextBox_ASM.Text);
            sw1.Close();
            StreamReader sr1 = new StreamReader("code.txt");
            while (sr1.Peek() > -1)//在读到文件尾前
            {
                asm = sr1.ReadLine();//每次读入一行
                asm = asm.ToUpper();//转换为大写
                listBox_ASM.Items.Add(asm + "\n");//添加到“汇编指令”代码框中
                ASMtoMach(asm, ins);//转换为机器指令
                SFormat = ins[index++];
                machla = null;
                for (i = 0; i < 4; i++)//控制机器指令输出格式
                {
                    sformat[i] = SFormat.Substring(i * 4, 4);//将机器指令4位一组
                    machla += sformat[i];
                    machla += "  ";//插入空格
                }
                listBox_Machla.Items.Add(machla);//添加到“机器指令”代码框中
            }
            sr1.Close();//关闭读写器  
        }
        //界面刷新（微操作与机器周期的刷新、寄存器值监视值刷新、指令运行与代码高亮的同步）
        int lineStart = 0;//指令起始行
        int line = 0;//指令当前行
        private void Form1_Load(object sender, EventArgs e)
        {
            listView_Value.View = View.Details;//显示寄存器状态值
            listView_Value.Items[0].SubItems[1].Text = BtoH(R0) + "H";
            listView_Value.Items[1].SubItems[1].Text = BtoH(R1) + "H";
            listView_Value.Items[2].SubItems[1].Text = BtoH(R2) + "H";
            listView_Value.Items[3].SubItems[1].Text = BtoH(R3) + "H";
            listView_Value.Items[4].SubItems[1].Text = BtoH(R4) + "H";
            listView_Value.Items[5].SubItems[1].Text = BtoH(R5) + "H";
            listView_Value.Items[6].SubItems[1].Text = BtoH(R6) + "H";
            listView_Value.Items[7].SubItems[1].Text = BtoH(R7) + "H";
            listView_Value.Items[8].SubItems[1].Text = BtoH(TEMP) + "H";
            listView_Value.Items[0].SubItems[3].Text = BtoH(PC) + "H";
            listView_Value.Items[1].SubItems[3].Text = BtoH(IR) + "H";
            listView_Value.Items[2].SubItems[3].Text = BtoH(MAR) + "H";
            listView_Value.Items[3].SubItems[3].Text = BtoH(MDR) + "H";
            listView_Value.Items[4].SubItems[3].Text = BtoH(BUS) + "H";
            listView_Value.Items[5].SubItems[3].Text = BtoH(SR) + "H";
            listView_Value.Items[6].SubItems[3].Text = BtoH(DR) + "H";
            listView_Value.Items[7].SubItems[3].Text = BtoH(LA) + "H";
            listView_Value.Items[8].SubItems[3].Text = BtoH(LT) + "H";
            listView_Value.Items[0].SubItems[5].Text = BtoH(PSW) + "H";
            listView_Value.Items[1].SubItems[5].Text = H.ToString();
            listView_Value.Items[2].SubItems[5].Text = S.ToString();
            listView_Value.Items[3].SubItems[5].Text = V.ToString();
            listView_Value.Items[4].SubItems[5].Text = N.ToString();
            listView_Value.Items[5].SubItems[5].Text = Z.ToString();
            listView_Value.Items[6].SubItems[5].Text = C.ToString();
            if (toolStripButton_Run.Enabled == true)//控制工具栏按钮的使能，当按下“启动”后：
            {
                toolStripMenuItem_OneStep.Enabled = true;//可以一键执行
                toolStripButton_OneStep.Enabled = true;
                单微指令执行ToolStripMenuItem.Enabled = true;
                toolStripButton_RunBYMicOrder.Enabled = true;//可以单微指令执行
                单指令执行ToolStripMenuItem.Enabled = true;
                toolStripButton_RunByOrder.Enabled = true; //可以单指令执行
                复位ToolStripMenuItem.Enabled = true;
                toolStripButton_Stop.Enabled = true;//可以停止运行
            }

            timer1.Enabled = false;//界面刷新一次完成
        }
        private void CloseLabelVisable()//控制微操作标签的显示
        {
            label_LTtoBUS.Visible = false;
            label_ALUtoLT.Visible = false;
            label_BUStoLA.Visible = false;
            label_1toC0.Visible = false;
            label_CLEARLA.Visible = false;
            label_ADD.Visible = false;
            label_SUB.Visible = false;
            label_AND.Visible = false;
            label_INC.Visible = false;
            label_DEC.Visible = false;
            label_NEC.Visible = false;
            label_BUStoMDR.Visible = false;
            label_BUStoMAR.Visible = false;
            label_MDRtoBUS.Visible = false;
            label_READ.Visible = false;
            label_WRITE.Visible = false;
            label_WAITE.Visible = false;
            label_PCtoBUS.Visible = false;
            label_BUStoPC.Visible = false;
            label_BUStoIR.Visible = false;
            label_R0toBUS.Visible = false;
            label_R1toBUS.Visible = false;
            label_R2toBUS.Visible = false;
            label_R3toBUS.Visible = false;
            label_R4toBUS.Visible = false;
            label_R5toBUS.Visible = false;
            label_R6toBUS.Visible = false;
            label_R7toBUS.Visible = false;
            label_SRtoBUS.Visible = false;
            label_DRtoBUS.Visible = false;
            label_TEMPtoBUS.Visible = false;
            label_BUStoR0.Visible = false;
            label_BUStoR1.Visible = false;
            label_BUStoR2.Visible = false;
            label_BUStoR3.Visible = false;
            label_BUStoR4.Visible = false;
            label_BUStoR5.Visible = false;
            label_BUStoR6.Visible = false;
            label_BUStoR7.Visible = false;
            label_BUStoSR.Visible = false;
            label_BUStoDR.Visible = false;
            label_BUStoTEMP.Visible = false;
            label_ADDC.Visible = false;
            label_SUBC.Visible = false;
            label_ANDC.Visible = false;
            label_INCC.Visible = false;
            label_DECC.Visible = false;
            label_NECC.Visible = false;
            label_JMPC.Visible = false;
            label_JCC.Visible = false;
            label_MOVC.Visible = false;
            label_LDC.Visible = false;
            label_LDIC.Visible = false;
            label_NOPC.Visible = false;
            label_vans.Visible = false;
            label_OnetoFT.Visible = false;
            label_OnetoST.Visible = false;
            label_OnetoDT.Visible = false;
            label_OnetoET.Visible = false;
        }
        private void CloselabelTime()//机器周期标签单独控制
        {
            label_FT.Visible = false;
            label_ET.Visible = false;
            label_ST.Visible = false;
            label_DT.Visible = false;
        }
        private void OpenFile()
        {
            listBox_ASM.Items.Clear();
            listBox_Machla.Items.Clear();
            coding = false;
            richTextBox_ASM.Enabled = false;//关闭richTextBox，显示listBox
            richTextBox_ASM.Visible = false;
        }
        private void NewFile()
        {

            richTextBox_ASM.Enabled = true;
            richTextBox_ASM.Visible = true;
            listBox_ASM.Items.Clear();
            listBox_Machla.Items.Clear();
            coding = true;
            richTextBox_ASM.AppendText("请在此代码框中输入汇编指令：（输入时将此句删掉）");
            toolStripButton_Run.Enabled = true;//新建文件后，“启动”按钮使能有效
        }
        private void RESET()
        {
            CloseLabelVisable();//清除界面显示
            CloselabelTime();
            int i, j;
            for (i = 0; i < 16; i++)//给PC赋初值1000H
            {
                PC[i] = 0;
            }
            PC[12] = 1;
            for (i = 0; i < 65536; i++)//内存空间复位
            {
                for (j = 0; j < 4; j++)
                {
                    M[i, j] = 1;
                }
                for (j = 4; j < 8; j++)
                {
                    M[i, j] = 0;
                }
            }
            for (i = 0; i <= 8; i++)//同步寄存器与对应内存空间的值
            {
                MtoR(i);
            }
            for (i = 0; i < 16; i++)//寄存器值复位
            {
                TEMP[i] = 0;
                IR[i] = 0;
                MAR[i] = 0;
                MDR[i] = 0;
                BUS[i] = 0;
                LA[i] = 0;
                LT[i] = 0;
                C0[i] = 0;
            }
            for (i = 0; i < 8; i++)
            {
                SR[i] = 0;
                DR[i] = 0;
            }
            line = lineStart = 0;//全局变量值复位
            p = 0;
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            Form1_Load(sender, e);
        }

        //程序日志模块
        //将程序运行过程中各寄存器值记录在temp文件中，由用户选择是否保存程序日志
        static FileStream fs = File.Create("temp.txt");
        StreamWriter sw = new StreamWriter(fs);
        private void WriteLog()
        {
            if (label_FT.Visible == true&&index>0)//输出每条汇编指令和机器指令
            {
                sw.Write("汇编指令：" + listBox_ASM.SelectedItem.ToString());
                sw.Write("机器指令：" + listBox_Machla.SelectedItem.ToString()+"\r\n");
            }
            if (label_FT.Visible == true)//输出对应周期
            {
                sw.Write("取指周期[FT]\r\n");
            }
            else if (label_ST.Visible == true)
            {
                sw.Write("取源操作数周期[ST]\r\n");
            }
            else if (label_DT.Visible == true)
            {
                sw.Write("取目的操作数周期[DT]\r\n");
            }
            else if (label_ET.Visible == true)
            {
                sw.Write("执行周期[ET]\r\n");
            }
            sw.Write("{0}\t{1}H\t", "PC:  ", BtoH(PC));//输出寄存器状态值
            sw.Write("{0}\t{1}H\t", "IR:  ", BtoH(IR));
            sw.Write("{0}\t{1}H\t", "R0:  ", BtoH(R0));
            sw.Write("{0}\t{1}H\t", "R1:  ", BtoH(R1));
            sw.Write("{0}\t{1}H\t", "R2:  ", BtoH(R2));
            sw.Write("{0}\t{1}H\t", "R3:  ", BtoH(R3));
            sw.Write("{0}\t{1}H\t", "R4:  ", BtoH(R4));
            sw.Write("{0}\t{1}H\t", "R5:  ", BtoH(R5));
            sw.Write("{0}\t{1}H\t", "R6:  ", BtoH(R6));
            sw.Write("{0}\t{1}H\r\n", "R7:  ", BtoH(R7));
            sw.Write("{0}\t{1}H\t", "PSW: ", BtoH(PSW));
            sw.Write("{0}\t{1}H\t", "MAR: ", BtoH(MAR));
            sw.Write("{0}\t{1}H\t", "MDR: ", BtoH(MDR));
            sw.Write("{0}\t{1}H\t", "BUS: ", BtoH(BUS));
            sw.Write("{0}\t{1}H\t", "SR:  ", BtoH(SR));
            sw.Write("{0}\t{1}H\t", "DR:  ", BtoH(DR));
            sw.Write("{0}\t{1}H\t", "LA:  ", BtoH(LA));
            sw.Write("{0}\t{1}H\t", "LT:  ", BtoH(LT));
            sw.Write("{0}\t{1}H\r\n", "TEMP:", BtoH(TEMP));
        }       
    } 
}
