#!/bin/bash
goto BATCH
clear

echo "DirectPackageInstaller Build Script - Unix";

if !(dotnet --list-sdks | grep -q '8.'); then
  echo ".NET 8 SDK NOT FOUND"
  exit;
fi

echo ".NET 8 SDK FOUND"

# The correct SDK directory, contains a build-tools directory
export AndroidSdkDirectory=~/Android/Sdk/

# The correct NDK directory, contains a ndk-build executable
export AndroidNdkDirectory=~/Android/Sdk/ndk/24.0.8215888/

if [ -d "/usr/local/lib/android/sdk" ]; then
   echo "Github Action Android SDK Path Found"
   export AndroidSdkDirectory=/usr/local/lib/android/sdk
   export AndroidNdkDirectory=/usr/local/lib/android/sdk/ndk/24.0.8215888/
fi

dotnet clean
rm -rf Release
mkdir -p Release

# BUILD_TARGETS controls which platforms get built. Default = all.
# Accepts comma-separated list: win, linux, osx, android
BUILD_TARGETS="${BUILD_TARGETS:-win,linux,osx,android}"
echo "Build targets: $BUILD_TARGETS"

has_target() {
   case ",$BUILD_TARGETS," in *,"$1",*) return 0;; *) return 1;; esac
}

Publish () {
   echo "Building for $1"
   dotnet restore -r $1 DirectPackageInstaller/DirectPackageInstaller.Desktop/DirectPackageInstaller.Desktop.csproj
   dotnet publish DirectPackageInstaller/DirectPackageInstaller.Desktop/DirectPackageInstaller.Desktop.csproj -c Release -r $1 --no-restore $2
   case "$1" in
      win-*|linux-*|osx-*) ./Tools/prepare-pkgstream-runtime.sh $1 DirectPackageInstaller/DirectPackageInstaller.Desktop/bin/Release/net8.0/$1/publish ;;
   esac
   zip -j -9 -r Release/$1.zip DirectPackageInstaller/DirectPackageInstaller.Desktop/bin/Release/net8.0/$1/publish/* -x Icon.icns
}

WINPublish(){
   Publish $1
   cd Release
   unzip $1.zip -d tmp/
   dotnet ../Files/NSubsys.Tasks.dll ./tmp/DirectPackageInstaller.Desktop.exe
   rm $1.zip
   zip -j -9 -r $1.zip tmp/*
   rm -r tmp
   cd ..
}

OSXPublish (){
   Publish $1 "--self-contained -p:PublishSingleFile=false"
   cd Release
   mkdir -p tmp
   cp -R ../Files/OSXAppBase/DirectPackageInstaller.app tmp/
   unzip $1.zip -d tmp/DirectPackageInstaller.app/Contents/MacOS
   if [ -f tmp/DirectPackageInstaller.app/Contents/MacOS/DirectPackageInstaller.Desktop ]; then
      chmod +x tmp/DirectPackageInstaller.app/Contents/MacOS/DirectPackageInstaller.Desktop
   fi
   if command -v codesign >/dev/null 2>&1; then
      echo "Ad-hoc signing $1 .app bundle"
      codesign --force --deep --sign - tmp/DirectPackageInstaller.app
   else
      echo "WARNING: codesign not available, skipping ad-hoc signature"
      echo "         arm64 builds produced on non-macOS hosts will not launch on Apple Silicon"
   fi
   cd tmp
   zip -9 -r ../$1-app.zip ./
   cd ..
   if command -v hdiutil >/dev/null 2>&1; then
      echo "Packaging $1 DMG"
      mkdir -p dmg
      cp -R tmp/DirectPackageInstaller.app dmg/
      ln -s /Applications dmg/Applications
      hdiutil create -volname "DirectPackageInstaller" -srcfolder dmg -ov -format UDZO $1-app.dmg
      rm -rf dmg
   fi
   rm -r tmp
   cd ..
}

AndroidPublish () {
   if ! [ -d "${AndroidSdkDirectory}build-tools" ]; then
      echo "POSSIBLE INVALID ANDROID SDK PATH"
   fi
   if ! [ -f "${AndroidNdkDirectory}ndk-build" ]; then
      echo "POSSIBLE INVALID ANDROID NDK PATH"
   fi

   dotnet workload restore

   if [ ! -f "Release/.android-test.keystore" ]; then
      keytool -genkeypair -v -keystore "Release/.android-test.keystore" -storepass android -keypass android -alias androiddebugkey -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Android Debug,O=Android,C=US" >/dev/null 2>&1
   fi

   echo "Building Android for $1"
   dotnet restore -r $1 DirectPackageInstaller/DirectPackageInstaller.Android/DirectPackageInstaller.Android.csproj
   if ! dotnet publish DirectPackageInstaller/DirectPackageInstaller.Android/DirectPackageInstaller.Android.csproj -c Release -r $1 --no-restore; then
      echo "ANDROID DOTNET PUBLISH FAILED for $1"
      echo "Android bin tree:"
      find DirectPackageInstaller/DirectPackageInstaller.Android/bin -maxdepth 5 -print 2>/dev/null | head -300 || true
      echo "Android obj tree:"
      find DirectPackageInstaller/DirectPackageInstaller.Android/obj -maxdepth 5 -print 2>/dev/null | head -300 || true
      exit 1
   fi

   ANDROID_BIN_DIR="DirectPackageInstaller/DirectPackageInstaller.Android/bin"
   APK_SOURCE="$(find "$ANDROID_BIN_DIR" -type f -name '*.apk' -print -quit)"

   if [ -z "$APK_SOURCE" ]; then
      echo "ANDROID APK NOT FOUND under: $ANDROID_BIN_DIR"
      echo "Android bin tree:"
      find "$ANDROID_BIN_DIR" -maxdepth 8 -print 2>/dev/null | head -500 || true
      echo "Android obj tree:"
      find "DirectPackageInstaller/DirectPackageInstaller.Android/obj" -maxdepth 6 -print 2>/dev/null | head -300 || true
      echo "Possible Android packages anywhere in workspace:"
      find DirectPackageInstaller -type f \( -name '*.apk' -o -name '*.aab' -o -name '*.apks' \) -print 2>/dev/null | head -100 || true
      exit 1
   fi

   echo "Android package found: $APK_SOURCE"

   APK_DIR="$(dirname "$APK_SOURCE")"
   mv "$APK_SOURCE" "$APK_DIR/DPI-Extended.apk"
   APKSIGNER="$(find "$AndroidSdkDirectory/build-tools" -type f -name apksigner -print -quit)"
   if [ -z "$APKSIGNER" ]; then
      echo "ANDROID APKSIGNER NOT FOUND"
      exit 1
   fi
   "$APKSIGNER" sign --ks "Release/.android-test.keystore" --ks-pass pass:android --key-pass android --out "$APK_DIR/DPI-Extended-signed.apk" "$APK_DIR/DPI-Extended.apk"
   mv "$APK_DIR/DPI-Extended-signed.apk" "$APK_DIR/DPI-Extended.apk"
   "$APKSIGNER" verify --verbose "$APK_DIR/DPI-Extended.apk"
   zip -j -9 -r Release/$1.zip "$APK_DIR/DPI-Extended.apk"
}

if has_target win; then
   WINPublish win-x64
   WINPublish win-x86
   WINPublish win-arm
   WINPublish win-arm64
fi

if has_target linux; then
   Publish linux-x64
   Publish linux-arm
   Publish linux-arm64
fi

if has_target osx; then
   OSXPublish osx-x64
   OSXPublish osx-arm64
fi

if has_target android; then
   AndroidPublish android-x64
   AndroidPublish android-x86
   AndroidPublish android-arm
   AndroidPublish android-arm64
fi

cd Release

if has_target win; then
   mv win-x64.zip Windows-X64.zip
   mv win-x86.zip Windows-X86.zip
   mv win-arm.zip Windows-ARM.zip
   mv win-arm64.zip Windows-ARM64.zip
fi

if has_target linux; then
   mv linux-x64.zip Linux-X64.zip
   mv linux-arm.zip Linux-ARM.zip
   mv linux-arm64.zip Linux-ARM64.zip
fi

if has_target osx; then
   mv osx-x64.zip OSX-X64.zip
   mv osx-arm64.zip OSX-ARM64.zip
   mv osx-x64-app.zip OSX-X64-APP.zip
   mv osx-arm64-app.zip OSX-ARM64-APP.zip
   [ -f osx-x64-app.dmg ] && mv osx-x64-app.dmg OSX-X64-APP.dmg
   [ -f osx-arm64-app.dmg ] && mv osx-arm64-app.dmg OSX-ARM64-APP.dmg
fi

if has_target android; then
   mv android-x64.zip Android-X64.zip
   mv android-x86.zip Android-X86.zip
   mv android-arm.zip Android-ARM.zip
   mv android-arm64.zip Android-ARM64.zip
fi

cd ..

echo "Build Finished."
exit;

================================    UNIX SCRIPT END   ====================================
================================ WINDOWS SCRIPT BEGIN ====================================

:BATCH
echo off

dotnet --list-sdks | find /i "8."
if errorlevel 1 (
   cls
   echo DirectPackageInstaller Build Script - Windows
   echo .NET 8 SDK NOT FOUND.
   goto :eof
)

set AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk\
set AndroidNdkDirectory=C:\Program Files (x86)\Android\android-sdk\ndk\24.0.8215888\

cls
echo DirectPackageInstaller Build Script - Windows
echo .NET 8 SDK FOUND

dotnet clean
rmdir /s /q .\Release
mkdir .\Release

dotnet workload restore

call :Build win-x64
call :Build win-x86
call :Build win-arm
call :Build win-arm64
call :Build linux-x64
call :Build linux-arm
call :Build linux-arm64
call :OSXBuild osx-x64
call :OSXBuild osx-arm64
call :AndroidBuild android-x64
call :AndroidBuild android-x86
call :AndroidBuild android-arm
call :AndroidBuild android-arm64

cd Release
move win-x64.zip Windows-X64.zip
move win-x86.zip Windows-X86.zip
move win-arm.zip Windows-ARM.zip
move win-arm64.zip Windows-ARM64.zip
move linux-x64.zip Linux-X64.zip
move linux-arm.zip Linux-ARM.zip
move linux-arm64.zip Linux-ARM64.zip
move osx-x64.zip OSX-X64.zip
move osx-arm64.zip OSX-ARM64.zip
move osx-x64-app.zip OSX-X64-app.zip
move osx-arm64-app.zip OSX-ARM64-app.zip
move android-x64.zip Android-X64.zip
move android-x86.zip Android-X86.zip
move android-arm.zip Android-ARM.zip
move android-arm64.zip Android-ARM64.zip
cd ..
echo Build Finished.
goto :eof

exit
:Build
echo Building for %1
dotnet restore -r %1 .\DirectPackageInstaller\DirectPackageInstaller.Desktop\DirectPackageInstaller.Desktop.csproj
dotnet publish .\DirectPackageInstaller\DirectPackageInstaller.Desktop\DirectPackageInstaller.Desktop.csproj -c Release -r %1
if /i "%1"=="win-x64" powershell -ExecutionPolicy Bypass -File .\Tools\prepare-pkgstream-runtime.ps1 -Target %1 -Out .\DirectPackageInstaller\DirectPackageInstaller.Desktop\bin\Release\net8.0\%1\publish
if /i "%1"=="win-x86" powershell -ExecutionPolicy Bypass -File .\Tools\prepare-pkgstream-runtime.ps1 -Target %1 -Out .\DirectPackageInstaller\DirectPackageInstaller.Desktop\bin\Release\net8.0\%1\publish
if /i "%1"=="win-arm" powershell -ExecutionPolicy Bypass -File .\Tools\prepare-pkgstream-runtime.ps1 -Target %1 -Out .\DirectPackageInstaller\DirectPackageInstaller.Desktop\bin\Release\net8.0\%1\publish
if /i "%1"=="win-arm64" powershell -ExecutionPolicy Bypass -File .\Tools\prepare-pkgstream-runtime.ps1 -Target %1 -Out .\DirectPackageInstaller\DirectPackageInstaller.Desktop\bin\Release\net8.0\%1\publish
powershell Compress-Archive .\DirectPackageInstaller\DirectPackageInstaller.Desktop\bin\Release\net8.0\%1\publish\* .\Release\%1.zip
goto :eof

exit
:OSXBuild
call :Build %1
mkdir .\Release\tmp
xcopy /E /I /Y ".\Files\OSXAppBase\DirectPackageInstaller.app" ".\Release\tmp\DirectPackageInstaller.app"
powershell Expand-Archive -LiteralPath ".\Release\%1.zip" -DestinationPath ".\Release\tmp\DirectPackageInstaller.app\Contents\MacOS" -Force
powershell Compress-Archive .\Release\tmp\* .\Release\%1-app.zip
rmdir /s /q .\Release\tmp
goto :eof
:AndroidBuild
IF NOT EXIST "%AndroidSdkDirectory%build-tools" (
 echo ANDROID SDK NOT FOUND
 goto :eof
)
IF NOT EXIST "%AndroidNdkDirectory%ndk-build.cmd" (
 echo ANDROID NDK NOT FOUND
 goto :eof
)
echo Building Android for %1
if not exist ".\Release\.android-test.keystore" keytool -genkeypair -v -keystore ".\Release\.android-test.keystore" -storepass android -keypass android -alias androiddebugkey -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Android Debug,O=Android,C=US"
dotnet restore -r %1 .\DirectPackageInstaller\DirectPackageInstaller.Android\DirectPackageInstaller.Android.csproj
dotnet publish .\DirectPackageInstaller\DirectPackageInstaller.Android\DirectPackageInstaller.Android.csproj -c Release -r %1
set "ANDROID_OUTPUT_DIR=.\DirectPackageInstaller\DirectPackageInstaller.Android\bin"
set "APK_SOURCE="
for /r "%ANDROID_OUTPUT_DIR%" %%F in (*.apk) do (
  if exist "%%~fF" if not defined APK_SOURCE set "APK_SOURCE=%%~fF"
)
if not defined APK_SOURCE (
  echo ANDROID APK NOT FOUND under: %ANDROID_OUTPUT_DIR%
  exit /b 1
)
for %%D in ("%APK_SOURCE%") do set "APK_DIR=%%~dpD"
move /Y "%APK_SOURCE%" "%APK_DIR%DPI-Extended.apk"
set "APKSIGNER="
for /r "%AndroidSdkDirectory%build-tools" %%F in (apksigner.bat) do if not defined APKSIGNER set "APKSIGNER=%%~fF"
if not defined APKSIGNER (
  echo ANDROID APKSIGNER NOT FOUND
  exit /b 1
)
call "%APKSIGNER%" sign --ks ".\Release\.android-test.keystore" --ks-pass pass:android --key-pass android --out "%APK_DIR%DPI-Extended-signed.apk" "%APK_DIR%DPI-Extended.apk"
move /Y "%APK_DIR%DPI-Extended-signed.apk" "%APK_DIR%DPI-Extended.apk"
call "%APKSIGNER%" verify --verbose "%APK_DIR%DPI-Extended.apk"
powershell Compress-Archive -Path "%APK_DIR%DPI-Extended.apk" -DestinationPath ".\Release\%1.zip" -Force
goto :eof
