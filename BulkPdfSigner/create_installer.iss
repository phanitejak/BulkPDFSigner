[Setup]
AppName=Bulk PDF Signer
AppVersion=1.0
DefaultDirName={pf}\BulkPdfSigner
DefaultGroupName=BulkPdfSigner
OutputDir=.
OutputBaseFilename=BulkPdfSignerInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Bulk PDF Signer"; Filename: "{app}\BulkPdfSigner.exe"
Name: "{group}\Uninstall Bulk PDF Signer"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Bulk PDF Signer"; Filename: "{app}\BulkPdfSigner.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
