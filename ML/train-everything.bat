@echo off

set ToolPath=%localappdata%/Programs/Python/Python38

for %%i in (sinocolo-*.py) do %ToolPath%/python %%i

pause