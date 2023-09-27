using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mvlView
{
    class Format
    {
        //webp
        public static bool IsWebp(byte[] picread)
        {
            byte[] header = new byte[4];
            Array.Copy(picread, 8, header, 0, 4);
            byte[] defaultheader = { 0x57, 0x45, 0x42, 0x50 };//"WEBP"
            if (!header.SequenceEqual(defaultheader)) return false;
            return true;
        }
        public static bool IsWebp(string filepath)
        {
            Stream picdata = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            byte[] picread = new byte[(int)picdata.Length];
            picdata.Read(picread, 0, picread.Length);
            picdata.Close();
            return IsWebp(picread);
        }

        //mvl
        public static bool IsMvl(byte[] mvlread)
        {
            byte[] header = new byte[4];
            Array.Copy(mvlread, 0, header, 0, 4);
            byte[] defaultheader = { 0x4d, 0x56, 0x4c, 0x31 };//"MVL1"
            if (!header.SequenceEqual(defaultheader))
            {
                //throw new CustMessage("Not a MVL file with MVL1 header, it is " + Encoding.ASCII.GetString(header));
                return false;
            }
            byte[] signofmvl = new byte[10];
            Array.Copy(mvlread, 0x20, signofmvl, 0, 10);
            byte[] defaultsign = Encoding.ASCII.GetBytes("XFYF0FUFVF");
            //byte[] defaultsign = "XFYF0FUFVF".ToCharArray();
            if (!signofmvl.SequenceEqual(defaultsign))
            {
                //throw new CustMessage("Not a MVL file with sign of XFYF0FUFVF, it is " + Encoding.ASCII.GetString(signofmvl));
                return false;
            }
            return true;
        }
        public static bool IsMvl(string filepath)
        {
            Stream mvldata = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            byte[] mvlread = new byte[(int)mvldata.Length];
            mvldata.Read(mvlread, 0, mvlread.Length);
            mvldata.Close();
            return IsMvl(mvlread);
        }

        //json for mvl
        public static List<MvlSpirit> JsonToMvl(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            JObject json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
            sr.Close();

            List<MvlSpirit> mvljson = new List<MvlSpirit>();

            int num = 0;
            List<string> names = new List<string>();
            foreach (var item in json)
            {
                num++;
                names.Add(item.Key);
            }
            for (int i = 0; i < num; i++)
            {
                MvlSpirit temp;
                temp.name = names[i];
                temp.max_x = (int)json[names[i]]["max_x"];
                temp.min_x = (int)json[names[i]]["min_x"];
                temp.max_y = (int)json[names[i]]["max_y"];
                temp.min_y = (int)json[names[i]]["min_y"];
                mvljson.Add(temp);
            }
            return mvljson;
        }

        /*--Json handeler for Kaleido ADV Workshop
         * */
        /*public static int JsonKaleido(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            JObject json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
            sr.Close();


            return 0;
        }*/

        //identifying json
        public static bool IsKaleidoJson(string jsonpath)
        {
            if (jsonpath.EndsWith(".psb.m.json")) return true;
            else return false;
        }

        //pure number filename handler
        public static string Pure_GetOtherOne(string filename)
        {
            string inputfilename = GetFileNameOnly(filename);
            int length = inputfilename.Length;
            int inputnum = int.Parse(inputfilename);
            if(inputnum % 2 == 0) //input pic, always even
            {
                //return inputnum+1
                return IntToStrFill(inputnum + 1, length);
            }
            else //input mvl or lay, always odd
            {
                //return inputnum-1
                return IntToStrFill(inputnum - 1, length);
            }
        }

        static string IntToStrFill(uint inint, int leng)
        {
            string retstr = inint.ToString();
            while (retstr.Length < leng) retstr = "0" + retstr;
            return retstr;
        }
        static string IntToStrFill(int inint, int leng) {
            //if (inint < 0) return "";
            uint i;
            try
            {
                i = Convert.ToUInt32(inint);
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return IntToStrFill(i, leng);
        }
        static string IntToStrFill(uint inint, long leng)
        {
            int lengi;
            try
            {
                lengi = Convert.ToInt32(leng);
            }
            catch (OverflowException ex)
            {
                throw ex;
            }
            return IntToStrFill(inint, lengi);
        }
        static string IntToStrFill(int inint, long leng)
        {
            int lengi;
            try
            {
                lengi = Convert.ToInt32(leng);
            }
            catch (OverflowException ex)
            {
                throw ex;
            }
            uint i;
            try
            {
                i = Convert.ToUInt32(inint);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return IntToStrFill(i, lengi);
        }

        //common
        public static string GetFileName(string filename)//with extension
        {
            string m_FileName = filename.Substring(filename.LastIndexOf('\\') + 1);
            return m_FileName;
        }
        public static string GetFileNameOnly(string filename)//without extension
        {
            if (!HasExtensionName(filename)) return GetFileName(filename);
            string m_FileName = filename.Substring(filename.LastIndexOf('\\') + 1, filename.LastIndexOf('.') - filename.LastIndexOf('\\') - 1);
            return m_FileName;
        }
        public static bool HasExtensionName(string filename)
        {
            string m_filename = GetFileName(filename);
            if (m_filename.LastIndexOf('.') == -1) return false;
            else return true;
        }
    }
}
