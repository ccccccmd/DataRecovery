using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace datare
{
   
    public class DataProcess
    {
        /// <summary>
        /// 数据处理类
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sliceNum"></param>
        public DataProcess(string path, int sliceNum)
        {
            this.DPath = path;
            this.SliceNum = sliceNum;
        }

        private string DPath { get; set; }
        private int SliceNum { get; set; }
 


        /// <summary>
        /// 创建 分片数据
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sliceNum"></param>
        public void Slicing()
        {

            int sliceNum = this.SliceNum;
            string path = this.DPath;
            //清空原有的备份分片文件
            DirectoryInfo d = new DirectoryInfo(path);
            var files = d.GetFiles("slice*.dat");
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i].FullName);
                }
            }

            //获得文件长度
            using (FileStream fs = new FileStream(path + "\\source.dat", FileMode.Open))
            {
                //获取每片数据字节长度
                int sliceLength = (int)Math.Ceiling((double)fs.Length / (sliceNum * sliceNum));
               //存储最后一片分片的真实长度
               int trueLength=(int) fs.Length - (sliceNum*sliceNum - 1)*sliceLength;
               File.WriteAllText("conf.ini", trueLength.ToString ());
                byte[] buffer = new byte[sliceLength];
                
                int readDateLength;
                for (int i = 1; i <= sliceNum; i++)
                {
                    for (int j = 1; j <= sliceNum; j++)
                    {
                        if ((readDateLength = fs.Read(buffer, 0, sliceLength)) > 0)
                        {
                            //把每次读取的数据写入分片文件
                            FileStream fw = new FileStream(path + "\\slice_" + i + "_" + j + ".dat", FileMode.Create);
                            fw.Write(buffer, 0, sliceLength);
                            fw.Close();
                            fw.Dispose();
                        }
                        else
                        {
                            break;
                        }

                    }

                }

            }
            //分片文件创建好后去建立异或文件
            XORingFile();
        }

        /// <summary>
        /// 建立异或文件
        /// </summary>
        private void XORingFile()
        {
            //得到分片文件
            List<SlicingDataInfo> slicingDataList = GetSlicingFiles("slice*.dat");
            //执行建立异或文件方法
            CreateXORFile(slicingDataList);
        }
        /// <summary>
        /// 创建异或文件
        /// </summary>
        /// <param name="slicingDataList"></param>
        private void CreateXORFile(List<SlicingDataInfo> slicingDataList)
        {
            for (int i = 1; i <= this.SliceNum; i++)
            {
                //遍历集合中数据，把同一行，同一列的数据放入同一集合中。
                List<SlicingDataInfo> listR = new List<SlicingDataInfo>();
                List<SlicingDataInfo> listC = new List<SlicingDataInfo>();
                for (int j = 0; j < slicingDataList.Count; j++)
                {
                    if (listC.Count != this.SliceNum)
                    {
                        if (slicingDataList[j].ColNum == i.ToString())
                        {
                            SlicingDataInfo sd = new SlicingDataInfo() { ColNum = i.ToString(), Data = slicingDataList[j].Data, RowNum = (this.SliceNum + 1).ToString() };
                            listC.Add(sd);
                        }
                    }
                    if (listR.Count != this.SliceNum)
                    {
                        if (slicingDataList[j].RowNum == i.ToString())
                        {
                            SlicingDataInfo sd = new SlicingDataInfo() { RowNum = i.ToString(), Data = slicingDataList[j].Data, ColNum = (this.SliceNum + 1).ToString() };
                            listR.Add(sd);
                        }
                    }
                }
                //得到同一行的所有 分片数据，去进行异或运算
                GoCreateXORFile(listR, "slice_" + listR[0].RowNum + "_" + listR[0].ColNum + ".dat");
                //得到同一列的所有分片数据，去进行异或运算
                GoCreateXORFile(listC, "slice_" + listC[0].RowNum + "_" + listC[0].ColNum + ".dat");
            }
        }
        /// <summary>
        /// 异或运算并写入磁盘
        /// </summary>
        /// <param name="list"></param>
        private void GoCreateXORFile(List<SlicingDataInfo> list, string xorFilePath)
        {
            byte[] b = list[0].Data;
            string path = this.DPath + "\\" + xorFilePath;
            for (int i = 1; i < list.Count; i++)
            {
                b = XOR(b, list[i].Data);
            }
            File.WriteAllBytes(path, b);
        }
        /// <summary>
        /// 异或运算算法
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        private byte[] XOR(byte[] b1, byte[] b2)
        {
            byte[] b3 = new byte[b1.Length];
            for (int i = 0; i < b1.Length; i++)
            {
                b3[i] = Convert.ToByte(b1[i] ^ b2[i]);
            }
            return b3;
        }
        /// <summary>
        /// 获得分片数据
        /// </summary>
        /// <returns></returns>
        private List<SlicingDataInfo> GetSlicingFiles(string fileName)
        {

            DirectoryInfo d = new DirectoryInfo(this.DPath);
            //得到目录下所有的分片数据
            FileInfo[] files = d.GetFiles(fileName);
            List<SlicingDataInfo> slicingDataList = new List<SlicingDataInfo>();
            for (int i = 0; i < files.Length; i++)
            {
                //得到目录中每片分片的数据，行号，列号，并存入list集合
                SlicingDataInfo sd = new SlicingDataInfo() { ColNum = Path.GetFileNameWithoutExtension(files[i].Name).Split('_')[2], RowNum = Path.GetFileNameWithoutExtension(files[i].Name).Split('_')[1], Data = File.ReadAllBytes(files[i].FullName) };
                slicingDataList.Add(sd);

            }
         
            return slicingDataList;
        }
        /// <summary>
        /// 数据恢复操作
        /// </summary>
        /// <returns></returns>
        public string Recovery()
        {
            DirectoryInfo d = new DirectoryInfo(this.DPath);
            var files = d.GetFiles("slice*.dat");
            int length=   File.ReadAllBytes(files [0].FullName).Length ;
            if (files.Length != 0)
            {
                string message = "数据恢复成功";
                List<string> deleteFiles = CheckDeletedSlices(files);
                GoRecoverDeletedFiles(deleteFiles);
                var filesNew = d.GetFiles("slice*.dat");
                List<string> cantRec= CheckDeletedSlices(filesNew);
                if (cantRec .Count>0)
                {
                    FillZero(cantRec,length);
                    message = "有部分数据无法恢复";
                }
                CombineSlice();
                
                return message ;
            }
            else
            {
                return "没有分片数据！";
            }


        }

        /// <summary>
        /// 不可恢复数据填充0
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="byteLength"></param>
        private void FillZero(List<string> fileName,int byteLength)
        { 
                byte[] b = new byte[byteLength  ];
                for (int i = 0; i <b.Length ; i++)
                {
                    b[i] = 0;
                }
            var files = fileName.ToArray();
            for (int i = 0; i < files .Length ; i++)
            {
                FileStream fs = new FileStream(this.DPath + "\\" + files[i], FileMode.Create);
                fs.Write(b, 0, b.Length);
                fs.Close();
                fs.Dispose();
            }
                
        }

        /// <summary>
        /// 检查哪些文件被删除
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private List<string> CheckDeletedSlices(FileInfo[] files)
        {
            //生成文件名
            List<string> fileNameList = new List<string>();
            for (int i = 1; i <= this.SliceNum + 1; i++)
            {
                for (int j = 1; j <= SliceNum + 1; j++)
                {
                    fileNameList.Add("slice_" + i.ToString() + "_" + j.ToString() + ".dat");
                }
            }
            //移除不能出现的异或文件名
            fileNameList.Remove("slice_" + (this.SliceNum + 1).ToString() + "_" + (this.SliceNum + 1).ToString() + ".dat");
            //List<string> existsFileNameList = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                //移除已存在的文件名
                fileNameList.Remove(files[i].Name);
            }
            return fileNameList;

        }

        private void GoRecoverDeletedFiles(List<string> deletedFileList)
        {
          
            string[] list = deletedFileList.ToArray();
             //恢复
                RecoverDeletedFiles(list);
  
        }

        public static int errorTime = 0;
        /// <summary>
        /// 恢复文件算法
        /// </summary>
        /// <param name="list"></param>
        private void RecoverDeletedFiles(string[] list)
        {
            //存储第一次没有恢复成功的文件
          
            List<string> noRec = new List<string>();
            for (int i = 0; i < list.Length; i++)
            {
                string f = RecoverSingleFile(list[i]);
                if (f != "")
                {
                    noRec.Add(f);
                }
            }
            if (noRec.Count != 0)
            {
                //递归
                errorTime += 1;
                if (errorTime>list.Length )
                {
                 //跳出递归
                    return ;
                  
                }
                RecoverDeletedFiles(noRec .ToArray ());
            }
  
        }

        /// <summary>
        /// 恢复单个分片数据
        /// </summary>
        /// <param name="fileName"></param>
        private  string RecoverSingleFile(string fileName)
        {
            
            string name= Path.GetFileNameWithoutExtension(fileName );
            string r = name.Split('_')[1];
            string c =name.Split('_')[2];
            DirectoryInfo di = new DirectoryInfo(this.DPath);
            FileInfo []fr= di.GetFiles("slice_" + r + "_*.dat");
            FileInfo []fc= di.GetFiles("slice_*_" + c + ".dat");
            //满足行恢复条件
                if (fr.Length == this.SliceNum)
                {
                    GoToRecovery(r, "r", fileName);
                    return "";
                }
            //满足列恢复条件
                if (fc.Length == this.SliceNum)
                {
                    GoToRecovery(c, "c", fileName);
                    return "";
                }
                    //不能恢复返回文件名
                else
                {               
                    return fileName;               
                }    

        }

        /// <summary>
        /// 数据恢复算法
        /// </summary>
        /// <param name="rowNum"></param>
        /// <param name="type"></param>
        /// <param name="fileName"></param>
        private void GoToRecovery(string rowOrColNum, string type, string fileName)
        {
           
            List<SlicingDataInfo> list;
            if (type == "r")
            {
                //按行恢复数据
                list = GetSlicingFiles("slice_" + rowOrColNum + "_*.dat");
                GoCreateXORFile(list, fileName);

            }
            else if (type =="c")
            {
                //按列恢复数据
                list = GetSlicingFiles("slice_*_" + rowOrColNum + ".dat");
                GoCreateXORFile(list, fileName);
            }
          
        }


        /// <summary>
        /// 组和分片信息，还原原始数据
        /// </summary>
        private void CombineSlice()
        {
            //得到恢复完的所有文件
            List<SlicingDataInfo> list = GetSlicingFiles("slice*.dat");
            //去除异或的文件
            list.RemoveAll((t) => { return t.RowNum.Contains((this.SliceNum + 1).ToString()) || t.ColNum.Contains((this.SliceNum + 1).ToString()); });
         list=   list.OrderBy(t=>Convert .ToInt32 (t.RowNum )).ThenBy(t=>Convert .ToInt32 ( t.ColNum) ).ToList ();
        // list.OrderBy((t) => (Convert.ToInt32(t.RowNum),);

            using (FileStream fs = new FileStream(this.DPath + "\\target.dat", FileMode.Create))
            {

                for (int i=0;i<list.Count-1 ;i++)
                {

                    fs.Write(list[i].Data, 0, list[i].Data.Length);
                }
                if (File.Exists ("conf.ini"))
                {
                    string t = File.ReadAllText("conf.ini");
                    fs.Write(list.LastOrDefault().Data, 0,Convert .ToInt32 (t));
                }
                else
                {
                    fs.Write(list.LastOrDefault().Data, 0, list.LastOrDefault().Data.Length );
                }
             
               
            }
        }

    }

}
