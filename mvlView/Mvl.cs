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
using System.Runtime.InteropServices;
using Imazen.WebP;
using System.Threading;

namespace mvlView
{
    /// <summary>处理mvl过程中的原图上的小片图片</summary>
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
    ///<summary>最终输出至临时目录的图片</summary>
    public struct MvlSpirit
    {
        public string name;
        public int max_x, max_y, min_x, min_y;
    }
    ///<summary>Methods from mvl.py, https://github.com/ningshanwutuobang/ChaosChildPCTools</summary>
    /*"""
    mvl format :
    ------------
    head
    ------------0x60
    pictures
    ------------
    blocks
    x, y, z, u, v
    ------------
    block_indexs of picturess
    
    """*/
    public class Mvl
    {
        Image pic;
        string mvlname;
        uint num;
        public string targetTempPath;
        public List<MvlSpirit> listofmvl = new List<MvlSpirit>();
        public Mvl(string filename)
        {
            mvlname = filename;
            pic = GetPicture(filename);
            /*if (picname.Equals("File not found!")){
                return "Failed";
            }*/
            if (!Directory.Exists(Path.GetTempPath() + "\\mvl_" + Mvl.GetFileNameOnly(filename)))
                Directory.CreateDirectory(Path.GetTempPath() + "\\mvl_" + Mvl.GetFileNameOnly(filename));
            targetTempPath = Path.GetTempPath() + "\\mvl_" + Mvl.GetFileNameOnly(filename) + "\\";

            //process the mvl and png
            MvlProcess();

            //End
            //return targetTempPath + "\\index.json";
        }
        protected void MvlProcess()
        {
            Stream mvldata = new FileStream(mvlname, FileMode.Open, FileAccess.Read);
            byte[] mvlread = new byte[(int)mvldata.Length];//long Length to int, requires it no larger than about 2GB
            mvldata.Read(mvlread, 0, mvlread.Length);
            mvldata.Close();

            //confirm
            if (mvlread[0] == 0x78 && mvlread[1] == 0x9c)
            {
                throw new CustMessage("This mvl file seems to have been compressed. Please handle it with python script.");
                /*try
                {
                    mvlread = Decompress(mvlread);
                }
                catch { }*/
            }
            if (!Format.IsMvl(mvlread)) throw new CustMessage("Not a MVL file");

            /*byte[] header = new byte[4];
            Array.Copy(mvlread, 0, header, 0, 4);
            byte[] defaultheader = { 0x4d, 0x56, 0x4c, 0x31 };//"MVL1"
            if (!header.SequenceEqual(defaultheader)){
                debug
                //MessageBox.Show(header.Length.ToString());
                throw new CustMessage("Not a MVL file with MVL1 header, it is "+ Encoding.ASCII.GetString(header));
            }
            byte[] signofmvl = new byte[10];
            Array.Copy(mvlread, 0x20, signofmvl, 0, 10);
            byte[] defaultsign = Encoding.ASCII.GetBytes("XFYF0FUFVF");
            //byte[] defaultsign = "XFYF0FUFVF".ToCharArray();
            if (!signofmvl.SequenceEqual(defaultsign))
            {
                //MessageBox.Show(Encoding.ASCII.GetString(signofmvl));
                throw new CustMessage("Not a MVL file with sign of XFYF0FUFVF, it is "+ Encoding.ASCII.GetString(signofmvl));
            }*/

            //process
            num = BitConverter.ToUInt32(mvlread, 4);
            //MVLGetPic();
            List<MvlPiece> pics = new List<MvlPiece>((int)num);
            byte[] tempsigntrue = { 0x04, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 };
            for (int i = 0; i < num; i++)
            //Parallel.For(0, num, i =>
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
                /*pics.Add(temp_piece);
            }
            //MVLGetBlocks
            for (int i = 0; i < num; i++)
            {
                MvlPiece temp = pics[i];*/
                uint bi = temp_piece.first_block;
                uint bl = temp_piece.block_len;
                byte[] block_data = new byte[20 * bl];
                Array.Copy(mvlread, bi, block_data, 0, 20 * bl);
                byte[] tempdata = new byte[2 * temp_piece.length];
                Array.Copy(mvlread, temp_piece.index, tempdata, 0, 2 * temp_piece.length);
                for (int j = 0; j < (temp_piece.length * 2); j += 2)
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
                    if (tempblock.z != 0)
                    {
                        throw new CustMessage("z!=0");
                    }
                    //pics[i].blocks.Add(tempblock);
                    temp_piece.blocks.Add(tempblock);
                }
                /*lock(pics) */pics.Add(temp_piece);
            }
            //MVLCombine
            //open the pic first!
            //Image pic = Bitmap.FromFile(picname);
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
            //Parallel.For(0, num, index =>
            {
                if (pics[i].length <= 0) continue;
                //int i = (int)index;
                //if (pics[i].length <= 0) return;
                //Bitmap img = PureBackground();
                Bitmap img = new Bitmap(4000, 6000);
                Graphics img_g = Graphics.FromImage(img);
                MvlBlock tmppoint = pics[i].blocks[0];
                int x = (int)Math.Round(tmppoint.x / rx) + 2000;
                int y = (int)Math.Round(tmppoint.y / ry) + 1000;
                int minx = x, miny = y, maxx = x, maxy = y;
                //for(int j = 0; j< )
                int j = 0;
                foreach (MvlBlock tpoint in pics[i].blocks)
                {
                    if (j++ % 6 != 0) continue;
                    x = (int)Math.Round(tpoint.x / rx) + 2000;
                    y = (int)Math.Round(tpoint.y / ry) + 1000;
                    Rectangle crop = new Rectangle((int)Math.Round(tpoint.u * w), (int)Math.Round(tpoint.v * h), dw, dh);
                    Bitmap target = new Bitmap(dw, dh);
                    Graphics gr = Graphics.FromImage(target);
                    /*lock (pic) */gr.DrawImage(pic, 0, 0, crop, GraphicsUnit.Pixel);
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
                Bitmap imgf = new Bitmap(fincrop.Width, fincrop.Height);
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
        public void Close()
        {
            pic.Dispose();
            listofmvl.Clear();
            GC.SuppressFinalize(this);
        }

        //static methods
        protected static Image GetPicture(string filename) {
            string m_FileName;
            if (Format.HasExtensionName(filename)) m_FileName = filename.Substring(0, filename.LastIndexOf('.') - 1);
            else m_FileName = filename;
            string namewe = m_FileName + ".webp";
            string namepn = m_FileName + ".png";
            if (File.Exists(namepn)){
                Image pic = Bitmap.FromFile(namepn);
                return pic;
            }
            else {
                if (File.Exists(namewe))
                {
                    //Image pic = Bitmap.FromFile(namewe);//It doesn't work

                    /*WebPFormat.WebP pic = new WebPFormat.WebP();
                    Bitmap picdec = pic.Load(namewe);
                    return picdec;*/ //method from webpformat, returns sh!t quality

                    Stream inpic = new FileStream(namewe, FileMode.Open, FileAccess.Read);
                    byte[] picdata = new byte[(int)inpic.Length];
                    inpic.Read(picdata, 0, (int)inpic.Length);
                    if (!Format.IsWebp(picdata)) throw new CustMessage("Picture file Error!");
                    SimpleDecoder dec = new SimpleDecoder();
                    Bitmap picdec = dec.DecodeFromBytes(picdata, inpic.Length);
                    inpic.Close();
                    return picdec;
                }
                else
                {
                    if (Format.IsMvl(filename) & !Format.HasExtensionName(filename))
                    {
                        m_FileName = filename.Substring(0, filename.LastIndexOf('\\')) + "\\" + Format.Pure_GetOtherOne(filename);
                    }
                    else
                    {
                        throw new CustMessage("Not a MVL file!");
                    }
                    //MessageBox.Show(m_FileName + ".wav");
                    if (File.Exists(m_FileName + ".wav") | File.Exists(m_FileName + ".webp"))
                    {
                        Stream inpic;
                        if (File.Exists(m_FileName + ".webp"))
                        {
                            if(!Format.IsWebp(m_FileName + ".webp")) throw new CustMessage("Picture file Error!");
                            inpic = new FileStream(m_FileName + ".webp", FileMode.Open, FileAccess.Read);
                        }
                        else
                        {
                            if (!Format.IsWebp(m_FileName + ".wav")) throw new CustMessage("Picture file Error!");
                            inpic = new FileStream(m_FileName + ".wav", FileMode.Open, FileAccess.Read);
                        }
                        //inpic = new FileStream(m_FileName + ".wav", FileMode.Open, FileAccess.Read);
                        byte[] picdata = new byte[(int)inpic.Length];
                        inpic.Read(picdata, 0, (int)inpic.Length);
                        /*byte[] header = new byte[4];
                        Array.Copy(picdata, 8, header, 0, 4);
                        byte[] defaultheader = { 0x57, 0x45, 0x42, 0x50 };*///"WEBP"
                        //if (!Format.IsWebp(picdata)) throw new CustMessage("Picture file Error!");
                        SimpleDecoder dec = new SimpleDecoder();
                        Bitmap picdec = dec.DecodeFromBytes(picdata, inpic.Length);
                        inpic.Close();
                        return picdec;
                    }
                    else throw new CustMessage("Picture file not found!");
                }
            }
        }
        static string GetFileNameOnly(string filename)
        {
            if (!Format.HasExtensionName(filename)) {
                return Format.GetFileNameOnly(filename);
            }
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
        /*public static Bitmap PureBackground()
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
        }*/
    }
    public class CustMessage : Exception
    {
        public CustMessage(string str)
            : base(str)
        {
        }
    }
}
