@echo off
REM 构建 WebGL 网页版（IL2CPP），输出到 Builds/WebGL
REM 需由计划任务在交互会话中启动（Asset Import Worker 需要桌面会话）
"C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -nographics -silent-crashes -accept-apiupdate -projectPath "D:\Project\NailoongAdventure" -logFile "C:\Users\33352\AppData\Local\Temp\build_webgl.log" -executeMethod Nailoong.EditorTools.GameBuilder.BuildWebGL
exit /b %errorlevel%
