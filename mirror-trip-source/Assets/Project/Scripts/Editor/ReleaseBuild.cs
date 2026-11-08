using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace MirrorTrip.Editor
{
 public static class ReleaseBuild
 {
  public static void Web()=>Run(BuildTarget.WebGL);
  public static void Windows()=>Run(BuildTarget.StandaloneWindows64);
  static void Run(BuildTarget target)
  {
   string root=Environment.GetEnvironmentVariable("MIRROR_RELEASE_OUTPUT");
   if(string.IsNullOrEmpty(root))throw new Exception("MIRROR_RELEASE_OUTPUT required");
   try{
    PlayerSettings.companyName="Rong Shen (Singlong404)";PlayerSettings.productName="Mirror Trip";PlayerSettings.bundleVersion="1.0.0";
    PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
    PlayerSettings.defaultWebScreenWidth=1920;PlayerSettings.defaultWebScreenHeight=1080;
    PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
    PlayerSettings.runInBackground=false;
    var platform=target==BuildTarget.WebGL?NamedBuildTarget.WebGL:NamedBuildTarget.Standalone;
    bool windowsMono=target==BuildTarget.StandaloneWindows64&&Environment.GetEnvironmentVariable("MIRROR_WINDOWS_MONO")=="1";
    PlayerSettings.SetScriptingBackend(platform,windowsMono?ScriptingImplementation.Mono2x:ScriptingImplementation.IL2CPP);
    PlayerSettings.SetManagedStrippingLevel(platform,ManagedStrippingLevel.Low);
    PlayerSettings.SetIl2CppCompilerConfiguration(platform,Il2CppCompilerConfiguration.Release);
    PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
    PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
    PlayerSettings.WebGL.nameFilesAsHashes=true;PlayerSettings.WebGL.initialMemorySize=256;
    PlayerSettings.WebGL.maximumMemorySize=2048;PlayerSettings.WebGL.dataCaching=true;
    EditorUserBuildSettings.development=false;EditorUserBuildSettings.allowDebugging=false;EditorUserBuildSettings.connectProfiler=false;
    AssetDatabase.SaveAssets();
    string path=target==BuildTarget.WebGL?Path.Combine(root,"Web"):Path.Combine(root,"Windows","MirrorTrip.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Project/Scenes/Levels/Level_01.unity"},locationPathName=path,target=target,options=BuildOptions.CompressWithLz4HC});
    File.WriteAllText(Path.Combine(root,"Build_"+target+".txt"),report.summary.result+"\nBytes="+report.summary.totalSize+"\nErrors="+report.summary.totalErrors+"\nDuration="+report.summary.totalTime);
    if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Release build failed: "+report.summary.result);
    EditorApplication.Exit(0);
   }catch(Exception e){Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"Build_"+target+"_ERROR.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
