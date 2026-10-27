namespace AutoCreateTag.source;

//文件解析,提取数据,

public class ContentData
{
    public ContentData()
    {
        Comment = "";
        OldTag = "";
        NewTag = "";
        Type = "";
    }

    //原始数据
    public string OldTag;

    //处理后的数据
    public string NewTag;

    //用来划分区域的数据,取标签的前2段
    public string Type;

    //说明
    public string Comment;
}

public struct FileAnalysis
{
    //文件解析,提取内容


    public static bool AnalysisFile(string inStr, out ContentData outData)
    {
        outData = new ContentData();
        string tempStr = inStr.Trim();
        // string tempStr = "+GameplayTagList = (Tag=\"GameplayEffect.Damage.Type.Environment\",DevComment=\"环境伤害\")";
        // +GameplayTagList = (Tag="GameplayEffect.Damage.Type.Environment",DevComment="环境伤害")
        if (tempStr.Contains("+GameplayTagList"))
        {
            int index = tempStr.IndexOf('=');
            string cacheStr = "";
            string[] cacheStrList = Array.Empty<string>();
            if (index >= 0)
            {
                cacheStr = tempStr.Substring(index + 1);

                cacheStrList = cacheStr.Split('=');
            }


            outData.Comment = cacheStrList.Last().Substring(1, cacheStrList.Last().Length - 3);

            string[] tempTag = cacheStrList[1].Split(',');
            int firstIndex = tempTag[0].IndexOf('.');
            int secondIndex = tempTag[0].IndexOf('.', firstIndex + 1);

            if (-1 == firstIndex)
            {
                outData.Type = tempTag[0].Substring(1, tempTag[0].Length - 2);
            }
            else if (-1 == secondIndex)
            {
                outData.Type = tempTag[0].Substring(1, tempTag[0].Length - 2);
            }
            else
            {
                outData.Type = tempTag[0].Substring(1, secondIndex - 1);
            }


            outData.OldTag = tempTag[0].Substring(1, tempTag[0].Length - 2);

            outData.NewTag = outData.OldTag.Replace('.', '_');

            return true;
        }

        outData.OldTag = "";
        outData.NewTag = "";
        outData.Type = "";
        outData.Comment = "";
        return false;
    }
}