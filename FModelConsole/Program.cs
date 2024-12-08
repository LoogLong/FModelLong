// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using System;
using CUE4Parse.GameTypes.InfinityNikki.Encryption.Aes;
using SharpGLTF.Schema2;

// Create a timer
var watch = System.Diagnostics.Stopwatch.StartNew();
watch.Start();
Console.WriteLine("FModel Console Exporter");

if (args.Length < 5)
{
    Console.WriteLine("usage: FModelConsole.exe [PAKPATH] [EXPORTPATH] [AESKEY] [CONTENTPATH] [TYPE]" );
    Console.WriteLine("eg: FModelConsole.exe D:\\FinalFantasyVIIRemake\\End\\Content\\Paks D:\\FF7RExport 0xABCD End/Content/Environment StaticMesh" );
    if (args.Length != 0)
    {
        return;
    }
}

var argCount = 0;

var GAME_ENUM = args.Length > argCount ? args[argCount++] : "GAME_FinalFantasy7Remake";
var ARCHIVE_DIRECTORY_HERE = args.Length > argCount ? args[argCount++] : "D:\\BOTW\\Wuf\\Client\\Client\\Content\\Paks";
var EXPORT_PATH = args.Length > argCount ? new DirectoryInfo(args[argCount++]) : new DirectoryInfo("D:\\BOTW\\BlackMythWukong");
var REPLACE_PATH = args.Length > argCount ? args[argCount++] : "";
var AESKEY = args.Length > argCount
    ? new FAesKey(args[argCount++])
    : new FAesKey("0xF259C330E6B308BF34086CF30013241A1277F6E25D8F580746C2E8829EA1E15F");
var PACKAGE_PATH_HERE = args.Length > argCount ? args[argCount++] : "Client/Content/Aki/Map/AkiWorld_WP/_Generated_/WPRT_AkiWorld_WP_Grid_SSuperFar";
var OBJECT_TYPE = args.Length > argCount ? args[argCount++] : "World"; // World SkeletalMesh StaticMesh AnimSequence

var MAPPING_FILE = args.Length > argCount ? args[argCount++] : "";

var PARSEONLY = false;

var aesKeys = new Dictionary<FGuid, FAesKey>();
if (true)
{
    GAME_ENUM = "GAME_InfinityNikki";
    ARCHIVE_DIRECTORY_HERE = "G:\\game\\InfinityNikki Launcher\\InfinityNikki";
    var aesKey = new FAesKey("0xF0F2BA714FE32FACC23CD332BF35E8A00F73937BA4BB6D26659276A31E714E84");
    var guid = new FGuid();
    aesKeys.Add(guid, aesKey);

    aesKey = new FAesKey("0x73E45FAC0AC7E5419E231612084BCE8A8C0484DE3BC2123FFD975477E2EB709F");
    guid = new FGuid("380C7ABE421FDBAFDE6300B831B6C363");
    aesKeys.Add(guid, aesKey);

    aesKey = new FAesKey("0x9822579421D43B32F1FE266E70E19B1B7230C33F7E485C3960324D210CD7E820");
    guid = new FGuid("B28BB9064D33EA04AF17B186C1694C49");
    aesKeys.Add(guid, aesKey);

    aesKey = new FAesKey("0x4A20DF4F3530159919B6EDAA55C9A29796E43EA7A2FE956BCA659F58CA9302D4");
    guid = new FGuid("EC2C8DCE40EF6B57541D81AFFA3CEA0F");
    aesKeys.Add(guid, aesKey);

    PACKAGE_PATH_HERE = "X6Game/Content/Assets/Buildin/Character/Main/Nikki";
    MAPPING_FILE = "G:\\GameExport\\.data\\REL_5.4.4-0+UE5-X6Game.usmap";
    EXPORT_PATH = new DirectoryInfo("G:\\GameExport");
}

if (false)
{
    GAME_ENUM = "GAME_BlackMythWukong";
    ARCHIVE_DIRECTORY_HERE = "D:\\WeGameApps\\rail_apps\\BlackMythWukong(2002122)\\b1\\Content\\Paks";
    AESKEY = new FAesKey("0xA896068444F496956900542A215367688B49B19C2537FCD2743D8585BA1EB128");
    PACKAGE_PATH_HERE = "b1/Content/00MainHZ/Environment/Buildings/Meshs/LYS/Objects/JinGangXiang";
    REPLACE_PATH = "/Content/BlackMyth/";
    OBJECT_TYPE = "StaticMesh";
    MAPPING_FILE = "D:\\GKNIFE\\LocalBuilds\\WindowsClient\\LightingBeast\\Content\\Mappings.usmap";
}

if (false)
{
    GAME_ENUM = "GAME_WutheringWaves";
    ARCHIVE_DIRECTORY_HERE = "G:\\game\\Wuthering Waves\\Wuthering Waves Game\\Client\\Content\\Paks";
    AESKEY = new FAesKey("0x8CE9D7D7635F1CB0DAD9574B03A6396F97E94BE07A42D014933D2A9014D057AD");
    // PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/Chun";
    // PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/BaseAnim";
    // PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/BaseAnim2";
    // PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/Nvzhu";
    PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/BaseAnim/Stand1";
    MAPPING_FILE = "";
    EXPORT_PATH = new DirectoryInfo("G:\\GameExport");
}

var EXPORT_OPTIONS = new ExporterOptions
{
    LodFormat = ELodFormat.AllLods,
    MeshFormat = EMeshFormat.UEFormat,
    AnimFormat = EAnimFormat.UEFormat,
    MaterialFormat = EMaterialFormat.AllLayers,
    TextureFormat = ETextureFormat.Png,
    SocketFormat = ESocketFormat.None,
    Platform = ETexturePlatform.DesktopMobile,
    ExportMorphTargets = true,
    ExportMaterials = true,
    CompressionFormat = EFileCompressionFormat.None
};

EGame game = Enum.Parse<EGame>(GAME_ENUM);

//Client/Content/Aki/Map/AkiWorld_WP/_Generated_/WPRT_AkiWorld_WP_Grid_AudioFar_Cell_L0_X-10_Y17_Z0_DL0_0.umap
var provider = new DefaultFileProvider(ARCHIVE_DIRECTORY_HERE, SearchOption.AllDirectories, true, new VersionContainer(game));
provider.CustomEncryption = provider.Versions.Game switch
{
    EGame.GAME_InfinityNikki => InfinityNikkiAes.InfinityNikkiDecrypt,
    _ => provider.CustomEncryption
};
provider.Initialize(); // will scan the archive directory for supported file extensions
if (MAPPING_FILE != "" && MAPPING_FILE != "None")
{
    provider.MappingsContainer = new FileUsmapTypeMappingsProvider(MAPPING_FILE);
}

provider.SubmitKeys(aesKeys);
provider.PostMount();
provider.LoadVirtualPaths(game.GetVersion());
{
    var OutputDirectory = EXPORT_PATH.ToString();
    var oodlePath = Path.Combine(OutputDirectory, ".data", OodleHelper.OODLE_DLL_NAME);
    if (File.Exists(OodleHelper.OODLE_DLL_NAME))
    {
        File.Move(OodleHelper.OODLE_DLL_NAME, oodlePath, true);
    }
    else if (!File.Exists(oodlePath))
    {
        await OodleHelper.DownloadOodleDllAsync(oodlePath);
    }

    OodleHelper.Initialize(oodlePath);
}
watch.Stop();
var initTime = watch.Elapsed.TotalSeconds * 1000.0;
watch.Reset();

watch.Start();

List<GameFile> allValidateFile = new();
List<UObject> allExports = new();
var totalSubs = 0;
//fetch all files start with package path
foreach (var file in provider.Files)
{
    if ((file.Value.Extension == "umap" || file.Value.Extension == "uasset") && !file.Value.Name.Contains("_BuiltData") && file.Key.StartsWith(PACKAGE_PATH_HERE.ToLower()))
    {
        allValidateFile.Add(file.Value);
    }
}

Console.WriteLine("-----------------------------------------------");
Console.WriteLine($"Exporting: {PACKAGE_PATH_HERE}:{OBJECT_TYPE} -> {EXPORT_PATH}");
Console.WriteLine($"Filtered Files: {allValidateFile.Count:0}");

if(false)
{
    List<Package> allSecondaryMI = new();
    List<Package> allMI = new();
    foreach (var file in allValidateFile)
    {
        bool IsType = false;
        bool SecondaryMI = false;
        string parentName = "";
        Package package = provider.LoadPackage(file.Path) as Package; // {GAME}/Content/Folder1/Folder2/PackageName.uasset

        for (var i = 0; i < package.ExportMap.Length; i++)
        {  
            if (package.ExportMap[i].ClassName.Equals("MaterialInstanceConstant", StringComparison.OrdinalIgnoreCase))
            {
                IsType = true;
                allMI.Add(package);
                break;
            }
        }

        if (!IsType)
        {
            continue;
        }

        // cast to Package
        for (var i = 0; i < package.ImportMap.Length; i++)
        {
            if (package.ImportMap[i].ClassName.Text.Equals("Material", StringComparison.OrdinalIgnoreCase))
            {
                parentName = package.ImportMap[i].ObjectName.Text;
                SecondaryMI = true;
                break;
            }
        }

        if (SecondaryMI)
        {
            //Console.WriteLine($"{package.GetPathName()} > {parentName}");
            allSecondaryMI.Add(package);
        }
    }

    // remove package in allMI that is in allSecondaryMI
    foreach (var MI in allSecondaryMI)
    {
        allMI.Remove(MI);
    }
    
    Console.WriteLine($"2nd MI / 3rd MI: {allSecondaryMI.Count:0} / {allMI.Count:0}");

    int TotalVariants = 0;
    int Total2ndVariants = 0;
    int Total3rdVariants = 0;
    Dictionary<String, List<String> > MasterMaterialSwitchMap = new();
    {
        // for all 2nd mi, load and fetch switches
        foreach (var MI in allSecondaryMI)
        {
            var allObjects = provider.LoadAllObjects(MI.GetPathName());
            foreach (var export in allObjects)
            {
                if (export.ExportType == "MaterialInstanceConstant")
                {
                    var mi = export as UMaterialInstanceConstant;
                    if (mi.Parent != null && mi.StaticParameters != null)
                    {
                        var parentName = mi.Parent.GetPathName();
                        if (!MasterMaterialSwitchMap.ContainsKey(parentName))
                        {
                            MasterMaterialSwitchMap[parentName] = new List<String>();
                        }
                        
                        //Console.WriteLine($"{mi.GetPathName()} > {mi.Parent.GetPathName()}");

                        var variantHash = "";
                        foreach(var miSWC in mi.StaticParameters.StaticSwitchParameters)
                        {
                            string SingleSWC = miSWC.Name + miSWC.Value;
                            
                            variantHash += SingleSWC;
                        }

                        if(mi.BasePropertyOverrides != null)
                        {
                            string SingleOverride = mi.BasePropertyOverrides.ShadingModel.ToString() +
                                                    mi.BasePropertyOverrides.BlendMode.ToString() +
                                                    mi.BasePropertyOverrides.OpacityMaskClipValue.ToString();
                            variantHash += SingleOverride;
                        }
                        
                        if (!MasterMaterialSwitchMap[parentName].Contains(variantHash))
                        {
                            MasterMaterialSwitchMap[parentName].Add(variantHash);
                            Total2ndVariants++;
                        }
                    }
                }
            }
        }
    }
    
    {
        // for all 2nd mi, load and fetch switches
        foreach (var MI in allMI)
        {
            var allObjects = provider.LoadAllObjects(MI.GetPathName());
            foreach (var export in allObjects)
            {
                if (export.ExportType == "MaterialInstanceConstant")
                {
                    var mi = export as UMaterialInstanceConstant;
                    if (mi.Parent != null && mi.StaticParameters != null)
                    {
                        var parentName = mi.Parent.GetPathName();
                        if (!MasterMaterialSwitchMap.ContainsKey(parentName))
                        {
                            MasterMaterialSwitchMap[parentName] = new List<String>();
                        }
                        
                        //Console.WriteLine($"{mi.GetPathName()} > {mi.Parent.GetPathName()}");

                        var variantHash = "";
                        foreach(var miSWC in mi.StaticParameters.StaticSwitchParameters)
                        {
                            string SingleSWC = miSWC.Name + miSWC.Value;
                            
                            variantHash += SingleSWC;
                        }

                        if(mi.BasePropertyOverrides != null)
                        {
                            string SingleOverride = mi.BasePropertyOverrides.ShadingModel.ToString() +
                                                    mi.BasePropertyOverrides.BlendMode.ToString() +
                                                    mi.BasePropertyOverrides.OpacityMaskClipValue.ToString();
                            variantHash += SingleOverride;
                        }
                        
                        if (!MasterMaterialSwitchMap[parentName].Contains(variantHash))
                        {
                            MasterMaterialSwitchMap[parentName].Add(variantHash);
                            Console.WriteLine(variantHash);
                            Total3rdVariants++;
                        }
                    }
                }
            }
        }
    }
    
    // Log the MasterMaterialSwitchMap
    
    foreach (var entry in MasterMaterialSwitchMap)
    {
        Console.WriteLine($"{entry.Key} > {entry.Value.Count:0}");
        // log all hash
        foreach (var hash in entry.Value)
        {
            //Console.WriteLine($"  {hash}");
        }
        TotalVariants += entry.Value.Count;
    }
        
    Console.WriteLine($"2nd / 3rd / Total Variants: {Total2ndVariants:0}, {Total3rdVariants:0}, {TotalVariants:0}");  
   
    
    return;
}
var bExportSkeletalMesh = true;
// var bExportStaticMesh = false;true
var bExportAnimSequence = false;

foreach (var file in allValidateFile)
{
    var allObjects = provider.LoadAllObjects(file.Path); // {GAME}/Content/Folder1/Folder2/PackageName.uasset
    foreach (var export in allObjects)
    {
        if (bExportSkeletalMesh)
        {
            if (export.ExportType == "SkeletalMesh")
            {
                allExports.Add(export);
            }
        }

        if (bExportAnimSequence)
        {
            if (export.ExportType == "AnimSequence")
            {
                if (export.Name == "Stand1")
                {
                    allExports.Add(export);
                }
            }
        }
    }
}

Console.WriteLine($"FirstLayer Exports: {allExports.Count:0}");

watch.Stop();
var prepareTime = watch.Elapsed.TotalSeconds * 1000.0;
watch.Reset();
watch.Start();

// get cpu threads num
var cpuThreads = Environment.ProcessorCount;
#if DEBUG
cpuThreads = 1;
#endif

ConcurrentQueue<UObject> stack = new ();
foreach (var export in allExports)
{
    stack.Enqueue(export);
}

var COUNTER = 0;
ulong TOTAL_SIZE = 0;

using (var progress = new ProgressBar())
{
    while (stack.Count > 0)
    {

        ConcurrentQueue<UObject> stackCopy = new ConcurrentQueue<UObject>();
        List<string> guids = new();
        foreach (var obj in stack)
        {
            if (!guids.Contains(obj.GetPathName()))
            {
                stackCopy.Enqueue(obj);
                guids.Add(obj.GetPathName());
            }
        }

        totalSubs += stackCopy.Count;
        stack.Clear();

        Parallel.For(0, cpuThreads, i =>
        {
            while (true)
            {
                UObject export;
                if (stackCopy.TryDequeue(out export))
                {
                    var threadDependencies = new List<UObject>();
                    var sub = new Exporter(export, EXPORT_OPTIONS);
                    if (!PARSEONLY)
                    {
                        if (sub.TryWriteToDir(EXPORT_PATH, out var label1, out var savedFilePath1))
                        {
                            Interlocked.Increment(ref COUNTER);
                            progress.ReportExt((double)COUNTER / (double)totalSubs, Path.GetFileName(savedFilePath1));
                            if (File.Exists(savedFilePath1))
                            {
                                var fs = new FileInfo(savedFilePath1);
                                Interlocked.Add(ref TOTAL_SIZE, (ulong)fs.Length);
                            }
                        }
                    }

                    // remove duplicates in threadDependencies
                    var simpleThreadDependencies = new List<UObject>();
                    foreach (var threadDependency in threadDependencies)
                    {
                        if (!simpleThreadDependencies.Contains(threadDependency))
                        {
                            simpleThreadDependencies.Add(threadDependency);
                        }
                    }

                    foreach (var newly in simpleThreadDependencies)
                    {
                        if (!stack.Contains(newly))
                        {
                            stack.Enqueue(newly);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        });
    }
}

watch.Stop();

Console.WriteLine("\n\n-----------------------------------------------");
Console.WriteLine($"Totally Exports: {allExports.Count:0}");
Console.WriteLine($"Totally SubExports: {totalSubs:0}");
Console.WriteLine($"Totally Writes: {TOTAL_SIZE / 1024 / 1024:0}MB");
Console.WriteLine($"Initialize cost: {initTime:0}ms");
Console.WriteLine($"Prepare cost: {prepareTime:0}ms");
Console.WriteLine($"Export cost: {watch.Elapsed.TotalSeconds * 1000.0:0}ms");

Console.WriteLine("All Success!");

