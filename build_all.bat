@echo off
setlocal
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe"
set LOG=%TEMP%\build_all.log
%UNITY% -projectPath "D:\Project\NailoongAdventure" -batchmode -nographics -quit -executeMethod Nailoong.EditorTools.GameBuilder.BuildAll -logFile "%LOG%"
