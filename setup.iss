; dsh-float 0.7.32 beta - 安装脚本
; Inno Setup 6.7.1+
; C# + WebView2 版

#define MyAppName "dsh-float"
#define MyAppVersion "0.7.32"
#define MyAppPublisher "dsh-float"
#define MyAppExeName "dsh-float.exe"
#define MyAppURL "https://api.lwx3dp.de5.net/api/dsh"

; ============ 源文件目录 ============
#define BuildDir "build"

; ============ 依赖下载地址 ============
#define NodeMsiUrl "https://nodejs.org/dist/v24.21.0/node-v24.21.0-x64.msi"
#define FfmpegZipUrl "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"

[Setup]
AppId={{B8F3A2E1-7C4D-4A8B-9E2F-1D5C6A3B7E9F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=dsh-float-{#MyAppVersion}-setup
SetupIconFile={#BuildDir}\favicon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dark includetitlebar
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0
VersionInfoVersion=0.7.32.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=0.7.32.0

[Languages]
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
; ★ 已删除 autostart 选项，改为默认写入注册表
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务："; Flags: checkedonce
Name: "installdeps"; Description: "自动安装缺失的依赖（Node.js v24.21.0 / FFmpeg），不再弹窗询问"; GroupDescription: "依赖安装："; Flags: checkedonce

[Files]
Source: "{#BuildDir}\dsh-float.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\splash.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\injected.js"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\dsh_wait.html"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\dsh_busy.html"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Registry]
; ★ 开机自启：安装时无条件写入（不再依赖 Tasks）
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "dsh-float"; \
    ValueData: """{app}\{#MyAppExeName}"" --update-checker"; \
    Flags: uninsdeletevalue

[UninstallDelete]
Type: files; Name: "{app}\config.json"
Type: files; Name: "{app}\dsh_float.log"
Type: filesandordirs; Name: "{app}\dsh-float.exe.WebView2"

[Code]
// ============================================================
//  全局变量
// ============================================================
var
  DownloadPage: TDownloadWizardPage;
  NeedNode: Boolean;
  NeedFfmpeg: Boolean;
  NodeInstalled: Boolean;
  FfmpegInstalled: Boolean;
  DepsPrompted: Boolean;

// ============================================================
//  辅助：在 PATH 字符串里查找某个可执行文件
// ============================================================
function FindInPathString(const PathStr, ExeName: String): Boolean;
var
  I: Integer;
  Cur: String;
  Expanded: String;
begin
  Result := False;
  Cur := '';

  for I := 1 to Length(PathStr) do
  begin
    if PathStr[I] = ';' then
    begin
      if Cur <> '' then
      begin
        Expanded := ExpandConstant(Cur);
        StringChangeEx(Expanded, '%SystemRoot%',
          ExpandConstant('{win}'), True);
        StringChangeEx(Expanded, '%windir%',
          ExpandConstant('{win}'), True);
        StringChangeEx(Expanded, '%ProgramFiles%',
          ExpandConstant('{pf}'), True);
        StringChangeEx(Expanded, '%ProgramFiles(x86)%',
          ExpandConstant('{pf32}'), True);
        StringChangeEx(Expanded, '%LOCALAPPDATA%',
          ExpandConstant('{localappdata}'), True);
        StringChangeEx(Expanded, '%APPDATA%',
          ExpandConstant('{userappdata}'), True);

        if FileExists(AddBackslash(Expanded) + ExeName) then
        begin
          Result := True;
          Exit;
        end;
      end;
      Cur := '';
    end
    else
      Cur := Cur + PathStr[I];
  end;

  if Cur <> '' then
  begin
    Expanded := ExpandConstant(Cur);
    StringChangeEx(Expanded, '%SystemRoot%',
      ExpandConstant('{win}'), True);
    StringChangeEx(Expanded, '%windir%',
      ExpandConstant('{win}'), True);
    StringChangeEx(Expanded, '%ProgramFiles%',
      ExpandConstant('{pf}'), True);
    StringChangeEx(Expanded, '%ProgramFiles(x86)%',
      ExpandConstant('{pf32}'), True);
    StringChangeEx(Expanded, '%LOCALAPPDATA%',
      ExpandConstant('{localappdata}'), True);
    StringChangeEx(Expanded, '%APPDATA%',
      ExpandConstant('{userappdata}'), True);

    if FileExists(AddBackslash(Expanded) + ExeName) then
      Result := True;
  end;
end;

// ============================================================
//  辅助：从注册表里读 PATH（系统 + 用户）并查找可执行文件
// ============================================================
function FindExeInRegistryPath(const ExeName: String): Boolean;
var
  SysPath, UserPath: String;
begin
  Result := False;

  if RegQueryStringValue(HKLM64,
       'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
       'Path', SysPath) then
  begin
    if FindInPathString(SysPath, ExeName) then
    begin
      Result := True;
      Exit;
    end;
  end;

  SysPath := '';
  if RegQueryStringValue(HKLM32,
       'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
       'Path', SysPath) then
  begin
    if FindInPathString(SysPath, ExeName) then
    begin
      Result := True;
      Exit;
    end;
  end;

  if RegQueryStringValue(HKCU, 'Environment', 'Path', UserPath) then
  begin
    if FindInPathString(UserPath, ExeName) then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

// ============================================================
//  检测 Node.js 是否已安装
// ============================================================
function IsNodeInstalled: Boolean;
var
  Version: String;
begin
  Result := False;

  if RegQueryStringValue(HKLM64, 'SOFTWARE\Node.js',
       'Version', Version) then
  begin Result := True; Exit; end;

  if RegQueryStringValue(HKLM32, 'SOFTWARE\Node.js',
       'Version', Version) then
  begin Result := True; Exit; end;

  if RegQueryStringValue(HKCU, 'SOFTWARE\Node.js',
       'Version', Version) then
  begin Result := True; Exit; end;

  if FileExists(ExpandConstant('{pf}\nodejs\node.exe')) then
  begin Result := True; Exit; end;

  if FileExists(ExpandConstant('{pf32}\nodejs\node.exe')) then
  begin Result := True; Exit; end;

  if FileExists(ExpandConstant('{localappdata}\Programs\nodejs\node.exe')) then
  begin Result := True; Exit; end;

  if FileExists(ExpandConstant('{userappdata}\nvm\node.exe')) then
  begin Result := True; Exit; end;

  if FileExists('C:\nodejs\node.exe') then
  begin Result := True; Exit; end;

  if FindExeInRegistryPath('node.exe') then
  begin Result := True; Exit; end;
end;

// ============================================================
//  检测 FFmpeg 是否已安装
// ============================================================
function IsFfmpegInstalled: Boolean;
begin
  Result := False;

  if FileExists(ExpandConstant('{app}\ffmpeg.exe')) then
  begin Result := True; Exit; end;

  if FindExeInRegistryPath('ffmpeg.exe') then
  begin Result := True; Exit; end;
end;

// ============================================================
//  将指定目录追加到当前用户的 PATH（不覆盖原有值）
// ============================================================
procedure AddAppDirToUserPath(const Dir: String);
var
  PathEnv: String;
begin
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', PathEnv) then
    PathEnv := '';

  if Pos(LowerCase(Dir), LowerCase(PathEnv)) > 0 then
    Exit;

  if (PathEnv <> '') and (PathEnv[Length(PathEnv)] <> ';') then
    PathEnv := PathEnv + ';';
  PathEnv := PathEnv + Dir;

  RegWriteStringValue(HKCU, 'Environment', 'Path', PathEnv);
end;

// ============================================================
//  解析 FFmpeg 便携版压缩包，解压 ffmpeg.exe 到 {app}
// ============================================================
procedure InstallFfmpegFromZip;
var
  ZipFile: String;
  AppDir: String;
  FindRec: TFindRec;
  BinDir: String;
  ResultCode: Integer;
begin
  ZipFile := ExpandConstant('{tmp}\ffmpeg.zip');
  AppDir := ExpandConstant('{app}');

  if not FileExists(ZipFile) then
  begin
    MsgBox('FFmpeg 压缩包未找到，请手动下载 ffmpeg.exe 放到程序目录。' + #13#10 +
           '下载地址：{#FfmpegZipUrl}',
           mbError, MB_OK);
    Exit;
  end;

  try
    ForceDirectories(ExpandConstant('{tmp}\ffmpeg_extract'));

    if not ShellExec('open',
         'powershell.exe',
         '-NoProfile -Command "Expand-Archive -Path ''' + ZipFile +
         ''' -DestinationPath ''' + ExpandConstant('{tmp}\ffmpeg_extract') +
         ''' -Force"',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('解压 FFmpeg 失败，请手动下载 ffmpeg.exe 放到程序目录。',
             mbError, MB_OK);
      Exit;
    end;

    BinDir := '';
    if FindFirst(ExpandConstant('{tmp}\ffmpeg_extract\*'), FindRec) then
    begin
      try
        repeat
          if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
          begin
            if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
            begin
              if FileExists(ExpandConstant('{tmp}\ffmpeg_extract\') +
                 FindRec.Name + '\bin\ffmpeg.exe') then
              begin
                BinDir := ExpandConstant('{tmp}\ffmpeg_extract\') +
                          FindRec.Name + '\bin';
                Break;
              end;
            end;
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;

    if BinDir = '' then
    begin
      if FileExists(ExpandConstant('{tmp}\ffmpeg_extract\ffmpeg.exe')) then
        BinDir := ExpandConstant('{tmp}\ffmpeg_extract');
    end;

    if BinDir <> '' then
    begin
      FileCopy(BinDir + '\ffmpeg.exe', AppDir + '\ffmpeg.exe', False);
      if FileExists(BinDir + '\ffprobe.exe') then
        FileCopy(BinDir + '\ffprobe.exe', AppDir + '\ffprobe.exe', False);

      AddAppDirToUserPath(AppDir);

      MsgBox('FFmpeg 已解压到程序目录：' + #13#10 + AppDir + #13#10 + #13#10 +
             '已添加到用户 PATH，重启终端后生效。',
             mbInformation, MB_OK);
    end
    else
    begin
      MsgBox('在压缩包中未找到 ffmpeg.exe，请手动下载。',
             mbError, MB_OK);
    end;

  except
    MsgBox('处理 FFmpeg 时出错，请手动下载 ffmpeg.exe 放到程序目录。',
           mbError, MB_OK);
  end;
end;

// ============================================================
//  安装 Node.js（静默 MSI）
// ============================================================
function InstallNodeFromMsi: Boolean;
var
  ResultCode: Integer;
  MsiFile: String;
begin
  Result := False;
  MsiFile := ExpandConstant('{tmp}\node_setup.msi');

  if not FileExists(MsiFile) then
  begin
    MsgBox('Node.js 安装包未找到，请手动安装：' + #13#10 +
           'https://nodejs.org/dist/v24.21.0/',
           mbError, MB_OK);
    Exit;
  end;

  if Exec('msiexec.exe',
       '/i "' + MsiFile + '" /qn /norestart ALLUSERS=1 ADDLOCAL=ALL',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      Result := True;
      MsgBox('Node.js v24.21.0 安装完成！' + #13#10 +
             '打开新的命令行窗口，输入 node --version 应显示 v24.21.0；' + #13#10 +
             '输入 npx --version 应显示 11.19.0。',
             mbInformation, MB_OK);
    end
    else
      MsgBox('Node.js 安装返回代码：' + IntToStr(ResultCode) + #13#10 +
             '请手动安装：https://nodejs.org/dist/v24.21.0/',
             mbError, MB_OK);
  end
  else
    MsgBox('无法启动 Node.js 安装程序，请手动安装。',
           mbError, MB_OK);
end;

// ============================================================
//  下载进度回调
// ============================================================
function OnDownloadProgress(const Url, FileName: String;
  const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('下载完成: %s', [FileName]));
  Result := True;
end;

// ============================================================
//  下载并安装依赖
// ============================================================
procedure DownloadAndInstallDeps;
begin
  DownloadPage.Clear;

  if NeedNode then
    DownloadPage.Add('{#NodeMsiUrl}', 'node_setup.msi', '');

  if NeedFfmpeg then
    DownloadPage.Add('{#FfmpegZipUrl}', 'ffmpeg.zip', '');

  if (not NeedNode) and (not NeedFfmpeg) then
    Exit;

  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
    except
      SuppressibleMsgBox('依赖下载失败：' + GetExceptionMessage + #13#10 +
        '你可以跳过，稍后手动安装。',
        mbCriticalError, MB_OK, IDOK);
    end;
  finally
    DownloadPage.Hide;
  end;

  if NeedNode then
    InstallNodeFromMsi;

  if NeedFfmpeg then
    InstallFfmpegFromZip;
end;

// ============================================================
//  初始化安装程序
// ============================================================
function InitializeSetup: Boolean;
begin
  Result := True;
  DepsPrompted := False;
  NeedNode := False;
  NeedFfmpeg := False;
  NodeInstalled := False;
  FfmpegInstalled := False;
end;

// ============================================================
//  向导初始化
// ============================================================
procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(
    SetupMessage(msgWizardPreparing),
    SetupMessage(msgPreparingDesc),
    @OnDownloadProgress);

  DownloadPage.Caption := '正在下载依赖';
  DownloadPage.Description := '正在从官方网站下载 Node.js v24.21.0 / FFmpeg...';
end;

// ============================================================
//  下一步按钮点击
// ============================================================
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if (CurPageID = wpSelectTasks) and (not DepsPrompted) then
  begin
    DepsPrompted := True;

    NodeInstalled := IsNodeInstalled;
    FfmpegInstalled := IsFfmpegInstalled;

    NeedNode := not NodeInstalled;
    NeedFfmpeg := not FfmpegInstalled;

    if WizardIsTaskSelected('installdeps') then
    begin
      // 自动安装，不弹窗
    end
    else
    begin
      if NeedNode then
      begin
        if MsgBox('未检测到 Node.js。' + #13#10 + #13#10 +
                  'Node.js 用于"任务"模式（本地 DSH 服务）。' + #13#10 +
                  '将自动下载安装 Node.js v24.21.0（自带 npm/npx 11.19.0）。' + #13#10 + #13#10 +
                  '是否现在自动下载并安装？' + #13#10 +
                  '（约 30 MB，从 nodejs.org 官方下载）',
                  mbConfirmation, MB_YESNO) = IDYES then
        begin
          NeedNode := True;
        end
        else
        begin
          NeedNode := False;
          MsgBox('已跳过 Node.js 安装。' + #13#10 +
                 '你可以稍后手动安装：https://nodejs.org/dist/v24.21.0/',
                 mbInformation, MB_OK);
        end;
      end;

      if NeedFfmpeg then
      begin
        if MsgBox('未检测到 FFmpeg。' + #13#10 + #13#10 +
                  'FFmpeg 用于视频帧合成 mp4。' + #13#10 +
                  '是否现在自动下载并安装 FFmpeg？' + #13#10 + #13#10 +
                  '（约 80 MB，从 gyan.dev 下载便携版）',
                  mbConfirmation, MB_YESNO) = IDYES then
        begin
          NeedFfmpeg := True;
        end
        else
        begin
          NeedFfmpeg := False;
          MsgBox('已跳过 FFmpeg 安装。' + #13#10 +
                 '你可以稍后手动下载 ffmpeg.exe 放到程序目录。',
                 mbInformation, MB_OK);
        end;
      end;
    end;
  end;

  if CurPageID = wpReady then
  begin
    DownloadAndInstallDeps;
  end;
end;

// ============================================================
//  安装完成提示
// ============================================================
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox(
      '安装完成！' + #13#10 +
      '' + #13#10 +
      '【使用说明】' + #13#10 +
      '  · 双击桌面或开始菜单里的 dsh-float 启动主程序。' + #13#10 +
      '  · 程序启动后右下角会出现悬浮栏，可拖动到屏幕任意位置。' + #13#10 +
      '  · 🐋 聊天、💻 任务（打开 DSH 服务）、🪟 小窗、🛠 工具菜单。' + #13#10 +
      '  · 点 🛠 → 设置 可切换工具栏模式（悬浮栏 / 网页注入）。' + #13#10 +
      '' + #13#10 +
      '【关于"任务"按钮】' + #13#10 +
      '  任务模式会启动本地 DSH 服务（需要 Node.js）。' + #13#10 +
      '  首次使用会在后台自动下载依赖，可能要等 1~3 分钟。' + #13#10 +
      '  如果不需要此功能，忽略"任务"按钮即可。' + #13#10 +
      '' + #13#10 +
      '【依赖安装说明】' + #13#10 +
      '  · 若已安装 Node.js，安装程序会自动识别并跳过下载。' + #13#10 +
      '  · FFmpeg 若不存在，会解压到程序目录并添加到用户 PATH。' + #13#10 +
      '  · 如遇问题，可在设置中手动指定 ffmpeg.exe 路径。' + #13#10 +
      '' + #13#10 +
      '【开机自启说明】' + #13#10 +
      '  · 已自动写入注册表，开机后台检查更新（不弹主窗口）。' + #13#10 +
      '  · 注册表位置：HKCU\Software\Microsoft\Windows\CurrentVersion\Run' + #13#10 +
      '  · 命令：dsh-float.exe --update-checker' + #13#10 +
      '  · 检查周期 3 小时，失败 30 秒重试。' + #13#10 +
      '  · 卸载时会自动移除该注册表项。' + #13#10 +
      '' + #13#10 +
      '祝使用愉快！',
      mbInformation, MB_OK);
  end;
end;

// ============================================================
//  卸载前先杀掉进程
// ============================================================
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec('taskkill.exe', '/F /IM dsh-float.exe', '',
        SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(500);
  end;
end;