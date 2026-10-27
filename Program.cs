using System;
using AutoCreateTag.source;

namespace AutoCreateTag;

class ProgramMain
{
    static void Main(string[] args)
    {
        FileManager fileManager = new FileManager();
        fileManager.StartFlow();
    }
}