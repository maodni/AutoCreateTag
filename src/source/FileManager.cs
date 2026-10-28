using System.Text;

namespace AutoCreateTag.source;

//文件管理, 检查是否存在,是否需要创建等,以及重写文件内容
public class FileManager
{
    // public FileManager()
    // {
    //     SourceFilePath = "";
    //     TagFileHeaderPath = "";
    //     TagFilePath = "";
    // }

    //启动流程
    public void StartFlow()
    {
        if (!TryReadConfigFile())
        {
            if (!CreateTag())
            {
                return;
            }
        }

        //
        StartReadConfigFile();
    }

    private bool CreateTag()
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
            return false;
        }


        if (!CheckUserInputValid(TagFileHeaderPath))
        {
            return false;
        }

        if (!CheckUserInputValid(TagFilePath))
        {
            return false;
        }

        ConfigSaveToFile();
        return true;
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
        string str = "";
        str = string.Join(SourceFilePath, str);
        str = string.Join("\n", str);
        str = string.Join(TagFileHeaderPath, str);
        str = string.Join("\n", str);
        str = string.Join(TagFilePath, str);
        File.AppendAllText(path, str);
    }

    //尝试读取配置文件,避免再次输入
    private bool TryReadConfigFile()
    {
        string path = "./config.txt";
        if (File.Exists(path))
        {
            IEnumerable<string> content = File.ReadLines(path);
            string[] lines = content.ToArray();
            if (lines.Length >= 3)
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
    private void WriteContentToHeadFile(SortedDictionary<string, List<ContentData>> inData)
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

    private void WriteContentToFile(SortedDictionary<string, List<ContentData>> inData)
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

/*
 * 公有字段 vs 属性
你现在的写法（C++ 视角完全合理）
csharp

public class ContentData
{
    public ContentData()   // 构造函数专门用来初始化
    {
        Comment = ""; OldTag = ""; NewTag = ""; Type = "";
    }
    public string OldTag;  // 字段，直接公开
}
C++ 里 struct 公开成员天经地义，想要 getter/setter 得手写一堆，所以大家干脆裸奔。C# 的自动属性把这套成本降到了零：

csharp

public class ContentData
{
    public string OldTag   { get; set; } = "";
    public string NewTag   { get; set; } = "";
    public string Type     { get; set; } = "";
    public string Comment  { get; set; } = "";
}
4 行搞定，构造函数整个删掉（= ""; 叫初始化器，编译器会把它编进构造流程最前面）。

那为什么属性比字段“高级”？关键在演化自由
属性本质是一对方法（编译成 get_OldTag() / set_OldTag()），字段是一块裸内存。这个差别平时看不出来，出事的时候才致命：

csharp

// 假设三个月后你想：Comment 不允许存前后空格
// 属性：改这一处，所有调用方无感
public string Comment
{
    get => comment;
    set => comment = value.Trim();
}
private string comment;
如果当初公开的是字段，想加这个逻辑就得把字段改成属性——字段和属性在编译产物、反射、数据绑定、JSON 序列化眼里是两种完全不同的东西，跨 dll 引用的地方全部要重编译，反射找成员的代码直接失效。字段是“一次公开，终身裸奔”。

所以 C# 的社区准则就一句话：类对外暴露状态用属性，字段永远 private。{ get; set; } 写起来和字段一样短，却留了后门，白拿的保险为什么不要~

2️⃣ ContainsKey + 下标 → TryGetValue
你现在的写法
csharp

if (contentDataMap.ContainsKey(data.Type))   // 查找 ①：遍历红黑树找到 key
{
    contentDataMap[data.Type].Add(data);     // 查找 ②：又遍历一遍！
}
else
{
    List<ContentData> tempData = new List<ContentData>();
    tempData.Add(data);
    contentDataMap.Add(data.Type, tempData); // 查找 ③：Add 内部还要再查一次（防重复 key）
}
C++ 里这么写太常见了，map.count() + map[key] 嘛。但注意 C++ 老手其实会告诉你正确姿势是：

cpp

if (auto it = map.find(key); it != map.end()) {
    it->second.push_back(data);      // 拿着迭代器直接用，不找第二次
} else {
    map.emplace(key, {data});
}
TryGetValue 就是 C++ 这个 find + iterator 写法的直接等价物——一次查找，拿到引用直接用：

csharp

if (contentDataMap.TryGetValue(data.Type, out List<ContentData> list))
{
    list.Add(data);                          // 一次查找完成
}
else
{
    contentDataMap[data.Type] = new List<ContentData> { data };
}
TryGetValue 把“查找”和“取值”合并成一个原子操作：找到就返回 true 并通过 out 把 value 给你，找不到返回 false。数据量小时性能差异不明显，但它在 C# 里已经强到任何代码审查看到 ContainsKey + 索引器都会直接标出来，属于必改项。

⚠️ 顺带一个 C# 陷阱：SortedDictionary 的索引器 map[key] 在 key 不存在时不会像 C++ operator[] 那样默认构造插入，而是直接抛 KeyNotFoundException——这也是为什么不能学 C++ 那样直接 map[key].Add(...)。

3️⃣ ReadAllLines + 正则
先说读文件这个
csharp

IEnumerable<string> content = File.ReadLines(path);   // 惰性流式：一行一行读
string[] lines = content.ToArray();                   // 又一次性全 materialize
这两个 API 的定位不同：

API	行为	适用场景
File.ReadLines()	惰性，返回 IEnumerable，读一行处理一行	几 GB 大文件，边读边过滤，内存友好
File.ReadAllLines()	立即，一把读完返回 string[]	配置这种小文件
你 ReadLines 完立刻 ToArray = 把流式强行变一次性，绕了一圈结果等价于 ReadAllLines，还多一层包装。C++ 没有现成的“读全文件”标准 API，你们习惯 getline 循环手撸，C# 里这事儿有专门的一条路：

csharp

string[] lines = File.ReadAllLines(path);
（另外 StartReadConfigFile 里 IEnumerable<string> lines = File.ReadAllLines(...)，变量声成 IEnumerable 把“它是数组”这个事实藏起来了，直接 string[] 更诚实。）

再说解析，这个是重头
你在解析这种行：

plaintext

+GameplayTagList = (Tag="GameplayEffect.Damage.Type.Environment",DevComment="环境伤害")
你的做法是 Split('=') 然后手工数偏移：

csharp

outData.Comment = cacheStrList.Last().Substring(1, cacheStrList.Last().Length - 3);
//  1 = 跳过开头的引号，-3 = 去掉结尾的 ")"  ← 全靠脑内数格子
这么写的问题不只是难读——它脆。给你一个游戏配置里百分百会出现的例子：

plaintext

+GameplayTagList = (Tag="Effect.Damage",DevComment="攻击力=10时的伤害")
DevComment 里出现 =！Split('=') 会切成 5 段，.Last() 拿到的是 时的伤害")——解析直接错，而且没有任何报错，错的数据悄悄写进生成的 .h 文件里。同理：注释里带逗号会切坏逗号分割、格式里多个空格全盘皆输。

正则把“格式长什么样”声明式地写出来，交给引擎去匹配：

csharp

using System.Text.RegularExpressions;

private static readonly Regex TagLine = new(
    @"Tag\s*=\s*""(?<tag>[^""]+)""\s*,\s*DevComment\s*=\s*""(?<comment>[^""]*)""",
    RegexOptions.Compiled);

public static bool AnalysisFile(string line, out ContentData outData)
{
    Match m = TagLine.Match(line);
    if (!m.Success) { outData = null; return false; }

    string oldTag = m.Groups["tag"].Value;       // 按名字取，自文档化
    outData = new ContentData
    {
        OldTag = oldTag,
        NewTag = oldTag.Replace('.', '_'),
        Comment = m.Groups["comment"].Value,     // 注释里有 = 有逗号？随便，[^"]* 全兜住
        Type = TakeFirstTwoSegments(oldTag),
    };
    return true;
}
对着这条正则逐段翻译成人话：

Tag\s*=\s* → “Tag 这个词，等号，等号两边随便几个空格”（你的 Split 方案对空格零容忍）
""(?<tag>[^""]+)"" → 一对引号，中间捕获 1 个以上非引号字符，命名为 tag（C# 的 @"..." verbatim 字符串里 "" 代表一个引号，不用写 \"）
(?<comment>[^""]*) → 注释捕获组，* 允许空注释，引号里装什么内容都不影响外层结构
Type 的提取也不用 firstIndex/secondIndex 地狱了，直接：

csharp

private static string TakeFirstTwoSegments(string tag)
{
    string[] parts = tag.Split('.');
    return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : tag;
}
为什么 C++ 程序员不爱正则、C# 里却是主流
说句公道话：C++ 的 std::regex 又慢、又没有 verbatim 字符串（转义地狱 \\\"）、报错信息天书，所以 C++ 圈确实偏好手撸解析。但 C# 这边正则引擎快、@"..." 免转义、RegexOptions.Compiled 还能编译成 IL、命名捕获组自带文档性——同样一条正则，两种语言里的使用体验是两个世界。这是很典型的“习惯迁移了，但语言环境没跟上”。
 */