@echo off
setlocal
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe"
set LOG=%TEMP%\rebuild_audio.log
%UNITY% -projectPath "D:\Project\NailoongAdventure" -batchmode -nographics -quit -executeMethod Nailoong.EditorTools.GameBuilder.RebuildAudio -logFile "%LOG%"
