[Setup]
AppName=Bulk PDF Signer
AppVersion=2025.07.15
DefaultDirName={userappdata}\BulkPdfSigner
DefaultGroupName=BulkPdfSigner
OutputDir=installer
OutputBaseFilename=BulkPdfSignerInstaller
Compression=lzma
SolidCompression=yes
SetupIconFile=logo.ico
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Files]
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "logo_v.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Bulk PDF Signer"; Filename: "{app}\BulkPdfSigner.exe"; IconFilename: "{app}\logo_v.ico"
Name: "{group}\Uninstall Bulk PDF Signer"; Filename: "{uninstallexe}"; IconFilename: "{app}\logo_v.ico"
Name: "{userdesktop}\Bulk PDF Signer"; Filename: "{app}\BulkPdfSigner.exe"; Tasks: desktopicon; IconFilename: "{app}\logo_v.ico"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
