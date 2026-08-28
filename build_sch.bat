@echo off
"C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -nographics -silent-crashes -accept-apiupdate -projectPath "D:\Project\NailoongAdventure" -logFile "C:\Users\33352\AppData\Local\Temp\build_sch.log" -executeMethod Nailoong.EditorTools.GameBuilder.BuildWindows64
exit /b %errorlevel%
