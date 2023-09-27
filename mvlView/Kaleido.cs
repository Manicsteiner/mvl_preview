using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Imazen.WebP;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace mvlView
{
    class Locator
    {
        int total_h, total_w;
        int base_h, base_w, base_x, base_y;
        int diff_h, diff_w, diff_x, diff_y;
        int diffbase;
        public Locator(JObject json, string getwho)
        {
            total_h = (int)json["h"];
            total_w = (int)json["w"];
            base_h = (int)json["crop"]["h"];
            base_w = (int)json["crop"]["w"];
            base_x = (int)json["crop"]["x"];
            base_y = (int)json["crop"]["y"];
            diffbase = (int)json[getwho + "diffbase"];
            diff_h = (int)json[getwho + "diff"]["h"] + 2;
            diff_w = (int)json[getwho + "diff"]["w"] + 2;
            diff_x = (int)json[getwho + "diff"]["x"];
            diff_y = (int)json[getwho + "diff"]["y"];
        }

        //target 原图素材位置
        public int getTarget_x(int mapindex)
        {
            if(mapindex == -1)
            {
                return getPasteTarget_x();
            }
            int rollnum = base_h / diff_h;
            return diffbase + mapindex / rollnum * diff_w;
        }
        public int getTarget_y(int mapindex)
        {
            if (mapindex == -1)
            {
                return getPasteTarget_y();
            }
            int rollnum = base_h / diff_h;
            return mapindex % rollnum * diff_h;
        }

        //pastetarget 底图起点
        public int getPasteTarget_x()
        {
            return diff_x - base_x - 1;
        }
        public int getPasteTarget_y()
        {
            return diff_y - base_y - 1;
        }

        //diff_h,w
        public int getDiff_h()
        {
            return diff_h;
        }
        public int getDiff_w()
        {
            return diff_w;
        }
    }
    public class Kaleido
    {
        Image pic;
        string basename;
        uint num;
        public string targetTempPath;
        public List<MvlSpirit> listofmvl = new List<MvlSpirit>();
        public Kaleido(string jsonpath)
        {
            basename = GetFileNameOnly(jsonpath);
            pic = Bitmap.FromFile(jsonpath.Substring(0, jsonpath.LastIndexOf(".")) + "\\" + basename + ".png");

            if (!Directory.Exists(Path.GetTempPath() + "\\mvl_" + basename))
                Directory.CreateDirectory(Path.GetTempPath() + "\\mvl_" + basename);
            targetTempPath = Path.GetTempPath() + "\\mvl_" + basename + "\\";

            /**本段将解读json文件，拆分base.png，底图使用原名，子分支使用底图+分支名  -*/
            StreamReader sr = new StreamReader(jsonpath);
            JObject json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
            sr.Close();

            //拆分出底图
            Rectangle baseimgcrop = new Rectangle(0, 0, (int)json["crop"]["w"], (int)json["crop"]["h"]);
            Bitmap baseimg = new Bitmap(baseimgcrop.Width,baseimgcrop.Height);
            Graphics baseimg_g = Graphics.FromImage(baseimg);
            baseimg_g.DrawImage(pic, 0, 0, baseimgcrop, GraphicsUnit.Pixel);
            baseimg.Save(targetTempPath + basename + ".png", System.Drawing.Imaging.ImageFormat.Png);
            
            MvlSpirit tempbase;
            tempbase.name = basename;
            tempbase.min_x = 0;
            tempbase.min_y = 0;
            tempbase.max_x = baseimgcrop.Width;
            tempbase.max_y = baseimgcrop.Height;
            listofmvl.Add(tempbase);
            
            baseimg_g.Dispose();
            baseimg.Dispose();

            //拆分眼
            if (json.ContainsKey("eyediffbase")) { 
                Locator eyelocate = new Locator(json, "eye");
                int eyemapnum = 0;
                List<string> eyemap = new List<string>();
                JObject eyemapObj = JObject.FromObject(json["eyemap"]);
                foreach (var item in eyemapObj)
                {
                    eyemapnum++;
                    eyemap.Add(item.Key);
                }
                for (int i = 0; i < eyemapnum; i++)
                {
                    MvlSpirit temp;
                    temp.name = basename + "_" + eyemap[i];

                    int eyemapindex;
                    int? eyemapindexsus = (int?)eyemapObj[eyemap[i]];//通过int?转换null为-1
                    eyemapindex = (eyemapindexsus != null)? eyemapindexsus.Value : -1;

                    Rectangle eyecrop = new Rectangle(eyelocate.getTarget_x(eyemapindex), eyelocate.getTarget_y(eyemapindex), eyelocate.getDiff_w(), eyelocate.getDiff_h());
                    Bitmap eyediffimg = new Bitmap(eyelocate.getDiff_w(), eyelocate.getDiff_h());
                    Graphics eyediffimg_g = Graphics.FromImage(eyediffimg);
                    eyediffimg_g.DrawImage(pic, 0, 0, eyecrop, GraphicsUnit.Pixel);
                    eyediffimg.Save(targetTempPath + basename + "_" + eyemap[i] + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    eyediffimg_g.Dispose();
                    eyediffimg.Dispose();

                    temp.min_x = eyelocate.getPasteTarget_x();
                    temp.min_y = eyelocate.getPasteTarget_y();
                    temp.max_x = eyelocate.getPasteTarget_x() + eyelocate.getDiff_w();
                    temp.max_y = eyelocate.getPasteTarget_y() + eyelocate.getDiff_h();
                    listofmvl.Add(temp);
                }
            }

            //拆分嘴
            if(json.ContainsKey("lipdiffbase"))
            {
                Locator liplocate = new Locator(json, "lip");
                int lipmapnum = 0;
                List<string> lipmap = new List<string>();
                JObject lipmapObj = JObject.FromObject(json["lipmap"]);
                foreach (var item in lipmapObj)
                {
                    lipmapnum++;
                    lipmap.Add(item.Key);
                }
                for (int i = 0; i < lipmapnum; i++)
                {
                    MvlSpirit temp;
                    temp.name = basename + "_" + lipmap[i];

                    int lipmapindex;
                    int? lipmapindexsus = (int?)lipmapObj[lipmap[i]];//通过int?转换null为-1
                    lipmapindex = (lipmapindexsus != null) ? lipmapindexsus.Value : -1;

                    Rectangle lipcrop = new Rectangle(liplocate.getTarget_x(lipmapindex), liplocate.getTarget_y(lipmapindex), liplocate.getDiff_w(), liplocate.getDiff_h());
                    Bitmap lipdiffimg = new Bitmap(liplocate.getDiff_w(), liplocate.getDiff_h());
                    Graphics lipdiffimg_g = Graphics.FromImage(lipdiffimg);
                    lipdiffimg_g.DrawImage(pic, 0, 0, lipcrop, GraphicsUnit.Pixel);
                    lipdiffimg.Save(targetTempPath + basename + "_" + lipmap[i] + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    lipdiffimg_g.Dispose();
                    lipdiffimg.Dispose();

                    temp.min_x = liplocate.getPasteTarget_x();
                    temp.min_y = liplocate.getPasteTarget_y();
                    temp.max_x = liplocate.getPasteTarget_x() + liplocate.getDiff_w();
                    temp.max_y = liplocate.getPasteTarget_y() + liplocate.getDiff_h();
                    listofmvl.Add(temp);
                }
            }

            /* DRAW IMAGE FROM CROP
             * Rectangle fincrop = new Rectangle(minx, miny, maxx - minx, maxy - miny);
                Bitmap imgf = new Bitmap(fincrop.Width,fincrop.Height);
                Graphics imgf_g = Graphics.FromImage(imgf);
                imgf_g.DrawImage(img, 0, 0, fincrop, GraphicsUnit.Pixel);
                imgf.Save(targetTempPath + pics[i].name + ".png", System.Drawing.Imaging.ImageFormat.Png);
                img.Dispose();
                img_g.Dispose();
                imgf.Dispose();
                imgf_g.Dispose();*/

            /*拆分与解读完毕*/
        }
        protected static string GetPicture(string filepath)
        {
            return filepath.Substring(0, filepath.LastIndexOf('\\') + 1) + GetFileNameOnly(filepath) + ".psb.m\\" + GetFileNameOnly(filepath) + ".png";
        }
        static string GetFileNameOnly(string filepath)
        {
            string filename = filepath.Substring(filepath.LastIndexOf('\\') + 1);
            while (Format.HasExtensionName(filename))
            {
                filename = filename.Substring(0, filename.LastIndexOf("."));
            }
            return filename;
            //例：08_maoru_adfgghlj09990
        }
    }
}
