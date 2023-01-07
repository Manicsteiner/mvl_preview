using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace mvlView
{
    struct MvlPiece
    {
        public string name;
        public uint block_len;
        public uint first_block;
        public uint index;
        public uint length;
        public List<MvlBlock> blocks;
    }
    struct MvlBlock
    {
        public int x, y, z;
        public float u, v;
    }
    public struct MvlSpirit
    {
        public string name;
        public int max_x, max_y, min_x, min_y;
    }
    public class Mvl
    {
        string picname;
        string mvlname;
        uint num;
        public string targetTempPath;
        public List<MvlSpirit> listofmvl = new List<MvlSpirit>();
        public Mvl(string filename)
        {
            mvlname = filename;
            picname = FindFileName(filename);
            /*if (picname.Equals("File not found!")){
                return "Failed";
            }*/
            if (!Directory.Exists(Path.GetTempPath() + "\\mvl_" + GetFileNameOnly(filename)))
                Directory.CreateDirectory(Path.GetTempPath() + "\\mvl_" + GetFileNameOnly(filename));
            targetTempPath = Path.GetTempPath() + "\\mvl_" + GetFileNameOnly(filename) + "\\";

            //process the mvl and png
            MvlProcess();

            //End
            //return targetTempPath + "\\index.json";
        }
        public void MvlProcess()
        {
            Stream mvldata = new FileStream(mvlname, FileMode.Open, FileAccess.Read);
            byte[] mvlread = new byte[(int)mvldata.Length];
            mvldata.Read(mvlread, 0, mvlread.Length);
            mvldata.Close();

            //confirm
            if (mvlread[0] == 0x78 && mvlread[1] == 0x9c)
            {
                throw new CustMessage("This mvl file seems to have been compressed. Please handle it with python script.");
                try
                {
                    mvlread = Decompress(mvlread);
                }
                catch { }
            }
            byte[] header = new byte[4];
            Array.Copy(mvlread, 0, header, 0, 4);
            byte[] defaultheader = { 0x4d, 0x56, 0x4c, 0x31 };//"MVL1"
            if (!header.SequenceEqual(defaultheader)){
                /*debug*/
                //MessageBox.Show(header.Length.ToString());
                throw new CustMessage("Not a MVL file with MVL1 header, it is "+ Encoding.ASCII.GetString(header));
            }
            byte[] signofmvl = new byte[10];
            Array.Copy(mvlread, 0x20, signofmvl, 0, 10);
            byte[] defaultsign = System.Text.Encoding.ASCII.GetBytes("XFYF0FUFVF");
            //byte[] defaultsign = "XFYF0FUFVF".ToCharArray();
            if (!signofmvl.SequenceEqual(defaultsign))
            {
                //MessageBox.Show(Encoding.ASCII.GetString(signofmvl));
                throw new CustMessage("Not a MVL file with sign of XFYF0FUFVF, it is "+ Encoding.ASCII.GetString(signofmvl));
            }

            //process
            num = BitConverter.ToUInt32(mvlread, 4);
            //MVLGetPic();
            List<MvlPiece> pics = new List<MvlPiece>();
            byte[] tempsigntrue = { 0x04, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 };
            for (int i = 0; i < num; i++)
            {
                byte[] temp = new byte[0x40];
                Array.Copy(mvlread, i * 0x40 + 0x60, temp, 0, 0x40);
                byte[] tempsign = new byte[8];
                Array.Copy(temp, 8, tempsign, 0, 8);
                if (!tempsign.SequenceEqual(tempsigntrue))
                {
                    throw new CustMessage("MVL piece sign error");
                }
                MvlPiece temp_piece;
                temp_piece.block_len = BitConverter.ToUInt32(temp, 0x10);
                temp_piece.first_block = BitConverter.ToUInt32(temp, 0x14);
                temp_piece.length = BitConverter.ToUInt32(temp, 0x18);
                temp_piece.index = BitConverter.ToUInt32(temp, 0x1c);
                byte[] tempname = new byte[0x20];
                Array.Copy(temp, 0x20, tempname, 0, 0x20);
                temp_piece.name = CStr(tempname);
                temp_piece.blocks = new List<MvlBlock>();
                pics.Add(temp_piece);
            }
            //MVLGetBlocks
            for (int i = 0; i < num; i++)
            {
                MvlPiece temp = pics[i];
                uint bi = temp.first_block;
                uint bl = temp.block_len;
                byte[] block_data = new byte[20*bl];
                Array.Copy(mvlread, bi, block_data, 0, 20 * bl);
                byte[] tempdata = new byte[2 * temp.length];
                Array.Copy(mvlread, temp.index, tempdata, 0, 2 * temp.length);
                for(int j = 0; j < (temp.length * 2); j += 2)
                {
                    ushort k = BitConverter.ToUInt16(tempdata, j);
                    if (!(k <= bl))
                    {
                        throw new CustMessage("Out of blocks");
                    }
                    MvlBlock tempblock;
                    tempblock.x = (int)Math.Round(BitConverter.ToSingle(block_data, k * 20));
                    tempblock.y = (int)Math.Round(BitConverter.ToSingle(block_data, k * 20 + 4));
                    tempblock.z = (int)Math.Round(BitConverter.ToSingle(block_data, k * 20 + 8));
                    tempblock.u = BitConverter.ToSingle(block_data, k * 20 + 12);
                    tempblock.v = BitConverter.ToSingle(block_data, k * 20 + 16);
                    if(tempblock.z != 0)
                    {
                        throw new CustMessage("z!=0");
                    }
                    pics[i].blocks.Add(tempblock);
                }
            }
            //MVLCombine
            //open the pic first!
            Image pic = Bitmap.FromFile(picname);
            int w = pic.Width;
            int h = pic.Height;
            List<MvlBlock> mainblock = pics[0].blocks;
            int dx = Math.Abs(mainblock[0].x - mainblock[1].x);
            int dy = Math.Abs(mainblock[0].y - mainblock[2].y);
            int dw = (int)Math.Round(Math.Abs(mainblock[0].u - mainblock[1].u) * w);
            int dh = (int)Math.Round(Math.Abs(mainblock[0].v - mainblock[2].v) * h);
            double rx = dx / dw;
            double ry = dy / dh;
            //pic_i = 0
            for (int i = 0; i < num; i++)
            {
                if (pics[i].length <= 0) continue;
                //Bitmap img = PureBackground();
                Bitmap img = new Bitmap(4000, 6000);
                Graphics img_g = Graphics.FromImage(img);
                MvlBlock tmppoint = pics[i].blocks[0];
                int x = (int)Math.Round(tmppoint.x / rx) + 2000;
                int y = (int)Math.Round(tmppoint.y / ry) + 1000;
                int minx = x, miny = y, maxx = x, maxy = y;
                //for(int j = 0; j< )
                int j = 0;
                foreach(MvlBlock tpoint in pics[i].blocks)
                {
                    if(j++ % 6 != 0)
                    {
                        continue;
                    }
                    x = (int)Math.Round(tpoint.x / rx) + 2000;
                    y = (int)Math.Round(tpoint.y / ry) + 1000;
                    Rectangle crop = new Rectangle((int)Math.Round(tpoint.u*w), (int)Math.Round(tpoint.v * h),dw, dh);
                    Bitmap target = new Bitmap(dw, dh);
                    Graphics gr = Graphics.FromImage(target);
                    gr.DrawImage(pic, 0, 0, crop, GraphicsUnit.Pixel);
                    img_g.DrawImage(target, x, y);//mask?
                    minx = Math.Min(x, minx);
                    maxx = Math.Max(x, maxx);
                    miny = Math.Min(y, miny);
                    maxy = Math.Max(y, maxy);
                    target.Dispose();
                    gr.Dispose();
                }
                maxx += dw;
                maxy += dh;
                //MessageBox.Show("CanIDraw?");
                Rectangle fincrop = new Rectangle(minx, miny, maxx - minx, maxy - miny);
                Bitmap imgf = new Bitmap(fincrop.Width,fincrop.Height);
                Graphics imgf_g = Graphics.FromImage(imgf);
                imgf_g.DrawImage(img, 0, 0, fincrop, GraphicsUnit.Pixel);
                imgf.Save(targetTempPath + pics[i].name + ".png", System.Drawing.Imaging.ImageFormat.Png);
                img.Dispose();
                img_g.Dispose();
                imgf.Dispose();
                imgf_g.Dispose();
                MvlSpirit tmpsp;
                tmpsp.max_x = (int)Math.Round((maxx - 1000) * rx);
                tmpsp.max_y = (int)Math.Round((maxy - 1000) * ry);
                tmpsp.min_x = (int)Math.Round((minx - 1000) * rx);
                tmpsp.min_y = (int)Math.Round((miny - 1000) * ry);
                tmpsp.name = pics[i].name;
                listofmvl.Add(tmpsp);
            }
            //MessageBox.Show(listofmvl[0].min_x.ToString());
            //MessageBox.Show(listofmvl[1].min_x.ToString());
            //return the json here
        }

        //static methods
        public static string FindFileName(string filename) {
            string m_FileName = filename.Substring(0, filename.LastIndexOf('.') - 1);
            string namewe = m_FileName + ".webp";
            string namepn = m_FileName + ".png";
            if (File.Exists(namepn)){
                 return namepn;
            }
            else {
                if (File.Exists(namewe)){
                        return namewe;
                }
                else
                {
                    throw new CustMessage("Picture file not found!");
                }
            }
        }
        public static string GetFileNameOnly(string filename)
        {
            string m_FileName = filename.Substring(filename.LastIndexOf('\\') + 1, filename.LastIndexOf('.') - filename.LastIndexOf('\\') - 2);
            return m_FileName;
        }
        public static string CStr(byte[] input)
        {
            for(int i = 0; i < input.Length; i++)
            {
                if (input[i] == 0xfe) input[i] = 0x00;
            }
            string ret = Encoding.ASCII.GetString(input);
            return ret.Substring(0, ret.IndexOf('\x00'));
            //return BitConverter.ToString(input);
        }
        public static byte[] Decompress(byte[] inputBytes)
        /*Not sure it would work or not*/
        {
            using (MemoryStream inputStream = new MemoryStream(inputBytes))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (System.IO.Compression.GZipStream zipStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress))
                    {
                        zipStream.CopyTo(outStream);
                        zipStream.Close();
                        return outStream.ToArray();
                    }
                }

            }
        }
        public static Bitmap PureBackground()
        {
            Bitmap img = new Bitmap(6000, 10000);
            Color color = Color.FromArgb(0x00000000);
            for(int i = 0; i<img.Width; i++)
            {
                for(int j = 0; j < img.Height; j++)
                {
                    img.SetPixel(i,j,color);
                }
            }
            return img;
        }
        public static Bitmap PureBackground(int w, int h)
        {
            Bitmap img = new Bitmap(w, h);
            Color color = Color.FromArgb(0x00000000);
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    img.SetPixel(i, j, color);
                }
            }
            return img;
        }
    }
    public class CustMessage : Exception
    {
        public CustMessage(string str)
            : base(str)
        {
        }
    }
}
