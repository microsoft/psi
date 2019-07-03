# First check if we already have installed Doxygen on this machine
try {
    # check if doxygen is in standard location
    $found = $FALSE;
    if (Test-Path env:ProgramW6432) {
        $stddoxygenExe = $env:ProgramW6432 + "\doxygen\bin\doxygen.exe";
        if (Test-Path $stddoxygenExe) {
            $doxygenInstallDir = $env:ProgramW6432 + "\doxygen\bin";
            $doxygenExe = $doxygenInstallDir + "\doxygen.exe"
            $found = $TRUE;
        }
    }
    if (-not $found) {
        $doxygenInstallDir = $env:AGENT_BUILDDIRECTORY
        if (-not (Test-Path env:AGENT_BUILDDIRECTORY)) {
            if (-not (Test-Path env:InstallDoxygenDir)) {
                $doxygenInstallDir = $env:USERPROFILE + "\Documents";
            }
            else {
                $doxygenInstallDir = $env:InstallDoxygenDir;
            }
        }
        $doxygenInstallDir = $doxygenInstallDir + "\doxygen\bin"
        $doxygenExe = $doxygenInstallDir + "\doxygen.exe"
        $doxygenZip = $doxygenInstallDir + "\doxygen.zip"
        $doxygenExists = Test-Path $doxygenExe
        if ($doxygenExists -eq $False) {
            New-Item -Force -ItemType directory -Path $doxygenInstallDir
            Write-Host "Installing Doxygen to " $doxygenInstallDir
            $WebClient = New-Object System.Net.WebClient
            Write-Host "Downloading Doxygen to '" + $doxygenZip + "'"
            $WebClient.DownloadFile("http://doxygen.nl/files/doxygen-1.8.15.windows.x64.bin.zip", $doxygenZip)
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            Write-Host "Unzipping Doxygen to '" + $doxygenInstallDir + "'"
            [System.IO.Compression.ZipFile]::ExtractToDirectory($doxygenZip, $doxygenInstallDir);
            Write-Host "Finished downloading installer"
        }
        else {
            Write-Host "Doxygen already installed"
        }
    }

    $psiroot = $env:PsiRoot;
    if (-not (Test-Path env:PsiRoot)) {
        $psiroot = "c:\psi"
        $psirootsrc = "c:\psi\sources"
        if (-not (Test-Path $psirootsrc)) {
            Write-Host "Your PSI enlistment doesn't appear to exist at c:\psi."
            Write-Host "Please set the environment variable PsiRoot to point to your enlistment"
            [Environment]::Exit(1);
        }
        [Environment]::SetEnvironmentVariable("PsiRoot", $psiroot, 'Process');
    }
  
    $psidoxyoutput = $env:PSIDOXYOUTPUT;
    if (-not (Test-Path env:PSIDOXYOUTPUT)) {
        $psidoxyoutput = $psiroot
        [Environment]::SetEnvironmentVariable("PSIDOXYOUTPUT", $psidoxyoutput, 'Process');
    }
  
    if (-not $found) {
        $paths = [Environment]::GetEnvironmentVariable("PATH", 'Process');
        $paths = $doxygenInstallDir + ";" + $paths;
        [Environment]::SetEnvironmentVariable("PATH", $paths, 'Process');
    }

    # Create output folder
    $psidoxyout = $psidoxyoutput + "\DoxyOutput"
    if (-not (Test-Path $psidoxyout)) {
        New-Item -ItemType directory -Path $psidoxyout
    }
  
    $doxyConfig = $psiroot + "\Build\doxygen.config"
    Write-Host "Starting: '" $doxygenExe "' doxyConfig=" $doxyConfig
    $installer = Start-Process -FilePath $doxygenExe -PassThru -NoNewWindow -ArgumentList $doxyConfig
    $installer.WaitForExit()
    Write-Host "Doxygen finished. Final documentation can be found in " + $psiroot + "\DoxyOutput\html\classes.html"
}
catch {
    Write-Host "Failed to install Doxygen"
}