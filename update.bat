@echo off
rem GitHub(master)の最新コードを取り込む。Unityは開いたままでよい
cd /d "%~dp0"
git pull origin master
pause
