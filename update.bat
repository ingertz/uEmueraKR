@echo off
rem Pull the latest code from GitHub master. Unity can stay open.
cd /d "%~dp0"
git pull origin master
pause
