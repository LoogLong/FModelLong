using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.InfinityNikki.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.UEFormat.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static System.Net.Mime.MediaTypeNames;


// Create a timer
var watch = System.Diagnostics.Stopwatch.StartNew();
watch.Start();

Console.WriteLine("FModel Console Exporter");

if (args.Length < 5)
{
    Console.WriteLine("usage: FModelConsole.exe [PAKPATH] [EXPORTPATH] [AESKEY] [CONTENTPATH] [TYPE]");
    Console.WriteLine("eg: FModelConsole.exe D:\\FinalFantasyVIIRemake\\End\\Content\\Paks D:\\FF7RExport 0xABCD End/Content/Environment StaticMesh");
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

var PathMatch = new List<string>();

var aesKeys = new Dictionary<FGuid, FAesKey>();
if (false)
{
    GAME_ENUM = "GAME_InfinityNikki";
    ARCHIVE_DIRECTORY_HERE = "C:\\Program Files\\InfinityNikki Launcher\\InfinityNikki";
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

    PACKAGE_PATH_HERE = "X6Game/Content/Assets";
    //PACKAGE_PATH_HERE = "X6Game/Content/Assets/Buildin/Suits";
    MAPPING_FILE = "D:\\Output\\.data\\REL_5.4.4-0+UE5-X6Game.usmap";
    EXPORT_PATH = new DirectoryInfo("D:\\Output");
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
    ARCHIVE_DIRECTORY_HERE = "C:\\Users\\Administrator\\Documents\\WutheringWaves\\Wuthering Waves Game\\Client";

    var aesKey = new FAesKey("0x6F80948821CA338739A24D4D9F778BCAC0996B2EF2A73897A789C68AFF05174E");
    var guid = new FGuid();
    aesKeys.Add(guid, aesKey);

    aesKey = new FAesKey("0x1C2F579DFE9A44E04BE3C9E96AF7014F338B215715B5ABA837EE9B5CF3439568");
    guid = new FGuid("EF39C7CE77E646C1BC50C4659CC65ABC\n");
    aesKeys.Add(guid, aesKey);

    aesKey = new FAesKey("0x803C07CA5F56EE98EE08D30D32FC7B9608DADD822DD80853CC5957605B889306");
    guid = new FGuid("5034E73931CB45D68C665F7283A07022\n");
    aesKeys.Add(guid, aesKey);

    aesKey = new FAesKey("0x803C07CA5F56EE98EE08D30D32FC7B9608DADD822DD80853CC5957605B889306");
    guid = new FGuid("AF76C7EEFD034A91B5A03567A081EFBE\n");

    aesKey = new FAesKey("0x29FE48B79F91C3CED6583BA717061879A253687456369085751721B209F371A8");
    guid = new FGuid("F24816F76C7046EAAEDA774AB268F32B\n");

    aesKey = new FAesKey("0x4D794471F2970F064CE4172B7A3C8DCFB7D60309031DFEB45E44C85F31315691");
    guid = new FGuid("E95505DC5125469F86587A3EF4041243\n");

    aesKeys.Add(guid, aesKey);
     //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/Chun";
    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM";
    //PACKAGE_PATH_HERE = "Client/Content/Aki/Sequence";
    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleMS/Katixiya";
    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/Maxiaofang/R2T1MaxiaofangMd10011/ABP_Maxiaofang";
    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/BaseAnim2";
     PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleM/Lupa/R2T1LupaMd10011";
    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleS/Luokeke";
    //PathMatch.Add(PACKAGE_PATH_HERE);

    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Role/FemaleMS/Kmola";
    //PathMatch.Add(PACKAGE_PATH_HERE);

    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Monster/Boss/MB1HecateMd00601";
    //PathMatch.Add(PACKAGE_PATH_HERE);

    //PACKAGE_PATH_HERE = "Client/Content/Aki/Character/Monster/Boss/MB1HecateMd00611";
    //PathMatch.Add(PACKAGE_PATH_HERE);

    MAPPING_FILE = "";
    EXPORT_PATH = new DirectoryInfo("D:\\Output");
}
if (false)
{
    GAME_ENUM = "GAME_NevernessToEverness";
    ARCHIVE_DIRECTORY_HERE = "D:\\Neverness To Everness\\Client\\WindowsNoEditor\\HT\\Content\\Paks";
    var aesKey = new FAesKey("0xD6ACCF815BE273F2CB6EF1CD3BF4914F05EED157565A13EA61AA7D59588A10FC");
    var guid = new FGuid();
    aesKeys.Add(guid, aesKey);

    PACKAGE_PATH_HERE = "HT/Content/Characters/Player/010_nanally";

    MAPPING_FILE = "D:/Neverness To Everness/5.5.4-0+UE5-HT.usmap";
    EXPORT_PATH = new DirectoryInfo("D:\\NevernessToEvernessOutput");
}
if (false)
{
    GAME_ENUM = "GAME_UE5_1";
    ARCHIVE_DIRECTORY_HERE = "D:\\ZXSJclient\\ZXSJ\\Game\\ZhuxianClient\\Content\\Paks";
    var aesKey = new FAesKey("0xD7D19A4349AAA02C53CD9282D0E3B5B8BEE592829DD2DF729EBD0D377E4CC47D");
    var guid = new FGuid();
    aesKeys.Add(guid, aesKey);

    //PACKAGE_PATH_HERE = "ZhuxianClient/Content/Characters/Players/Chengnan/Mesh";
    PACKAGE_PATH_HERE = "ZhuxianClient/Content/Characters/Players/Chengnan/Animation/Fenxiang";

    MAPPING_FILE = "";
    EXPORT_PATH = new DirectoryInfo("D:\\Output_zxsj");
}

if (true)
{
    GAME_ENUM = "GAME_FinalFantasy7Rebirth";
    ARCHIVE_DIRECTORY_HERE = "D:\\FINAL FANTASY VII REBIRTH\\End\\Content\\Paks";
    var aesKey = new FAesKey("0x0000000000000000000000000000000000000000000000000000000000000000");
    var guid = new FGuid();
    aesKeys.Add(guid, aesKey);

    //PACKAGE_PATH_HERE = "End/Content/Character/Player/PC0000_00_Cloud_Standard/Model";
    PACKAGE_PATH_HERE = "End/Content/Motion/Player/PC0000_Cloud";
    PACKAGE_PATH_HERE = "End/Content/Motion/Summon/SU0001_Chocobo";

    MAPPING_FILE = "";
    EXPORT_PATH = new DirectoryInfo("D:\\FF7R_Output");
}

var EXPORT_OPTIONS = new ExporterOptions
{
    LodFormat = ELodFormat.AllLods,
    MeshFormat = EMeshFormat.UEFormat,
    AnimFormat = EAnimFormat.UEFormat,
    MaterialFormat = EMaterialFormat.AllHierarchy,
    TextureFormat = ETextureFormat.Png,
    SocketFormat = ESocketFormat.None,
    Platform = ETexturePlatform.DesktopMobile,
    ExportMorphTargets = true,
    ExportMaterials = true,
    CompressionFormat = EFileCompressionFormat.None
};

EGame game = Enum.Parse<EGame>(GAME_ENUM);

//Client/Content/Aki/Map/AkiWorld_WP/_Generated_/WPRT_AkiWorld_WP_Grid_AudioFar_Cell_L0_X-10_Y17_Z0_DL0_0.umap
var versionContainer = new VersionContainer(
            game: game, platform: ETexturePlatform.DesktopMobile,
            customVersions: new FCustomVersionContainer(null),
            optionOverrides: null,
            mapStructTypesOverrides: null);
var pathComparer = StringComparer.OrdinalIgnoreCase;
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

    var zlibPath = Path.Combine(OutputDirectory, ".data", ZlibHelper.DLL_NAME);
    if (!File.Exists(zlibPath))
    {
        await ZlibHelper.DownloadDllAsync(zlibPath);
    }

    ZlibHelper.Initialize(zlibPath);

    var detexPath = Path.Combine(OutputDirectory, ".data", DetexHelper.DLL_NAME);
    if (File.Exists(DetexHelper.DLL_NAME))
    {
        File.Move(DetexHelper.DLL_NAME, detexPath, true);
    }
    else if (!File.Exists(detexPath))
    {
        await DetexHelper.LoadDllAsync(detexPath);
    }

    DetexHelper.Initialize(detexPath);
}

var provider = new DefaultFileProvider(ARCHIVE_DIRECTORY_HERE, SearchOption.AllDirectories, versionContainer, pathComparer);
provider.ReadScriptData = false;
provider.ReadShaderMaps = false;

provider.CustomEncryption = provider.Versions.Game switch
{
    EGame.GAME_InfinityNikki => InfinityNikkiAes.InfinityNikkiDecrypt,
    _ => provider.CustomEncryption
};
provider.UnloadNonStreamedVfs();
provider.Initialize(); // will scan the archive directory for supported file extensions
provider.MappingsContainer = null;
if (MAPPING_FILE != "" && MAPPING_FILE != "None")
{
    provider.MappingsContainer = new FileUsmapTypeMappingsProvider(MAPPING_FILE);
}

provider.SubmitKeys(aesKeys);
provider.PostMount();
var count = provider.LoadVirtualPaths(game.GetVersion());

watch.Stop();
var initTime = watch.Elapsed.TotalSeconds * 1000.0;
watch.Reset();

watch.Start();

List<GameFile> allValidateFile = new();
var totalSubs = 0;
foreach (var file in provider.Files)
{
    if ((file.Value.Extension == "umap" || file.Value.Extension == "uasset") && file.Key.StartsWith(PACKAGE_PATH_HERE, StringComparison.OrdinalIgnoreCase))
    {
        allValidateFile.Add(file.Value);
    }
}

List<GameFile> SkeletalMeshFile = new();
foreach (var file in provider.Files)
{
    if ((file.Value.Extension == "umap" || file.Value.Extension == "uasset") && file.Key.StartsWith("End/Content/Character/Summon/SU0001_00_Chocobo_Standard/Model", StringComparison.OrdinalIgnoreCase))
    {
        SkeletalMeshFile.Add(file.Value);
        break;
    }
}
USkeletalMesh baseMesh = null;
USkeleton baseMeshSkeleton = null;
foreach (var file in SkeletalMeshFile)
{
    var package = provider.LoadPackage(file);
    var allObjects = package.GetExports();
    foreach (var export in allObjects)
    {
        if (export.ExportType == "SkeletalMesh")
        {
            var exportMesh = export as USkeletalMesh;
            if (exportMesh != null)
            {
                var skeleton = exportMesh.Skeleton.Load<USkeleton>();
                if (skeleton != null)
                {
                    baseMesh = exportMesh;
                    baseMeshSkeleton = skeleton;
                }
            }
        }
    }
}
if (baseMesh == null || baseMeshSkeleton == null)
{
    // exit program
    System.Environment.Exit(1);
}
Console.WriteLine("-----------------------------------------------");
Console.WriteLine($"Exporting: {PACKAGE_PATH_HERE}:{OBJECT_TYPE} -> {EXPORT_PATH}");
Console.WriteLine($"Filtered Files: {allValidateFile.Count:0}");

List<UObject> allExports = new();

var filecount = allValidateFile.Count;
var fileindex = 0;

var bExportSkeletalMesh = false;
// var bExportStaticMesh = false;true
var bExportAnimSequence = true;
var bExportAnimMontage = false;
Dictionary<string, string> ClassNames = new Dictionary<string, string>();
foreach (var file in allValidateFile)
{
    fileindex++;
    Console.WriteLine($"{fileindex}/{filecount}");

    var package = provider.LoadPackage(file);
    var allObjects = package.GetExports();
    // {GAME}/Content/Folder1/Folder2/PackageName.uasset
    foreach (var export in allObjects)
    {
        if (bExportSkeletalMesh)
        {
            if (export.ExportType == "SkeletalMesh")
            {
                var exportMesh = export as USkeletalMesh;
                if (exportMesh != null)
                {
                    var skeleton = exportMesh.Skeleton.Load<USkeleton>();
                    if (skeleton != null)
                    {
                        allExports.Add(export);
                    }
                }
            }
        }

        if (bExportAnimSequence)
        {
            if (export.ExportType == "AnimSequence")
            {
                var exportAnim = export as UAnimSequence;
                if (exportAnim != null)
                {
                    var skeleton = exportAnim.Skeleton.Load<USkeleton>();
                    if (skeleton.Guid == baseMeshSkeleton.Guid)
                    {
                        allExports.Add(export);
                    }
                }
            }
        }

        if (bExportAnimMontage)
        {
            if (export.ExportType == "AnimMontage")
            {
                var exportAnim = export as UAnimMontage;
                if (exportAnim != null)
                {
                    var skeleton = exportAnim.Skeleton.Load<USkeleton>();
                    if (skeleton != null)
                    {
                        allExports.Add(export);
                    }

                }
            }
        }
    }
}


watch.Stop();
var prepareTime = watch.Elapsed.TotalSeconds * 1000.0;
watch.Reset();
watch.Start();


var cpuThreads = Environment.ProcessorCount;
#if DEBUG
cpuThreads = 1;
#endif
ConcurrentQueue<UObject> stack = new();
stack.Enqueue(baseMesh);
foreach (var export in allExports)
{
    stack.Enqueue(export);
}

var COUNTER = 0;
ulong TOTAL_SIZE = 0;

using (var progress = new ProgressBar())
{
    List<string> guids = new();
    while (stack.Count > 0)
    {
        ConcurrentQueue<UObject> stackCopy = new ConcurrentQueue<UObject>();
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
                            progress.ReportExt((double) COUNTER / (double) totalSubs, Path.GetFileName(savedFilePath1));
                            if (File.Exists(savedFilePath1))
                            {
                                var fs = new FileInfo(savedFilePath1);
                                Interlocked.Add(ref TOTAL_SIZE, (ulong) fs.Length);
                            }
                        }
                    }
                    foreach (var newly in threadDependencies)
                    {
                        stack.Enqueue(newly);
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
