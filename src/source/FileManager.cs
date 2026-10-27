using System.Text;

namespace AutoCreateTag.source;

//文件管理, 检查是否存在,是否需要创建等,以及重写文件内容
public class FileManager
{
    public FileManager()
    {
        SourceFilePath = "";
        TagFileHeaderPath = "";
        TagFilePath = "";
    }

    //启动流程
    public void StartFlow()
    {
        if (TryReadConfigFile())
        {
        }
        else
        {
            CreateTag();
        }

        //
        StartReadConfigFile();
    }

    private void CreateTag()
    {
        if (string.IsNullOrEmpty(SourceFilePath))
        {
            Console.WriteLine("Input Source file path:");
            SourceFilePath = Console.ReadLine();
        }


        if (string.IsNullOrEmpty(TagFileHeaderPath))
        {
            Console.WriteLine("input out head file path:");
            TagFileHeaderPath = Console.ReadLine();
        }

        if (string.IsNullOrEmpty(TagFilePath))
        {
            Console.WriteLine("input out file path:");
            TagFilePath = Console.ReadLine();
        }

        if (!CheckUserInputValid(SourceFilePath))
        {
            return;
        }


        if (!CheckUserInputValid(TagFileHeaderPath))
        {
            return;
        }

        if (!CheckUserInputValid(TagFilePath))
        {
            return;
        }

        ConfigSaveToFile();
    }

    //检查文件是否存在
    private bool CheckFileExist(string inFilePath)
    {
        return File.Exists(inFilePath);
        // else if (Directory.Exists(inFilePath))
        // {
        //     return true;
        // }
    }

    private bool CheckUserInputValid(string inFilePath)
    {
        if (string.IsNullOrEmpty(inFilePath))
        {
            return false;
        }

        if (!CheckFileExist(inFilePath))
        {
            Console.WriteLine("File: {0} no Exist", inFilePath);
            Console.ReadKey();
            return false;
        }

        return true;
    }
    //创建文件 ,不需要

    //配置文件写入,长久保存
    private void ConfigSaveToFile()
    {
        string path = "./config.txt";
        File.WriteAllText(path, SourceFilePath);
        File.AppendAllText(path, "\n");
        File.AppendAllText(path, TagFileHeaderPath);
        File.AppendAllText(path, "\n");
        File.AppendAllText(path, TagFilePath);
    }

    //尝试读取配置文件,避免再次输入
    private bool TryReadConfigFile()
    {
        string path = "./config.txt";
        if (File.Exists(path))
        {
            IEnumerable<string> content = File.ReadLines(path);
            string[] lines = content.ToArray();
            if (lines.Length > 0)
            {
                SourceFilePath = lines[0];
                TagFileHeaderPath = lines[1];
                TagFilePath = lines[2];
                return true;
            }
        }

        return false;
    }

    //
    void StartReadConfigFile()
    {
        IEnumerable<string> lines = File.ReadAllLines(SourceFilePath);
        SortedDictionary<string, List<ContentData>> contentDataMap = new SortedDictionary<string, List<ContentData>>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            ContentData data;
            if (FileAnalysis.AnalysisFile(line, out data))
            {
                if (contentDataMap.ContainsKey(data.Type))
                {
                    contentDataMap[data.Type].Add(data);
                }
                else
                {
                    List<ContentData> tempData = new List<ContentData>();
                    tempData.Add(data);
                    contentDataMap.Add(data.Type, tempData);
                }
            }
        }

        WriteContentToHeadFile(contentDataMap);
        WriteContentToFile(contentDataMap);
    }

    //根据传过来的内容,重新写入文件中
    private void WriteContentToHeadFile(in SortedDictionary<string, List<ContentData>> inData)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("#pragma once\n#include \"CoreMinimal.h\"\n#include \"NativeGameplayTags.h\"\n namespace GameplayTags{\n");
        foreach (KeyValuePair<string, List<ContentData>> pair in inData)
        {
            sb.Append("//--------------------").Append(pair.Key).Append("\n");
            foreach (ContentData data in pair.Value)
            {
                sb.Append("UE_DECLARE_GAMEPLAY_TAG_EXTERN(").Append(data.NewTag).Append(")\n");
            }

            sb.Append("\n\n");
        }

        sb.Append("}");
        File.WriteAllText(TagFileHeaderPath, sb.ToString());
    }

    private void WriteContentToFile(in SortedDictionary<string, List<ContentData>> inData)
    {
        StringBuilder sb = new StringBuilder();
        string headPath = GetHeadFilePath();
        sb.Append("#include \"").Append(headPath).Append("\"\nnamespace GameplayTags {\n");
        foreach (KeyValuePair<string, List<ContentData>> pair in inData)
        {
            sb.Append("//-----------------------").Append(pair.Key).Append("\n");
            foreach (ContentData data in pair.Value)
            {
                sb.Append("UE_DEFINE_GAMEPLAY_TAG_COMMENT(").Append(data.NewTag).Append(", \"").Append(data.OldTag).Append("\", \"").Append(data.Comment).Append("\");\n");
            }
        }

        File.WriteAllText(TagFilePath, sb.ToString());
    }

    //提取头文件路径
    private string GetHeadFilePath()
    {
        string[] lists = TagFileHeaderPath.Split("Public\\");
        return lists.Last();
    }

    private string SourceFilePath;
    private string TagFileHeaderPath;
    private string TagFilePath;
}