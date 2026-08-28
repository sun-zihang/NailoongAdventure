@echo off
chcp 65001 >nul
REM ============================================================
REM  奶龙冒险 Nailoong Adventure —— 一键构建 Windows 64
REM  双击运行即可：生成全部资产 -> 打包 exe
REM  输出到: Builds\Win64\NailoongAdventure.exe
REM ============================================================

set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe"
set "PROJ=%~dp0"
set "LOG=%TEMP%\nailoong_build.log"

echo ==========================================
echo   奶龙冒险 - 一键构建 Windows 64
echo ==========================================
echo.
echo   项目路径: %PROJ%
echo   编辑器  : %UNITY%
echo   日志文件: %LOG%
echo.

if not exist "%UNITY%" (
    echo [错误] 未找到 Unity 编辑器：
    echo   %UNITY%
    echo 请修改本脚本中的 UNITY 变量为你本机的 Unity.exe 路径。
    echo.
    pause
    exit /b 1
)

echo [1/2] 正在启动 Unity 执行生成与构建（首次较慢，请耐心等待）...
echo.

REM 注意：BuildWindows64 内部会在批处理模式下调用 EditorApplication.Exit(0/1)，
REM 因此这里不加 -quit，避免编辑器在初始化完成前被提前终止。
"%UNITY%" -batchmode -silent-crashes -accept-apiupdate -projectPath "%PROJ%" -executeMethod Nailoong.EditorTools.GameBuilder.BuildWindows64 -logFile "%LOG%"

set EXITCODE=%ERRORLEVEL%

echo.
echo ==========================================
if "%EXITCODE%"=="0" (
    echo   [成功] 构建完成！
    echo   可执行文件: %PROJ%Builds\Win64\NailoongAdventure.exe
    if exist "%PROJ%Builds\Win64\NailoongAdventure.exe" (
        echo   文件大小  : 
        dir "%PROJ%Builds\Win64\NailoongAdventure.exe" | findstr "NailoongAdventure.exe"
    )
) else (
    echo   [失败] Unity 退出码: %EXITCODE%
    echo   请查看日志排查: %LOG%
    echo.
    echo   常见原因：
    echo     1. Unity 编辑器安装损坏（Unity Hub 中修复/重装该版本）
    echo     2. 缺少 Windows Standalone 构建模块（Hub 中添加模块）
    echo     3. 项目正被另一个 Unity 实例占用（关闭编辑器后重试）
)
echo ==========================================
echo.
pause
