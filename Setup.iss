#define MyAppName "KSO Download Turbo Ultra"
#define MyAppVersion "1.0 PRO"
#define MyAppPublisher "KSO - Abdullah & Abdelrahman Hany"

[Setup]
AppId={{KSO-Download-Turbo-Ultra-2026}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://kso.app
DefaultDirName={autopf}\KSO
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=KSO_Download_Turbo_Ultra_Setup_v1.0_PRO_x64
SetupIconFile=Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
LanguageDetectionMethod=none

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء أيقونة على سطح المكتب"; GroupDescription: "أيقونات إضافية:"; Flags: unchecked
Name: "addtopath"; Description: "إضافة KSO إلى PATH"; GroupDescription: "إعدادات متقدمة:"; Flags: unchecked

[Files]
; 1. بنسحب كل حاجة من فولدر publish اللي البيلد عمله
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 2. الايقونة لازم تكون في Resources
Source: "Resources\app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\KSO.exe"; IconFilename: "{app}\app.ico"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\KSO.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon
Name: "{group}\إلغاء تثبيت KSO"; Filename: "{uninstallexe}"; IconFilename: "{app}\app.ico"

[Run]
Filename: "{app}\KSO.exe"; Description: "تشغيل KSO الآن"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\KSO"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "Software\KSO"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

; اضافة للـ PATH لو علم على المهمة
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\KSO" ; يحذف الاعدادات والهيستوري مع الانستول

[Code]
procedure InitializeWizard();
begin
  WizardForm.Caption := 'معالج تثبيت KSO Download Turbo Ultra V1 PRO';
  WizardForm.WelcomeLabel2.Caption := 'سيتم تثبيت KSO Download Turbo Ultra على جهازك.'#13#10'يدعم التحميل بـ 1,000,000 خيط والضغط التلقائي وتغيير اللغة من داخل البرنامج.';
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
begin
  if CurStep = usPostUninstall then
    MsgBox('تم حذف KSO Download Turbo Ultra بنجاح.'#13#10'تم حذف جميع الاعدادات من AppData', mbInformation, MB_OK);
end;